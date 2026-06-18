using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

public sealed class UserSessionRepository(DemoDbContext dbContext) : IUserSessionRepository
{
    public void Add(UserSession session)
    {
        dbContext.UserSessions.Add(session);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}
