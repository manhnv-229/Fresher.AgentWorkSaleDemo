using Demo.Domain.Interfaces.Service;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Services;

public sealed class PermissionService(DemoDbContext dbContext) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(
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

    public async Task<IReadOnlyCollection<string>> GetGrantedPermissionCodesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
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

        return globalPermissions
            .Concat(tenantPermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permissionCode => permissionCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
