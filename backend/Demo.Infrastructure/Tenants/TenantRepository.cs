using Demo.Application.Features.Tenants;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Tenants;

public sealed class TenantRepository(DemoDbContext dbContext) : ITenantCatalogRepository
{
    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Tenant tenant)
    {
        dbContext.Tenants.Add(tenant);
    }
}
