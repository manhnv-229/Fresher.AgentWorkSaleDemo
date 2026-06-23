using Demo.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demo.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired();
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255);
        builder.Property(x => x.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50);
        builder.Property(x => x.Project).HasColumnName("project").HasMaxLength(255);
        builder.Property(x => x.JobPosition).HasColumnName("job_position").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        builder.HasIndex(x => x.Email).IsUnique();
    }
}

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class UserTenantConfiguration : IEntityTypeConfiguration<UserTenant>
{
    public void Configure(EntityTypeBuilder<UserTenant> builder)
    {
        builder.ToTable("user_tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(36);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.UserTenants).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Tenant).WithMany(x => x.UserTenants).HasForeignKey(x => x.TenantId);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(36);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.IsSystemRole).HasColumnName("is_system_role").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasOne(x => x.Tenant).WithMany(x => x.Roles).HasForeignKey(x => x.TenantId);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.GroupName).HasColumnName("group_name").HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.RoleId).HasColumnName("role_id").HasMaxLength(36);
        builder.Property(x => x.PermissionId).HasColumnName("permission_id").HasMaxLength(36);
        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => x.PermissionId);
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
        builder.Property(x => x.RoleId).HasColumnName("role_id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(36);
        builder.Property(x => x.AssignedAt).HasColumnName("assigned_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.UserId, x.RoleId, x.TenantId }).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        builder.HasOne(x => x.Tenant).WithMany(x => x.UserRoles).HasForeignKey(x => x.TenantId);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
        builder.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(36);
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(100);
        builder.Property(x => x.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(100);
        builder.Property(x => x.ReasonRevoked).HasColumnName("reason_revoked").HasMaxLength(255);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.TokenHash);
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Session).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.SessionId);
    }
}

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(100);
        builder.Property(x => x.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(100);
        builder.Property(x => x.ReasonRevoked).HasColumnName("reason_revoked").HasMaxLength(255);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasOne(x => x.User).WithMany(x => x.Sessions).HasForeignKey(x => x.UserId);
    }
}

internal sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("agents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(36);
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").HasMaxLength(36).IsRequired();
        builder.Property(x => x.ModifiedByUserId).HasColumnName("modified_by_user_id").HasMaxLength(36);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(500);
        builder.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.ModifiedAt).HasColumnName("modified_at");
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasOne(x => x.Tenant).WithMany(x => x.Agents).HasForeignKey(x => x.TenantId);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ModifiedByUser).WithMany().HasForeignKey(x => x.ModifiedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(36);
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(100);
        builder.Property(x => x.TargetId).HasColumnName("target_id").HasMaxLength(100);
        builder.Property(x => x.IPAddress).HasColumnName("ip_address").HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Action);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.SetNull);
    }
}
