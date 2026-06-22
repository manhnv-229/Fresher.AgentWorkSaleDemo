using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.AuditLogView)]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntryResponse>>> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] string? timePreset,
        [FromQuery] string[]? actions,
        [FromQuery] string[]? targetTypes,
        CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilterRequest(
            search,
            timePreset,
            actions?.ToList(),
            targetTypes?.ToList());

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
