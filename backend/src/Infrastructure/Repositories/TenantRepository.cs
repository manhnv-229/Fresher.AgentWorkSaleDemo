using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

/// <summary>
/// Đọc và cập nhật tenant catalog trong cơ sở dữ liệu.
/// </summary>
public sealed class TenantCatalogRepository(DemoDbContext dbContext) : ITenantCatalogRepository
{
    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeTenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants.AnyAsync(
            tenant => tenant.Code == code && (!excludeTenantId.HasValue || tenant.Id != excludeTenantId.Value),
            cancellationToken);
    }

    public void Add(Tenant tenant)
    {
        dbContext.Tenants.Add(tenant);
    }

    public void Update(Tenant tenant)
    {
        dbContext.Tenants.Update(tenant);
    }
}
