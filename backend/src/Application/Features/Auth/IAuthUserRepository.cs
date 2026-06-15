using Demo.Domain.Entities;

namespace Demo.Application.Features.Auth;

public interface IAuthUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
