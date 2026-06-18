using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

public sealed class AgentTenantRepository(DemoDbContext dbContext) : ITenantRepository
{
    public Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .FirstOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }
}
