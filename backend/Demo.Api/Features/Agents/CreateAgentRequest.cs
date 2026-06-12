namespace Demo.Api.Features.Agents;

public sealed record CreateAgentRequest(string Name, string Role, string? Description, string? Icon);
