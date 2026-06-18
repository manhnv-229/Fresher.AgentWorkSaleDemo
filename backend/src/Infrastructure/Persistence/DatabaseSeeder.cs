using Demo.Domain.Authorization;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Service;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Persistence;

public sealed class DatabaseSeeder(DemoDbContext dbContext, IPasswordHasher passwordHasher)
{
    private static readonly Guid SystemAdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantOneId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid TenantTwoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantManagerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    private static readonly Guid StaffRoleId = Guid.Parse("33333333-3333-3333-3333-333333333302");
    private static readonly Guid AdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    private static readonly Guid TenantUserId = Guid.Parse("44444444-4444-4444-4444-444444444442");
    private static readonly Guid StaffUserId = Guid.Parse("44444444-4444-4444-4444-444444444443");
    private static readonly Guid InternalAgentId = Guid.Parse("55555555-5555-5555-5555-555555555550");
    private static readonly Guid TenantOneAgentId = Guid.Parse("55555555-5555-5555-5555-555555555551");
    private static readonly Guid TenantTwoAgentId = Guid.Parse("55555555-5555-5555-5555-555555555552");
    private static readonly Guid TenantOneRootFolderId = Guid.Parse("66666666-6666-6666-6666-666666666661");
    private static readonly Guid TenantOneGuideFileId = Guid.Parse("77777777-7777-7777-7777-777777777771");

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
        (PermissionCodes.DocumentView, "View documents", "Document"),
        (PermissionCodes.DocumentCreate, "Create documents", "Document"),
        (PermissionCodes.DocumentUpdate, "Update documents", "Document"),
        (PermissionCodes.DocumentDelete, "Delete documents", "Document"),
        (PermissionCodes.ReportView, "View reports", "Report"),
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
        await CleanLegacyDataAsync(cancellationToken);
        await SeedPermissionsAsync(cancellationToken);
        await SeedTenantsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedMembershipsAsync(cancellationToken);
        await SeedAgentsAsync(cancellationToken);
        await SeedKnowledgeAsync(cancellationToken);
        await SeedAuditLogsAsync(cancellationToken);
    }

    private async Task CleanLegacyDataAsync(CancellationToken cancellationToken)
    {
        // Remove old roles that are no longer used
        var legacyRoleCodes = new[] { "TenantAdmin", "AgentManager", "AgentViewer" };
        var legacyRoles = await dbContext.Roles
            .Where(x => legacyRoleCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        if (legacyRoles.Count > 0)
        {
            // Remove role permissions for legacy roles
            var legacyRoleIds = legacyRoles.Select(x => x.Id).ToList();
            var legacyRolePermissions = await dbContext.RolePermissions
                .Where(x => legacyRoleIds.Contains(x.RoleId))
                .ToListAsync(cancellationToken);
            dbContext.RolePermissions.RemoveRange(legacyRolePermissions);

            // Remove user roles for legacy roles
            var legacyUserRoles = await dbContext.UserRoles
                .Where(x => legacyRoleIds.Contains(x.RoleId))
                .ToListAsync(cancellationToken);
            dbContext.UserRoles.RemoveRange(legacyUserRoles);

            // Remove legacy roles
            dbContext.Roles.RemoveRange(legacyRoles);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
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
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        await AddRoleAsync(SystemAdminRoleId, null, "SystemAdmin", "SystemAdmin", "System administrator with full access", true, cancellationToken);
        await AddRoleAsync(TenantManagerRoleId, null, "Tenant", "Tenant", "Agent manager", true, cancellationToken);
        await AddRoleAsync(StaffRoleId, null, "Staff", "Staff", "Staff member with limited access", true, cancellationToken);
    }

    private async Task AddRoleAsync(
        Guid id,
        Guid? tenantId,
        string name,
        string code,
        string description,
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
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);

        // SystemAdmin: ALL permissions
        await AddPermissionsAsync(SystemAdminRoleId, allPermissions.Select(x => x.Code), cancellationToken);

        // TenantManager: agent.*, document.*, user.*, report.view (no tenant.* or role.*)
        var tenantManagerPermissions = allPermissions
            .Where(x => x.GroupName is "Agent" or "Document" or "Report" or "User")
            .Select(x => x.Code);
        await AddPermissionsAsync(TenantManagerRoleId, tenantManagerPermissions, cancellationToken);

        // Staff: no permissions for now (empty set)
        await AddPermissionsAsync(StaffRoleId, [], cancellationToken);
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
        await AddUserAsync(TenantUserId, "tenant@example.com", "Tenant User", cancellationToken);
        await AddUserAsync(StaffUserId, "staff@example.com", "Staff User", cancellationToken);
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
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedMembershipsAsync(CancellationToken cancellationToken)
    {
        await AddUserTenantAsync(TenantUserId, TenantOneId, cancellationToken);
        await AddUserTenantAsync(TenantUserId, TenantTwoId, cancellationToken);
        await AddUserTenantAsync(StaffUserId, TenantOneId, cancellationToken);

        await AddUserRoleAsync(AdminUserId, SystemAdminRoleId, null, cancellationToken);
        await AddUserRoleAsync(TenantUserId, TenantManagerRoleId, TenantOneId, cancellationToken);
        await AddUserRoleAsync(StaffUserId, StaffRoleId, TenantOneId, cancellationToken);
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
            Status = MembershipStatus.Active,
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
        await AddAgentAsync(
            InternalAgentId,
            null,
            "AVA Internal Ticket",
            "Admin-only internal support agent for operations.",
            "Operations",
            AgentScope.Internal,
            cancellationToken);

        await AddAgentAsync(
            TenantOneAgentId,
            TenantOneId,
            "Demo Support Agent",
            "Demo tenant agent for permission checks.",
            "Support",
            AgentScope.Tenant,
            cancellationToken);

        await AddAgentAsync(
            TenantTwoAgentId,
            TenantTwoId,
            "Tenant Two Guide",
            "Demo tenant guide agent for sidebar switching checks.",
            "Guide",
            AgentScope.Tenant,
            cancellationToken);
    }

    private async Task AddAgentAsync(
        Guid id,
        Guid? tenantId,
        string name,
        string description,
        string role,
        AgentScope scope,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Agents.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.Agents.Add(new Agent
        {
            Id = id,
            TenantId = tenantId,
            CreatedByUserId = AdminUserId,
            Code = CreateAgentCode(name, id),
            Name = name,
            Description = description,
            Scope = scope,
            Status = AgentStatus.Active,
            Role = role,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedKnowledgeAsync(CancellationToken cancellationToken)
    {
        await AddKnowledgeFolderAsync(TenantOneRootFolderId, TenantOneAgentId, null, AdminUserId, "Guides", cancellationToken);
        await AddKnowledgeFileAsync(
            TenantOneGuideFileId,
            TenantOneAgentId,
            TenantOneRootFolderId,
            AdminUserId,
            "tenant-one-guide",
            "Tenant One Guide.pdf",
            "seed/tenant-one-guide.pdf",
            "application/pdf",
            ".pdf",
            1024L,
            cancellationToken);
    }

    private async Task AddKnowledgeFolderAsync(
        Guid id,
        Guid agentId,
        Guid? parentFolderId,
        Guid createdByUserId,
        string name,
        CancellationToken cancellationToken)
    {
        if (await dbContext.AgentKnowledgeFolders.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.AgentKnowledgeFolders.Add(new AgentKnowledgeFolder
        {
            Id = id,
            AgentId = agentId,
            ParentFolderId = parentFolderId,
            CreatedByUserId = createdByUserId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddKnowledgeFileAsync(
        Guid id,
        Guid agentId,
        Guid? folderId,
        Guid createdByUserId,
        string name,
        string originalName,
        string storageKey,
        string contentType,
        string? extension,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        if (await dbContext.AgentKnowledgeFiles.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.AgentKnowledgeFiles.Add(new AgentKnowledgeFile
        {
            Id = id,
            AgentId = agentId,
            FolderId = folderId,
            CreatedByUserId = createdByUserId,
            Name = name,
            OriginalName = originalName,
            ContentType = contentType,
            Extension = extension,
            StorageKey = storageKey,
            SizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAuditLogsAsync(CancellationToken cancellationToken)
    {
        await AddAuditLogAsync(
            Guid.Parse("88888888-8888-8888-8888-888888888881"),
            AdminUserId,
            null,
            "agent.create",
            "Admin User",
            "Agent",
            InternalAgentId.ToString(),
            "127.0.0.1",
            "Seeded internal agent for demo access.",
            cancellationToken);
    }

    private async Task AddAuditLogAsync(
        Guid id,
        Guid? userId,
        Guid? tenantId,
        string action,
        string userName,
        string? targetType,
        string? targetId,
        string? ipAddress,
        string description,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Set<AuditLogEntry>().AnyAsync(x => x.Id == id, cancellationToken))
        {
            return;
        }

        dbContext.Set<AuditLogEntry>().Add(new AuditLogEntry
        {
            Id = id,
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            UserName = userName,
            TargetType = targetType,
            TargetId = targetId,
            IPAddress = ipAddress,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid StableGuid(string scope, string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{scope}:{value}"));
        return new Guid(bytes);
    }

    private static string CreateAgentCode(string name, Guid id)
    {
        var normalized = new string(name.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        var prefix = string.IsNullOrWhiteSpace(normalized) ? "AGENT" : normalized[..Math.Min(normalized.Length, 10)];
        return $"{prefix}-{id.ToString("N")[..8]}";
    }
}
