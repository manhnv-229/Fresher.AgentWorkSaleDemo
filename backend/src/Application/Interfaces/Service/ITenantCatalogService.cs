using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;

namespace Demo.Domain.Interfaces.Service;

public interface ITenantCatalogService
{
    Task<ServiceResult<IReadOnlyList<TenantListItem>>> GetTenantsAsync(CancellationToken cancellationToken);

    Task<ServiceResult<TenantDetailItem>> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<ServiceResult<TenantListItem>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<TenantDetailItem>> UpdateTenantAsync(
        Guid tenantId,
        UpdateTenantCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<TenantDetailItem>> LockTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
