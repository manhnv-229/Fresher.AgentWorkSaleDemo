using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Repository;

public interface ITenantCatalogQueryRepository
{
    Task<IReadOnlyList<TenantListRow>> GetAllAsync(CancellationToken cancellationToken);

    Task<TenantDetailRow?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken);
}
