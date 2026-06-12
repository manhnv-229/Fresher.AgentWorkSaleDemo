using Demo.Application.Features.Auth;
using Demo.Application.UseCases.Common;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.Auth;

public sealed class AuthService(
    DemoDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenHasher refreshTokenHasher,
    IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<ServiceResult<AuthTokenResult>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InvalidCredentials, "Invalid email or password.");
        }

        if (user.Status != RecordStatus.Active)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InactiveUser, "User is not active.");
        }

        return ServiceResult<AuthTokenResult>.Success(await CreateSessionAsync(user, ipAddress, cancellationToken));
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
        var refreshToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .Include(x => x.Session)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken?.User is null ||
            refreshToken.Session is null ||
            refreshToken.ExpiresAt <= DateTime.UtcNow ||
            refreshToken.RevokedAt is not null ||
            refreshToken.Session.RevokedAt is not null ||
            refreshToken.Session.ExpiresAt <= DateTime.UtcNow ||
            refreshToken.Session.UserId != refreshToken.UserId ||
            refreshToken.User.Status != RecordStatus.Active)
        {
            return ServiceResult<AuthTokenResult>.Failure(AuthErrorCodes.InvalidRefreshToken, "Refresh token is invalid.");
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
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

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
        var refreshToken = await dbContext.RefreshTokens
            .Include(x => x.Session)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
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

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
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

    private async Task<AuthTokenResult> CreateSessionAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
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

        dbContext.UserSessions.Add(session);
        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(jwt.AccessToken, refreshToken.RawToken, jwt.ExpiresAt, refreshTokenEntity.ExpiresAt);
    }
}
