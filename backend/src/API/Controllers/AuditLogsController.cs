using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;
using Demo.Domain.Interfaces.Repository;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

/// <summary>
/// Cung cấp endpoint truy vấn audit log cho người dùng có quyền quản trị.
/// Controller chuyển các bộ lọc HTTP thành request cho audit log service và trả kết quả phân trang.
/// </summary>
[ApiController]
[Route("api/admin/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    /// <summary>
    /// Lấy audit log theo từ khóa, khoảng thời gian, action, target type và phân trang.
    /// <param name="search">Từ khóa tìm kiếm trong nội dung log.</param>
    /// <param name="timePreset">Khoảng thời gian định sẵn cần lọc.</param>
    /// <param name="actions">Danh sách action cần lọc.</param>
    /// <param name="targetTypes">Danh sách loại đối tượng cần lọc.</param>
    /// <param name="page">Số trang bắt đầu từ một.</param>
    /// <param name="pageSize">Số log tối đa trên một trang.</param>
    /// <returns>Kết quả audit log đã phân trang hoặc lỗi truy vấn.</returns>
    /// </summary>
    [HttpGet]
    [HasPermission(PermissionCodes.AuditLogView)]
    public async Task<ActionResult<PagedResult<AuditLogEntryResponse>>> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] string? timePreset,
        [FromQuery] string[]? actions,
        [FromQuery] string[]? targetTypes,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilterRequest(
            search,
            timePreset,
            actions?.ToList(),
            targetTypes?.ToList(),
            Math.Max(1, page ?? 1),
            Math.Max(1, pageSize ?? 9));

        var result = await auditLogService.GetAuditLogsAsync(filter, cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? "audit_log_error",
            result.ErrorMessage ?? "Could not load audit logs."));
    }
}
