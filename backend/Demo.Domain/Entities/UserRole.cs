namespace Demo.Domain.Entities;

public sealed class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
    public Tenant? Tenant { get; set; }
}
