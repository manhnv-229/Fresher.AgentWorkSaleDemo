using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class Agent
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public AgentScope Scope { get; set; } = AgentScope.Tenant;
    public AgentStatus Status { get; set; } = AgentStatus.Draft;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public AgentBranchInfo? BranchInfo { get; set; }
    public AgentInstruction? Instruction { get; set; }
}
