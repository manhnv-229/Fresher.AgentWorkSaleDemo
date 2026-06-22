namespace Demo.Application.DTOs;

public sealed record AuditLogFilterRequest(
    string? Search,
    string? TimePreset,
    IReadOnlyList<string>? Actions);

public sealed record AuditLogEntryResponse(
    Guid Id,
    string Action,
    string UserName,
    DateTime CreatedAt,
    string? IpAddress,
    string Description);
