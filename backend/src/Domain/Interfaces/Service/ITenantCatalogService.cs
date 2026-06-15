using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;

namespace Demo.Domain.Interfaces.Service;

public interface ITenantCatalogService
{
    Task<ServiceResult<IReadOnlyList<TenantListItem>>> GetTenantsAsync(CancellationToken cancellationToken);

    Task<ServiceResult<TenantListItem>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken);
}
