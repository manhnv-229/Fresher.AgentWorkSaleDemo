using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Options;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services;

public sealed class AuthService(
    IAuthUserRepository authUserRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUserSessionRepository userSessionRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenHasher refreshTokenHasher,
    IAuthOptions authOptions,
    IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<ServiceResult<AuthTokenResult>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await authUserRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InvalidCredentials, "Invalid email or password.");
        }

        if (user.Status == AccountStatus.Locked)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.LockedAccount, "User account is locked.");
        }

        if (user.Status != AccountStatus.Active)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InactiveUser, "User is not active.");
        }

        return ServiceResult<AuthTokenResult>.Success(await CreateSessionAsync(user, ipAddress, cancellationToken));
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await authUserRepository.GetForUpdateByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<bool>.Failure(AuthErrorCodes.UserNotFound, "User was not found.");
        }

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return ServiceResult<bool>.Failure(AuthErrorCodes.InvalidCurrentPassword, "Current password is incorrect.");
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.ModifiedAt = DateTime.UtcNow;

        await RevokeActiveSessionsAsync(user.Id, ipAddress, "PasswordChanged", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<AuthTokenResult>> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InvalidRefreshToken, "Refresh token is invalid.");
        }

        var tokenHash = refreshTokenHasher.HashToken(request.RefreshToken);
        var refreshToken = await refreshTokenRepository.GetByTokenHashWithUserAndSessionAsync(tokenHash, cancellationToken);

        if (refreshToken?.User is null || refreshToken.Session is null)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InvalidRefreshToken, "Refresh token is invalid.");
        }

        if (refreshToken.User.Status == AccountStatus.Locked)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.LockedAccount, "User account is locked.");
        }

        if (refreshToken.User.Status != AccountStatus.Active)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InactiveUser, "User is not active.");
        }

        if (refreshToken.ExpiresAt <= DateTime.UtcNow || refreshToken.RevokedAt is not null)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InvalidRefreshToken, "Refresh token is invalid.");
        }

        if (refreshToken.Session.RevokedAt is not null ||
            refreshToken.Session.ExpiresAt <= DateTime.UtcNow ||
            refreshToken.Session.UserId != refreshToken.UserId)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.SessionRevoked, "Session is no longer active.");
        }

        var replacement = refreshTokenHasher.GenerateToken();
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = "Rotated";
        refreshToken.ReplacedByTokenHash = replacement.TokenHash;

        var jwt = jwtTokenService.CreateAccessToken(refreshToken.User, refreshToken.SessionId);
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = refreshToken.UserId,
            SessionId = refreshToken.SessionId,
            TokenHash = replacement.TokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(authOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        refreshTokenRepository.Add(newRefreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthTokenResult>.Success(new AuthTokenResult(
            jwt.AccessToken,
            replacement.RawToken,
            jwt.ExpiresAt,
            newRefreshToken.ExpiresAt));
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = refreshTokenHasher.HashToken(request.RefreshToken);
        var refreshToken = await refreshTokenRepository.GetByTokenHashWithSessionAsync(tokenHash, cancellationToken);
        if (refreshToken is null || refreshToken.RevokedAt is not null)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = "Logout";

        if (refreshToken.Session is not null && refreshToken.Session.RevokedAt is null)
        {
            refreshToken.Session.RevokedAt = DateTime.UtcNow;
            refreshToken.Session.RevokedByIp = ipAddress;
            refreshToken.Session.ReasonRevoked = "Logout";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await authUserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<CurrentUserResponse>.Failure(AuthErrorCodes.UserNotFound, "User was not found.");
        }

        return ServiceResult<CurrentUserResponse>.Success(new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Status.ToString()));
    }

    private async Task RevokeActiveSessionsAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var session in activeSessions)
        {
            session.RevokedAt ??= DateTime.UtcNow;
            session.RevokedByIp = ipAddress;
            session.ReasonRevoked = reason;
        }
    }

    private async Task<AuthTokenResult> CreateSessionAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(authOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        var jwt = jwtTokenService.CreateAccessToken(user, session.Id);
        var refreshToken = refreshTokenHasher.GenerateToken();
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = session.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        userSessionRepository.Add(session);
        refreshTokenRepository.Add(refreshTokenEntity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(jwt.AccessToken, refreshToken.RawToken, jwt.ExpiresAt, refreshTokenEntity.ExpiresAt);
    }
}
