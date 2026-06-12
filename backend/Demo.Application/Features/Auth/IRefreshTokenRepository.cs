using Demo.Domain.Entities;

namespace Demo.Application.Features.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashWithUserAndSessionAsync(string tokenHash, CancellationToken cancellationToken);

    Task<RefreshToken?> GetByTokenHashWithSessionAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
