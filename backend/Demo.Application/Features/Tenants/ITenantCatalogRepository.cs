using Demo.Domain.Entities;

namespace Demo.Application.Features.Tenants;

public interface ITenantCatalogRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Tenant tenant);
}
