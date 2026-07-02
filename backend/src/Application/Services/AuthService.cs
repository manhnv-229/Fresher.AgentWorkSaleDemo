using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Options;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

public sealed class AuthService(
    IAuthUserRepository authUserRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUserSessionRepository userSessionRepository,
    IAuditLogService auditLogService,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IPermissionService permissionService,
    IRefreshTokenHasher refreshTokenHasher,
    IAuthOptions authOptions,
    IDistributedCacheService distributedCacheService,
    ILogger<AuthService> logger,
    IUnitOfWork unitOfWork) : IAuthService
{
    #region Method

    /// <summary>
    /// Xác thực thông tin đăng nhập và tạo phiên đăng nhập mới cho người dùng hợp lệ.
    /// </summary>
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

        var tokenResult = await CreateSessionAsync(user, ipAddress, cancellationToken);

        await auditLogService.RecordAsync(
            "login",
            user.FullName ?? user.Email,
            user.Id,
            null,
            ipAddress,
            $"User '{user.FullName ?? user.Email}' logged in successfully.",
            "User",
            user.Id.ToString(),
            cancellationToken);

        return ServiceResult<AuthTokenResult>.Success(tokenResult);
    }

    /// <summary>
    /// Đổi mật khẩu và thu hồi toàn bộ phiên đang còn hiệu lực của người dùng.
    /// </summary>
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
        user.ModifiedAt = DateTime.UtcNow;

        await RevokeActiveSessionsAsync(user.Id, ipAddress, "PasswordChanged", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.RecordAsync(
            "password_change",
            user.FullName ?? user.Email,
            user.Id,
            null,
            ipAddress,
            $"User '{user.FullName ?? user.Email}' changed their password.",
            "User",
            user.Id.ToString(),
            cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Làm mới access token bằng refresh token còn hiệu lực và xoay vòng refresh token cũ.
    /// </summary>
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
        // Refresh token cũ được đánh dấu thu hồi trước khi phát hành token mới để tránh tái sử dụng.
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = "Rotated";
        refreshToken.ReplacedByTokenHash = replacement.TokenHash;

        var jwt = await CreateAccessTokenAsync(refreshToken.User, refreshToken.SessionId, cancellationToken);
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

    /// <summary>
    /// Thu hồi refresh token hiện tại và đánh dấu phiên đăng xuất nếu còn hoạt động.
    /// </summary>
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
            await InvalidateSessionCacheAsync(refreshToken.Session.UserId, refreshToken.Session.Id, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy thông tin người dùng hiện tại từ định danh đã xác thực.
    /// </summary>
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

    /// <summary>
    /// Thu hồi toàn bộ phiên đang còn hiệu lực để buộc người dùng đăng nhập lại.
    /// </summary>
    private async Task RevokeActiveSessionsAsync(
        Guid userId,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        foreach (var session in activeSessions)
        {
            // Giữ nguyên thời điểm thu hồi cũ nếu phiên đã bị đánh dấu trước đó bởi luồng khác.
            session.RevokedAt ??= DateTime.UtcNow;
            session.RevokedByIp = ipAddress;
            session.ReasonRevoked = reason;
            await InvalidateSessionCacheAsync(session.UserId, session.Id, cancellationToken);
        }
    }

    /// <summary>
    /// Tạo bản ghi phiên, refresh token và access token chứa permission claims sau khi đăng nhập thành công.
    /// </summary>
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

        var jwt = await CreateAccessTokenAsync(user, session.Id, cancellationToken);
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

    /// <summary>
    /// Xóa cache session để tránh dùng lại trạng thái đã bị thu hồi.
    /// </summary>
    private Task InvalidateSessionCacheAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        return InvalidateSessionCacheCoreAsync(userId, sessionId, cancellationToken);
    }

    /// <summary>
    /// Xóa cache session nhưng không làm gián đoạn luồng xác thực khi Redis gặp sự cố.
    /// </summary>
    private async Task InvalidateSessionCacheCoreAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await distributedCacheService.RemoveAsync(BuildAuthSessionCacheKey(userId, sessionId), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể xóa cache session {SessionId} của user {UserId}.", sessionId, userId);
        }
    }

    /// <summary>
    /// Tạo khóa cache thống nhất cho trạng thái session xác thực.
    /// </summary>
    private static string BuildAuthSessionCacheKey(Guid userId, Guid sessionId)
    {
        return $"auth-session:{userId:N}:{sessionId:N}";
    }

    /// <summary>
    /// Tạo access token với tập quyền hiện hành để frontend route guard và backend authorization dùng cùng một tập quyền.
    /// </summary>
    private async Task<JwtTokenResult> CreateAccessTokenAsync(User user, Guid sessionId, CancellationToken cancellationToken)
    {
        var permissionCodes = await permissionService.GetGrantedPermissionCodesAsync(user.Id, cancellationToken);
        return jwtTokenService.CreateAccessToken(user, sessionId, permissionCodes);
    }

    #endregion
}
