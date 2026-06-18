namespace Demo.Api.DTOs;

public sealed record CreateTenantRequest(string Name, string Code);

public sealed record UpdateTenantRequest(string Name, string Code);
