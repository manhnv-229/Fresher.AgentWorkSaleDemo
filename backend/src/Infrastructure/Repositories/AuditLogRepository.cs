using Demo.Domain.Entities;
using Demo.Domain.Interfaces.Repository;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

/// <summary>
/// Ghi và truy vấn audit log trực tiếp qua Entity Framework Core.
/// </summary>
public sealed class AuditLogRepository(DemoDbContext dbContext) : IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLogEntry>> GetFilteredAsync(
        string? search,
        DateTime? createdDateFrom,
        DateTime? createdDateTo,
        IReadOnlyList<string>? actions,
        IReadOnlyList<string>? targetTypes,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Action.ToLower().Contains(term) ||
                e.UserName.ToLower().Contains(term) ||
                e.Description.ToLower().Contains(term));
        }

        if (createdDateFrom.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= createdDateFrom.Value);
        }

        if (createdDateTo.HasValue)
        {
            query = query.Where(e => e.CreatedAt < createdDateTo.Value);
        }

        if (actions is { Count: > 0 })
        {
            query = query.Where(e => actions.Contains(e.Action));
        }

        if (targetTypes is { Count: > 0 })
        {
            query = query.Where(e => e.TargetType != null && targetTypes.Contains(e.TargetType));
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(AuditLogEntry entry)
    {
        dbContext.AuditLogEntries.Add(entry);
    }
}
