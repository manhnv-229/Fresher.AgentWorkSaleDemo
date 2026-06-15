namespace Demo.Application.Features.Tenants;

public sealed record TenantListItem(
    Guid Id,
    string Name,
    string Code,
    string Status);

public sealed record CreateTenantCommand(string Name, string Code);
