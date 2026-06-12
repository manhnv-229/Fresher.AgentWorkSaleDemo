using Demo.Application.Authorization;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

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
                x.Status == RecordStatus.Active,
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
