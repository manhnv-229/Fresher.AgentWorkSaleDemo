using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashWithUserAndSessionAsync(string tokenHash, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByTokenHashWithSessionAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
