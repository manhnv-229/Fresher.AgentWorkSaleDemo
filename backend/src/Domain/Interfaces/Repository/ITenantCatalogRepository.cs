using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface ITenantCatalogRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Tenant tenant);
}
