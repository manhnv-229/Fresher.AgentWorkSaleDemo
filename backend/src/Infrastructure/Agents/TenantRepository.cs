using Demo.Application.Features.Agents;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Agents;

public sealed class TenantRepository(DemoDbContext dbContext) : ITenantRepository
{
    public Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }
}
