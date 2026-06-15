using Demo.Application.Features.Auth;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Auth;

public sealed class RefreshTokenRepository(DemoDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashWithUserAndSessionAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .Include(refreshToken => refreshToken.Session)
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);
    }

    public Task<RefreshToken?> GetByTokenHashWithSessionAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.Session)
            .SingleOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);
    }

    public void Add(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
    }
}
