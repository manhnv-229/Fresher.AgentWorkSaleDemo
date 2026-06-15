using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class UserTenant
{
    public Guid Id
    {
        get; set;
    }
    public Guid UserId
    {
        get; set;
    }
    public Guid TenantId
    {
        get; set;
    }
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? ModifiedAt
    {
        get; set;
    }

    public User? User
    {
        get; set;
    }
    public Tenant? Tenant
    {
        get; set;
    }
}
