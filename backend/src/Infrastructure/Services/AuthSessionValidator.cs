using Demo.Domain.Interfaces.Service;
using Demo.Infrastructure.Caching;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Xác thực trạng thái session bằng cache Redis trước khi fallback về MySQL.
/// </summary>
public sealed class AuthSessionValidator(
    DemoDbContext dbContext,
    IDistributedCacheService distributedCacheService,
    ILogger<AuthSessionValidator> logger) : IAuthSessionValidator
{
    /// <summary>
    /// Kiểm tra session còn hiệu lực hay không.
    /// </summary>
    public async Task<bool> IsSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cacheKey = CacheKeys.AuthSession(userId, sessionId);

        try
        {
            var cachedEntry = await distributedCacheService.GetAsync<AuthSessionCacheEntry>(cacheKey, cancellationToken);
            if (cachedEntry is not null &&
                cachedEntry.UserId == userId &&
                cachedEntry.SessionId == sessionId &&
                cachedEntry.ExpiresAt > now)
            {
                return true;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể đọc cache session {SessionId} của user {UserId}.", sessionId, userId);
        }

        var session = await dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existingSession =>
                    existingSession.Id == sessionId &&
                    existingSession.UserId == userId &&
                    existingSession.RevokedAt == null &&
                    existingSession.ExpiresAt > now,
                cancellationToken);

        if (session is null)
        {
            return false;
        }

        var timeToLive = session.ExpiresAt - now;
        if (timeToLive > TimeSpan.Zero)
        {
            var cacheEntry = new AuthSessionCacheEntry
            {
                UserId = userId,
                SessionId = sessionId,
                ExpiresAt = session.ExpiresAt
            };

            try
            {
                await distributedCacheService.SetAsync(cacheKey, cacheEntry, timeToLive, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Không thể ghi cache session {SessionId} của user {UserId}.", sessionId, userId);
            }
        }

        return true;
    }
}
