using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface ITenantCatalogRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludeTenantId, CancellationToken cancellationToken);

    void Add(Tenant tenant);

    void Update(Tenant tenant);
}
