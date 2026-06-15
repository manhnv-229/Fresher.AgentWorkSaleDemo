using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services;

public sealed class TenantCatalogService(
    ITenantCatalogRepository tenantRepository,
    IUnitOfWork unitOfWork) : ITenantCatalogService
{
    public async Task<ServiceResult<IReadOnlyList<TenantListItem>>> GetTenantsAsync(CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<TenantListItem>>.Success(tenants.Select(MapTenant).ToList());
    }

    public async Task<ServiceResult<TenantListItem>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Code))
        {
            return ServiceResult<TenantListItem>.Failure(
                TenantErrorCodes.ValidationError,
                "Name and code are required.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Code = command.Code.Trim(),
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        tenantRepository.Add(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<TenantListItem>.Success(MapTenant(tenant));
    }

    private static TenantListItem MapTenant(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.Name,
            tenant.Code,
            tenant.Status.ToString());
}
