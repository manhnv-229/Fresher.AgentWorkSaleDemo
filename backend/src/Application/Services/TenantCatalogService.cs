using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

public sealed class TenantCatalogService(
    ITenantCatalogQueryRepository tenantQueryRepository,
    ITenantCatalogRepository tenantRepository,
    IDistributedCacheService distributedCacheService,
    ICacheVersionService cacheVersionService,
    IApplicationCachePolicyProvider cachePolicyProvider,
    IMapper mapper,
    ILogger<TenantCatalogService> logger,
    IUnitOfWork unitOfWork) : ITenantCatalogService
{
    public async Task<ServiceResult<IReadOnlyList<TenantListItem>>> GetTenantsAsync(CancellationToken cancellationToken)
    {
        if (await TryBuildTenantListCacheKeyAsync(cancellationToken) is { } cacheKey)
        {
            var cachedTenants = await ApplicationCacheOperations.TryGetAsync<List<TenantListItem>>(
                distributedCacheService,
                logger,
                cacheKey,
                "tenant list",
                cancellationToken);
            if (cachedTenants is not null)
            {
                return ServiceResult<IReadOnlyList<TenantListItem>>.Success(cachedTenants);
            }
        }

        var tenants = await tenantQueryRepository.GetAllAsync(cancellationToken);
        var result = tenants.Select(tenant => mapper.Map<TenantListItem>(tenant)).ToList();
        if (await TryBuildTenantListCacheKeyAsync(cancellationToken) is { } tenantListCacheKey)
        {
            await ApplicationCacheOperations.TrySetAsync(
                distributedCacheService,
                logger,
                tenantListCacheKey,
                result,
                cachePolicyProvider.TenantListTimeToLive,
                "tenant list",
                cancellationToken);
        }

        return ServiceResult<IReadOnlyList<TenantListItem>>.Success(result);
    }

    public async Task<ServiceResult<TenantDetailItem>> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (await TryBuildTenantDetailCacheKeyAsync(tenantId, cancellationToken) is { } cacheKey)
        {
            var cachedTenant = await ApplicationCacheOperations.TryGetAsync<TenantDetailItem>(
                distributedCacheService,
                logger,
                cacheKey,
                "tenant detail",
                cancellationToken);
            if (cachedTenant is not null)
            {
                return ServiceResult<TenantDetailItem>.Success(cachedTenant);
            }
        }

        var tenant = await tenantQueryRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<TenantDetailItem>.Failure(
                TenantErrorCodes.TenantNotFound,
                "Tenant not found.");
        }

        var result = mapper.Map<TenantDetailItem>(tenant);
        if (await TryBuildTenantDetailCacheKeyAsync(tenantId, cancellationToken) is { } tenantDetailCacheKey)
        {
            await ApplicationCacheOperations.TrySetAsync(
                distributedCacheService,
                logger,
                tenantDetailCacheKey,
                result,
                cachePolicyProvider.TenantDetailTimeToLive,
                "tenant detail",
                cancellationToken);
        }

        return ServiceResult<TenantDetailItem>.Success(result);
    }

    public async Task<ServiceResult<TenantListItem>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
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
        await InvalidateTenantReadCacheAsync(tenant.Id, cancellationToken);

        return ServiceResult<TenantListItem>.Success(mapper.Map<TenantListItem>(tenant));
    }

    public async Task<ServiceResult<TenantDetailItem>> UpdateTenantAsync(
        Guid tenantId,
        UpdateTenantCommand command,
        CancellationToken cancellationToken)
    {
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
        await InvalidateTenantReadCacheAsync(tenant.Id, cancellationToken);

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
        await InvalidateTenantReadCacheAsync(tenant.Id, cancellationToken);

        return ServiceResult<TenantDetailItem>.Success(mapper.Map<TenantDetailItem>(tenant));
    }

    /// <summary>
    /// Tạo cache key cho danh sách tenant dùng version namespace chung.
    /// </summary>
    private async Task<string?> TryBuildTenantListCacheKeyAsync(CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.TenantListNamespace();
        var version = await ApplicationCacheOperations.TryGetVersionAsync(
            cacheVersionService,
            logger,
            namespaceKey,
            "tenant-list",
            cancellationToken);
        return version is null ? null : ApplicationCacheKeys.TenantList(version);
    }

    /// <summary>
    /// Tạo cache key cho chi tiết tenant.
    /// </summary>
    private async Task<string?> TryBuildTenantDetailCacheKeyAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.TenantDetailNamespace(tenantId);
        var version = await ApplicationCacheOperations.TryGetVersionAsync(
            cacheVersionService,
            logger,
            namespaceKey,
            $"tenant-detail cho tenant {tenantId}",
            cancellationToken);
        return version is null ? null : ApplicationCacheKeys.TenantDetail(tenantId, version);
    }

    /// <summary>
    /// Invalidate cache tenant list và tenant detail khi dữ liệu tenant thay đổi.
    /// </summary>
    private async Task InvalidateTenantReadCacheAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await ApplicationCacheOperations.TryRefreshVersionAsync(
            cacheVersionService,
            logger,
            ApplicationCacheKeys.TenantListNamespace(),
            "tenant list",
            cancellationToken);
        await ApplicationCacheOperations.TryRefreshVersionAsync(
            cacheVersionService,
            logger,
            ApplicationCacheKeys.TenantDetailNamespace(tenantId),
            "tenant detail",
            cancellationToken);
    }
}
