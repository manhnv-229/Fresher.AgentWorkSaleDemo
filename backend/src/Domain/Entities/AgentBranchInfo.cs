using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class AgentBranchInfo
{
    public Guid Id
    {
        get; set;
    }
    public Guid AgentId
    {
        get; set;
    }
    public string BranchName { get; set; } = string.Empty;
    public string? BranchPhoneNumber
    {
        get; set;
    }
    public string? Email
    {
        get; set;
    }
    public string? Address
    {
        get; set;
    }
    public string? Province
    {
        get; set;
    }
    public string? District
    {
        get; set;
    }
    public OpeningHourType OpeningHourType
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

    public Agent? Agent
    {
        get; set;
    }
}
