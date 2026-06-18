namespace Demo.Application.DTOs;

public sealed record TenantListItem(
    Guid Id,
    string Name,
    string Code,
    string Status);

public sealed record TenantDetailItem(
    Guid Id,
    string Name,
    string Code,
    string Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record CreateTenantCommand(string Name, string Code);

public sealed record UpdateTenantCommand(string Name, string Code);
