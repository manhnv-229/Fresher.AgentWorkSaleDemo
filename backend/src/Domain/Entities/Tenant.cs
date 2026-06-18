using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class Tenant
{
    public Guid Id
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? ModifiedAt
    {
        get; set;
    }

    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
