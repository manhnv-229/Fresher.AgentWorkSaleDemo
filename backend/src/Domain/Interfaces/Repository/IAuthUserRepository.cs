using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface IAuthUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> GetForUpdateByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);
}
