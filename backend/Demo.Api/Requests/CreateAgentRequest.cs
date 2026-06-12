namespace Demo.Api.Requests;

public sealed record CreateAgentRequest(string Name, string Role, string? Description);
