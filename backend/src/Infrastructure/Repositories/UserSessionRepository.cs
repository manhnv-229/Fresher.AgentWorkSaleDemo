using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

namespace Demo.Infrastructure.Repositories;

public sealed class UserSessionRepository(DemoDbContext dbContext) : IUserSessionRepository
{
    public void Add(UserSession session)
    {
        dbContext.UserSessions.Add(session);
    }
}
