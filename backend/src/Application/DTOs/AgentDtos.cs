namespace Demo.Application.DTOs;

public sealed record AgentListItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? Icon,
    string Role,
    string Scope,
    string Status,
    Guid? TenantId = null,
    string? TenantName = null);

public sealed record AgentDetailItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? Icon,
    string Role,
    string Scope,
    string Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    DateTime? DeletedAt);

public sealed record CreateAgentCommand(string Name, string Role, string? Description, string? Icon);

public sealed record UpdateAgentCommand(string Name, string Role, string? Description, string? Icon, string Status);

public sealed record PagedRequest(int Page, int PageSize);
