using Demo.Application.UseCases.Common;

namespace Demo.Application.Features.Tenants;

public interface ITenantCatalogService
{
    Task<ServiceResult<IReadOnlyList<TenantListItem>>> GetTenantsAsync(CancellationToken cancellationToken);

    Task<ServiceResult<TenantListItem>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken);
}
