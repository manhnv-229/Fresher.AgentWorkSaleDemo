namespace Demo.Domain.Entities;

public sealed class AuditLogEntry
{
    public Guid Id
    {
        get; set;
    }
    public Guid? UserId
    {
        get; set;
    }
    public Guid? TenantId
    {
        get; set;
    }
    public string Action { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? TargetType
    {
        get; set;
    }
    public string? TargetId
    {
        get; set;
    }
    public string? IPAddress
    {
        get; set;
    }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt
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
