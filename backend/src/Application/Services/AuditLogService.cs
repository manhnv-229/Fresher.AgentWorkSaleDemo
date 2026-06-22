using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Domain.Entities;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services;

public sealed class AuditLogService(
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : IAuditLogService
{
    public async Task<ServiceResult<IReadOnlyList<AuditLogEntryResponse>>> GetAuditLogsAsync(
        AuditLogFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        ResolveTimePreset(filter?.TimePreset, out var dateFrom, out var dateTo);

        var entries = await auditLogRepository.GetFilteredAsync(
            filter?.Search,
            dateFrom,
            dateTo,
            filter?.Actions,
            filter?.TargetTypes,
            cancellationToken);
        var response = entries.Select(MapToResponse).ToList();
        return ServiceResult<IReadOnlyList<AuditLogEntryResponse>>.Success(response);
    }

    public async Task RecordAsync(
        string action,
        string userName,
        Guid? userId,
        Guid? tenantId,
        string? ipAddress,
        string description,
        string? targetType,
        string? targetId,
        CancellationToken cancellationToken)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Action = action,
            UserName = userName,
            UserId = userId,
            TenantId = tenantId,
            IPAddress = ipAddress,
            Description = description,
            TargetType = targetType,
            TargetId = targetId,
            CreatedAt = DateTime.UtcNow
        };

        auditLogRepository.Add(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ResolveTimePreset(string? preset, out DateTime? from, out DateTime? to)
    {
        from = null;
        to = null;
        if (string.IsNullOrWhiteSpace(preset))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        switch (preset)
        {
            case "today":
                from = today;
                to = today.AddDays(1);
                break;
            case "yesterday":
                from = today.AddDays(-1);
                to = today;
                break;
            case "this_week":
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                from = weekStart;
                to = weekStart.AddDays(7);
                break;
            case "last_week":
                var lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 7);
                from = lastWeekStart;
                to = lastWeekStart.AddDays(7);
                break;
            case "this_month":
                from = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                to = from.Value.AddMonths(1);
                break;
            case "last_month":
                var lastMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
                from = lastMonthStart;
                to = lastMonthStart.AddMonths(1);
                break;
            case "this_year":
                from = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                to = from.Value.AddYears(1);
                break;
            case "last_year":
                from = new DateTime(today.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                to = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                break;
        }
    }

    private static AuditLogEntryResponse MapToResponse(AuditLogEntry entry) =>
        new(
            entry.Id,
            entry.Action,
            entry.UserName,
            DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc),
            entry.TargetType,
            entry.Description);
}
