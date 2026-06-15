namespace Demo.Domain.Entities;

public sealed class AgentInstruction
{
    public Guid Id
    {
        get; set;
    }
    public Guid AgentId
    {
        get; set;
    }
    public string? PrePrompt
    {
        get; set;
    }
    public string? RegionalAccent
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? UpdatedAt
    {
        get; set;
    }

    public Agent? Agent
    {
        get; set;
    }
}
