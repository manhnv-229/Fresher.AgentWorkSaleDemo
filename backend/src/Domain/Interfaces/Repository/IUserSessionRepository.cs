using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface IUserSessionRepository
{
    void Add(UserSession session);
}
