using Demo.Application.Authorization;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Persistence;

public sealed class DatabaseSeeder(DemoDbContext dbContext, IPasswordHasher passwordHasher)
{
    private static readonly Guid SystemAdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantOneId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid TenantTwoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantAdminRoleId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    private static readonly Guid AgentManagerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333332");
    private static readonly Guid AgentViewerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid AdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    private static readonly Guid ManagerUserId = Guid.Parse("44444444-4444-4444-4444-444444444442");
    private static readonly Guid ViewerUserId = Guid.Parse("44444444-4444-4444-4444-444444444443");

    private static readonly (string Code, string Name, string Group)[] Permissions =
    [
        (PermissionCodes.TenantView, "View tenants", "Tenant"),
        (PermissionCodes.TenantCreate, "Create tenants", "Tenant"),
        (PermissionCodes.TenantUpdate, "Update tenants", "Tenant"),
        (PermissionCodes.TenantDelete, "Delete tenants", "Tenant"),
        (PermissionCodes.AgentView, "View agents", "Agent"),
        (PermissionCodes.AgentCreate, "Create agents", "Agent"),
        (PermissionCodes.AgentUpdate, "Update agents", "Agent"),
        (PermissionCodes.AgentDelete, "Delete agents", "Agent"),
        (PermissionCodes.UserView, "View users", "User"),
        (PermissionCodes.UserInvite, "Invite users", "User"),
        (PermissionCodes.UserUpdate, "Update users", "User"),
        (PermissionCodes.RoleView, "View roles", "Role"),
        (PermissionCodes.RoleCreate, "Create roles", "Role"),
        (PermissionCodes.RoleUpdate, "Update roles", "Role"),
        (PermissionCodes.RoleAssign, "Assign roles", "Role")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedTenantsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedMembershipsAsync(cancellationToken);
        await SeedAgentsAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        foreach (var permission in Permissions)
        {
            if (await dbContext.Permissions.AnyAsync(x => x.Code == permission.Code, cancellationToken))
            {
                continue;
            }

            dbContext.Permissions.Add(new Permission
            {
                Id = StableGuid("permission", permission.Code),
                Code = permission.Code,
                Name = permission.Name,
                GroupName = permission.Group,
                Description = permission.Name
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTenantsAsync(CancellationToken cancellationToken)
    {
        await AddTenantAsync(TenantOneId, "Tenant One", "tenant-one", cancellationToken);
        await AddTenantAsync(TenantTwoId, "Tenant Two", "tenant-two", cancellationToken);
    }

    private async Task AddTenantAsync(Guid id, string name, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.Tenants.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.Tenants.Add(new Tenant
        {
            Id = id,
            Name = name,
            Code = code,
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        await AddRoleAsync(SystemAdminRoleId, null, "SystemAdmin", "SystemAdmin", true, cancellationToken);
        await AddRoleAsync(TenantAdminRoleId, TenantOneId, "TenantAdmin", "TenantAdmin", false, cancellationToken);
        await AddRoleAsync(AgentManagerRoleId, TenantOneId, "AgentManager", "AgentManager", false, cancellationToken);
        await AddRoleAsync(AgentViewerRoleId, TenantTwoId, "AgentViewer", "AgentViewer", false, cancellationToken);
    }

    private async Task AddRoleAsync(
        Guid id,
        Guid? tenantId,
        string name,
        string code,
        bool isSystemRole,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Roles.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.Roles.Add(new Role
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Code = code,
            Description = name,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        await AddPermissionsAsync(SystemAdminRoleId, allPermissions.Select(x => x.Code), cancellationToken);
        await AddPermissionsAsync(TenantAdminRoleId, allPermissions.Where(x => x.GroupName != "Tenant").Select(x => x.Code), cancellationToken);
        await AddPermissionsAsync(AgentManagerRoleId, [PermissionCodes.AgentView, PermissionCodes.AgentCreate, PermissionCodes.AgentUpdate], cancellationToken);
        await AddPermissionsAsync(AgentViewerRoleId, [PermissionCodes.AgentView], cancellationToken);
    }

    private async Task AddPermissionsAsync(Guid roleId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken)
    {
        foreach (var permissionCode in permissionCodes)
        {
            var permissionId = await dbContext.Permissions
                .Where(x => x.Code == permissionCode)
                .Select(x => x.Id)
                .SingleAsync(cancellationToken);

            if (await dbContext.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken))
            {
                continue;
            }

            dbContext.RolePermissions.Add(new RolePermission
            {
                Id = StableGuid("role-permission", $"{roleId}:{permissionId}"),
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        await AddUserAsync(AdminUserId, "admin@example.com", "Admin User", cancellationToken);
        await AddUserAsync(ManagerUserId, "manager@example.com", "Manager User", cancellationToken);
        await AddUserAsync(ViewerUserId, "viewer@example.com", "Viewer User", cancellationToken);
    }

    private async Task AddUserAsync(Guid id, string email, string fullName, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.Users.Add(new User
        {
            Id = id,
            Email = email,
            FullName = fullName,
            PasswordHash = passwordHasher.HashPassword("Password123!"),
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedMembershipsAsync(CancellationToken cancellationToken)
    {
        await AddUserTenantAsync(ManagerUserId, TenantOneId, cancellationToken);
        await AddUserTenantAsync(ManagerUserId, TenantTwoId, cancellationToken);
        await AddUserTenantAsync(ViewerUserId, TenantTwoId, cancellationToken);

        await AddUserRoleAsync(AdminUserId, SystemAdminRoleId, null, cancellationToken);
        await AddUserRoleAsync(ManagerUserId, AgentManagerRoleId, TenantOneId, cancellationToken);
        await AddUserRoleAsync(ManagerUserId, AgentViewerRoleId, TenantTwoId, cancellationToken);
        await AddUserRoleAsync(ViewerUserId, AgentViewerRoleId, TenantTwoId, cancellationToken);
    }

    private async Task AddUserTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserTenants.AnyAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken))
        {
            return;
        }

        dbContext.UserTenants.Add(new UserTenant
        {
            Id = StableGuid("user-tenant", $"{userId}:{tenantId}"),
            UserId = userId,
            TenantId = tenantId,
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddUserRoleAsync(Guid userId, Guid roleId, Guid? tenantId, CancellationToken cancellationToken)
    {
        if (await dbContext.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId && x.TenantId == tenantId, cancellationToken))
        {
            return;
        }

        dbContext.UserRoles.Add(new UserRole
        {
            Id = StableGuid("user-role", $"{userId}:{roleId}:{tenantId}"),
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAgentsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Agents.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Agents.Add(new Agent
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
            TenantId = TenantOneId,
            Name = "Demo Support Agent",
            Description = "Demo agent for permission checks.",
            Status = AgentStatus.Active,
            Role = "Support",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid StableGuid(string scope, string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{scope}:{value}"));
        return new Guid(bytes);
    }
}
