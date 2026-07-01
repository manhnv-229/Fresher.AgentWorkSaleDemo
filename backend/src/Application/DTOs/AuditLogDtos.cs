namespace Demo.Application.DTOs;

public sealed record AuditLogFilterRequest(
    string? Search,
    string? TimePreset,
    IReadOnlyList<string>? Actions,
    IReadOnlyList<string>? TargetTypes,
    int Page,
    int PageSize);

public sealed record AuditLogEntryResponse(
    Guid Id,
    string Action,
    string UserName,
    DateTime CreatedAt,
    string? TargetType,
    string Description);
