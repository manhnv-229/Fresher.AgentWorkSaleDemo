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
    public string? EmployeeCode
    {
        get; set;
    }
    public string? Project
    {
        get; set;
    }
    public string? JobPosition
    {
        get; set;
    }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime? PasswordChangedAt
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? ModifiedAt
    {
        get; set;
    }

    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
