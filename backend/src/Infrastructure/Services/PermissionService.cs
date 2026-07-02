using Demo.Application.Common;
using Demo.Domain.Interfaces.Service;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Cung cấp truy vấn RBAC với Redis cache cho tập quyền và phép kiểm tra quyền lặp lại.
/// </summary>
public sealed class PermissionService(
    DemoDbContext dbContext,
    IDistributedCacheService distributedCacheService,
    ICacheVersionService cacheVersionService,
    IApplicationCachePolicyProvider cachePolicyProvider,
    ILogger<PermissionService> logger) : IPermissionService
{
    /// <summary>
    /// Kiểm tra người dùng có quyền cụ thể trong scope tenant hiện tại hay không.
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid? tenantId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.PermissionNamespace(userId);
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            var cacheKey = ApplicationCacheKeys.PermissionCheck(userId, tenantId, permissionCode, version);
            var cachedResult = await distributedCacheService.GetAsync<bool?>(cacheKey, cancellationToken);
            if (cachedResult.HasValue)
            {
                return cachedResult.Value;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể đọc permission cache của user {UserId}.", userId);
        }

        var hasPermission = await EvaluatePermissionAsync(userId, tenantId, permissionCode, cancellationToken);
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            var cacheKey = ApplicationCacheKeys.PermissionCheck(userId, tenantId, permissionCode, version);
            await distributedCacheService.SetAsync(cacheKey, hasPermission, cachePolicyProvider.PermissionEntryTimeToLive, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể ghi permission cache của user {UserId}.", userId);
        }

        return hasPermission;
    }

    /// <summary>
    /// Lấy toàn bộ mã quyền hiệu lực của người dùng để dùng cho access token và UI authorization.
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetGrantedPermissionCodesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.PermissionNamespace(userId);
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            var cacheKey = ApplicationCacheKeys.PermissionSet(userId, version);
            var cachedPermissions = await distributedCacheService.GetAsync<string[]>(cacheKey, cancellationToken);
            if (cachedPermissions is not null)
            {
                return cachedPermissions;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể đọc permission-set cache của user {UserId}.", userId);
        }

        var globalPermissions = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId == userId &&
                userRole.TenantId == null &&
                userRole.Role != null &&
                userRole.Role.TenantId == null)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .Where(rolePermission => rolePermission.Permission != null)
            .Select(rolePermission => rolePermission.Permission!.Code)
            .ToListAsync(cancellationToken);

        var activeTenantIds = await dbContext.UserTenants
            .AsNoTracking()
            .Where(userTenant =>
                userTenant.UserId == userId &&
                userTenant.Status == MembershipStatus.Active)
            .Select(userTenant => userTenant.TenantId)
            .ToListAsync(cancellationToken);

        if (activeTenantIds.Count == 0)
        {
            return globalPermissions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(permissionCode => permissionCode, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var tenantPermissions = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.UserId == userId &&
                userRole.TenantId.HasValue &&
                activeTenantIds.Contains(userRole.TenantId.Value) &&
                userRole.Role != null &&
                userRole.Role.TenantId == userRole.TenantId)
            .SelectMany(userRole => userRole.Role!.RolePermissions)
            .Where(rolePermission => rolePermission.Permission != null)
            .Select(rolePermission => rolePermission.Permission!.Code)
            .ToListAsync(cancellationToken);

        var permissions = globalPermissions
            .Concat(tenantPermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permissionCode => permissionCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            var cacheKey = ApplicationCacheKeys.PermissionSet(userId, version);
            await distributedCacheService.SetAsync(cacheKey, permissions, cachePolicyProvider.PermissionEntryTimeToLive, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể ghi permission-set cache của user {UserId}.", userId);
        }

        return permissions;
    }

    /// <summary>
    /// Thực hiện logic RBAC gốc từ database khi cache miss hoặc cache lỗi.
    /// </summary>
    private async Task<bool> EvaluatePermissionAsync(
        Guid userId,
        Guid? tenantId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var hasGlobalPermission = await dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(userRole =>
                userRole.UserId == userId &&
                userRole.TenantId == null &&
                userRole.Role != null &&
                userRole.Role.TenantId == null &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.Permission != null &&
                    rolePermission.Permission.Code == permissionCode),
                cancellationToken);

        if (hasGlobalPermission)
        {
            return true;
        }

        if (tenantId is null)
        {
            return false;
        }

        var hasActiveTenantMembership = await dbContext.UserTenants
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == userId &&
                x.TenantId == tenantId &&
                x.Status == MembershipStatus.Active,
                cancellationToken);

        if (!hasActiveTenantMembership)
        {
            return false;
        }

        return await dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(userRole =>
                userRole.UserId == userId &&
                userRole.TenantId == tenantId &&
                userRole.Role != null &&
                userRole.Role.TenantId == tenantId &&
                userRole.Role.RolePermissions.Any(rolePermission =>
                    rolePermission.Permission != null &&
                    rolePermission.Permission.Code == permissionCode),
                cancellationToken);
    }
}
