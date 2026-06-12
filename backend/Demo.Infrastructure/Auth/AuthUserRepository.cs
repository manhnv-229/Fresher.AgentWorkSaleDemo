using Demo.Application.Features.Auth;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Auth;

public sealed class AuthUserRepository(DemoDbContext dbContext) : IAuthUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }
}
