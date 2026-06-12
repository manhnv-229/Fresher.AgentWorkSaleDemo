using Demo.Application.Features.Auth;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

namespace Demo.Infrastructure.Auth;

public sealed class UserSessionRepository(DemoDbContext dbContext) : IUserSessionRepository
{
    public void Add(UserSession session)
    {
        dbContext.UserSessions.Add(session);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
