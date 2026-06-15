namespace Demo.Application.DTOs;

public sealed record AgentListItem(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    string Role,
    string Scope,
    string Status);

public sealed record CreateAgentCommand(string Name, string Role, string? Description, string? Icon);
