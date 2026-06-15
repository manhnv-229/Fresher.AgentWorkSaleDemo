using Demo.Domain.Entities;

namespace Demo.Application.Features.Auth;

public interface IUserSessionRepository
{
    void Add(UserSession session);
}
