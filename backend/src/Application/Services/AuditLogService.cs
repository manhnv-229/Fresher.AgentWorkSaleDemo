using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Domain.Entities;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using AutoMapper;

namespace Demo.Application.Services;

/// <summary>
/// Điều phối việc truy vấn và ghi audit log của các thao tác nghiệp vụ.
/// Service được controller sử dụng để lọc log phân trang và các service khác sử dụng để ghi lịch sử thay đổi.
/// </summary>
public sealed class AuditLogService(
    IAuditLogQueryRepository auditLogQueryRepository,
    IAuditLogRepository auditLogRepository,
    IMapper mapper,
    IUnitOfWork unitOfWork) : IAuditLogService
{
    #region Method

    /// <summary>
    /// Lấy danh sách audit log theo bộ lọc thời gian và hành động.
    /// <param name="filter">Bộ lọc tìm kiếm, thời gian, action, target type và phân trang.</param>
    /// <returns>Kết quả audit log đã phân trang.</returns>
    /// </summary>
    public async Task<ServiceResult<PagedResult<AuditLogEntryResponse>>> GetAuditLogsAsync(
        AuditLogFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        ResolveTimePreset(filter?.TimePreset, out var dateFrom, out var dateTo);
        var page = Math.Max(1, filter?.Page ?? 1);
        var pageSize = Math.Max(1, filter?.PageSize ?? 9);

        var entries = await auditLogQueryRepository.GetFilteredAsync(
            filter?.Search,
            dateFrom,
            dateTo,
            filter?.Actions,
            filter?.TargetTypes,
            page,
            pageSize,
            cancellationToken);
        var response = new PagedResult<AuditLogEntryResponse>(
            entries.Items.Select(entry => mapper.Map<AuditLogEntryResponse>(entry)).ToList(),
            entries.Page,
            entries.PageSize,
            entries.TotalCount,
            entries.TotalPages);
        return ServiceResult<PagedResult<AuditLogEntryResponse>>.Success(response);
    }

    /// <summary>
    /// Ghi một bản ghi audit log mới xuống cơ sở dữ liệu.
    /// <param name="action">Tên thao tác nghiệp vụ.</param>
    /// <param name="userName">Tên người thực hiện thao tác.</param>
    /// <param name="userId">Định danh người thực hiện, nếu có.</param>
    /// <param name="tenantId">Định danh tenant liên quan, nếu có.</param>
    /// <param name="ipAddress">Địa chỉ IP của client, nếu có.</param>
    /// <param name="description">Mô tả chi tiết thay đổi.</param>
    /// <param name="targetType">Loại đối tượng bị tác động.</param>
    /// <param name="targetId">Định danh đối tượng bị tác động.</param>
    /// </summary>
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

    /// <summary>
    /// Chuyển time preset từ API sang khoảng thời gian UTC phục vụ lọc dữ liệu.
    /// </summary>
    private static void ResolveTimePreset(string? preset, out DateTime? from, out DateTime? to)
    {
        from = null;
        to = null;
        if (string.IsNullOrWhiteSpace(preset))
        {
            return;
        }

        // Luôn tính toán theo UTC để kết quả lọc đồng nhất với thời gian lưu trong database.
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

    #endregion
}
