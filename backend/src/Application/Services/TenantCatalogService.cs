using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using AutoMapper;

namespace Demo.Application.Services;

public sealed class TenantCatalogService(
    ITenantCatalogQueryRepository tenantQueryRepository,
    ITenantCatalogRepository tenantRepository,
    IMapper mapper,
    IUnitOfWork unitOfWork) : ITenantCatalogService
{
    public async Task<ServiceResult<IReadOnlyList<TenantListItem>>> GetTenantsAsync(CancellationToken cancellationToken)
    {
        var tenants = await tenantQueryRepository.GetAllAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<TenantListItem>>.Success(
            tenants.Select(tenant => mapper.Map<TenantListItem>(tenant)).ToList());
    }

    public async Task<ServiceResult<TenantDetailItem>> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantQueryRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.TenantNotFound,
                "Tenant not found.");
        }

        return ServiceResult<TenantDetailItem>.Success(mapper.Map<TenantDetailItem>(tenant));
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

        var exists = await tenantRepository.ExistsByCodeAsync(command.Code.Trim(), null, cancellationToken);
        if (exists)
        {
            return ServiceResult<TenantListItem>.Failure(
                TenantErrorCodes.DuplicateTenantCode,
                "A tenant with this code already exists.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Code = command.Code.Trim(),
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        tenantRepository.Add(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<TenantListItem>.Success(mapper.Map<TenantListItem>(tenant));
    }

    public async Task<ServiceResult<TenantDetailItem>> UpdateTenantAsync(
        Guid tenantId,
        UpdateTenantCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Code))
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.ValidationError,
                "Name and code are required.");
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.TenantNotFound,
                "Tenant not found.");
        }

        var codeExists = await tenantRepository.ExistsByCodeAsync(command.Code.Trim(), tenantId, cancellationToken);
        if (codeExists)
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.DuplicateTenantCode,
                "A tenant with this code already exists.");
        }

        tenant.Name = command.Name.Trim();
        tenant.Code = command.Code.Trim();
        tenant.ModifiedAt = DateTime.UtcNow;

        tenantRepository.Update(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<TenantDetailItem>.Success(mapper.Map<TenantDetailItem>(tenant));
    }

    public async Task<ServiceResult<TenantDetailItem>> LockTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.TenantNotFound,
                "Tenant not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.InvalidStatusTransition,
                "Tenant is already locked.");
        }

        tenant.Status = TenantStatus.Locked;
        tenant.ModifiedAt = DateTime.UtcNow;

        tenantRepository.Update(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<TenantDetailItem>.Success(mapper.Map<TenantDetailItem>(tenant));
    }
}
