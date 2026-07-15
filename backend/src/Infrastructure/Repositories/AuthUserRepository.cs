using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

/// <summary>
/// Truy vấn người dùng cho các luồng xác thực và quản trị tài khoản.
/// </summary>
public sealed class AuthUserRepository(DemoDbContext dbContext) : IAuthUserRepository
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetFilteredAsync(string? search, string? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.EmployeeCode != null && u.EmployeeCode.ToLower().Contains(term)) ||
                u.Email.ToLower().Contains(term) ||
                (u.Project != null && u.Project.ToLower().Contains(term)) ||
                (u.JobPosition != null && u.JobPosition.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status.ToString() == status);
        }

        return await query
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> GetForUpdateByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}
