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
            var cachedTenants = await TryGetCachedValueAsync<List<TenantListItem>>(cacheKey, "tenant list", cancellationToken);
            if (cachedTenants is not null)
            {
                return ServiceResult<IReadOnlyList<TenantListItem>>.Success(cachedTenants);
            }
        }

        var tenants = await tenantQueryRepository.GetAllAsync(cancellationToken);
        var result = tenants.Select(tenant => mapper.Map<TenantListItem>(tenant)).ToList();
        if (await TryBuildTenantListCacheKeyAsync(cancellationToken) is { } tenantListCacheKey)
        {
            await TrySetCachedValueAsync(tenantListCacheKey, result, cachePolicyProvider.TenantListTimeToLive, "tenant list", cancellationToken);
        }

        return ServiceResult<IReadOnlyList<TenantListItem>>.Success(result);
    }

    public async Task<ServiceResult<TenantDetailItem>> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (await TryBuildTenantDetailCacheKeyAsync(tenantId, cancellationToken) is { } cacheKey)
        {
            var cachedTenant = await TryGetCachedValueAsync<TenantDetailItem>(cacheKey, "tenant detail", cancellationToken);
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
            await TrySetCachedValueAsync(tenantDetailCacheKey, result, cachePolicyProvider.TenantDetailTimeToLive, "tenant detail", cancellationToken);
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
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            return ApplicationCacheKeys.TenantList(version);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể lấy tenant-list cache version.");
            return null;
        }
    }

    /// <summary>
    /// Tạo cache key cho chi tiết tenant.
    /// </summary>
    private async Task<string?> TryBuildTenantDetailCacheKeyAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.TenantDetailNamespace(tenantId);
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            return ApplicationCacheKeys.TenantDetail(tenantId, version);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể lấy tenant-detail cache version cho tenant {TenantId}.", tenantId);
            return null;
        }
    }

    /// <summary>
    /// Invalidate cache tenant list và tenant detail khi dữ liệu tenant thay đổi.
    /// </summary>
    private async Task InvalidateTenantReadCacheAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await TryRefreshCacheVersionAsync(ApplicationCacheKeys.TenantListNamespace(), "tenant list", cancellationToken);
        await TryRefreshCacheVersionAsync(ApplicationCacheKeys.TenantDetailNamespace(tenantId), "tenant detail", cancellationToken);
    }

    /// <summary>
    /// Đọc cache typed và fallback khi Redis lỗi.
    /// </summary>
    private async Task<TValue?> TryGetCachedValueAsync<TValue>(
        string cacheKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            return await distributedCacheService.GetAsync<TValue>(cacheKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể đọc {CacheLabel} cache.", cacheLabel);
            return default;
        }
    }

    /// <summary>
    /// Ghi cache typed mà không làm gián đoạn luồng hiện tại khi Redis lỗi.
    /// </summary>
    private async Task TrySetCachedValueAsync<TValue>(
        string cacheKey,
        TValue value,
        TimeSpan timeToLive,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await distributedCacheService.SetAsync(cacheKey, value, timeToLive, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể ghi {CacheLabel} cache.", cacheLabel);
        }
    }

    /// <summary>
    /// Làm mới version token của namespace cache.
    /// </summary>
    private async Task TryRefreshCacheVersionAsync(
        string namespaceKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await cacheVersionService.RefreshVersionAsync(namespaceKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể invalidate {CacheLabel} cache.", cacheLabel);
        }
    }
}
