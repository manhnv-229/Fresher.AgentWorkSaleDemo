using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface ITenantRepository
{
    Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken);
}
