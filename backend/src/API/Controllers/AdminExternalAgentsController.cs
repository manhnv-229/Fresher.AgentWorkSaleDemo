using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

/// <summary>
/// Cung cấp endpoint quản trị danh sách agent thuộc tenant.
/// Controller nhận bộ lọc HTTP và chuyển việc phân trang cho agent catalog service.
/// </summary>
[ApiController]
[Route("api/admin/agents/external")]
public sealed class AdminExternalAgentsController(IAgentCatalogService agentService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách agent tenant theo bộ lọc và phân trang trong màn hình quản trị.
    /// <param name="status">Trạng thái agent cần lọc.</param>
    /// <param name="search">Từ khóa tìm kiếm theo thông tin agent.</param>
    /// <param name="page">Số trang bắt đầu từ một.</param>
    /// <param name="pageSize">Số agent tối đa trên một trang.</param>
    /// <returns>Danh sách agent đã phân trang hoặc lỗi validation.</returns>
    /// </summary>
    [HttpGet]
    [HasPermission(PermissionCodes.AgentView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetExternalAgents(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetExternalAgentsPagedAsync(
            new AgentListFilters(status, search, page, pageSize),
            cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? AgentErrorCodes.ValidationError,
            result.ErrorMessage ?? "Status filter is invalid."));
    }
}
