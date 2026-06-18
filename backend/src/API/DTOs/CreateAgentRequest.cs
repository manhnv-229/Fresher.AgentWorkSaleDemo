namespace Demo.Api.DTOs;

public sealed record CreateAgentRequest(string Name, string Role, string? Description, string? Icon);

public sealed record UpdateAgentRequest(string Name, string Role, string? Description, string? Icon, string Status);
