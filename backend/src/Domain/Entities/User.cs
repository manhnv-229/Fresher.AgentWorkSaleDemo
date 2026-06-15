using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class User
{
    public Guid Id
    {
        get; set;
    }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FullName
    {
        get; set;
    }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? UpdatedAt
    {
        get; set;
    }

    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
