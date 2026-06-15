using Demo.Domain.Interfaces.Repository;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

public sealed class TenantRepository(DemoDbContext dbContext) : ITenantRepository
{
    public Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }
}
