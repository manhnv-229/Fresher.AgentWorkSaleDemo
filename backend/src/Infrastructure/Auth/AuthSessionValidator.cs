using Demo.Application.Features.Auth;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Auth;

public sealed class AuthSessionValidator(DemoDbContext dbContext) : IAuthSessionValidator
{
    public Task<bool> IsSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return dbContext.UserSessions
            .AsNoTracking()
            .AnyAsync(session =>
                session.Id == sessionId &&
                session.UserId == userId &&
                session.RevokedAt == null &&
                session.ExpiresAt > now,
                cancellationToken);
    }
}
