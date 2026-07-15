using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Api.DTOs;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;

using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

/// <summary>
/// Cung cấp các endpoint quản trị agent nội bộ.
/// Controller chuyển request HTTP thành command cho service catalog và trả response theo contract API.
/// </summary>
[ApiController]
[Route("api/admin/agents/internal")]
public sealed class AdminAgentsController(IAgentCatalogService agentService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách agent nội bộ theo bộ lọc và phân trang.
    /// <param name="status">Trạng thái agent cần lọc.</param>
    /// <param name="search">Từ khóa tìm kiếm.</param>
    /// <param name="page">Số trang bắt đầu từ một.</param>
    /// <param name="pageSize">Số phần tử trên một trang.</param>
    /// <returns>Danh sách agent hoặc lỗi validation.</returns>
    /// </summary>
    [HttpGet]
    [HasPermission(PermissionCodes.AgentView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetInternalAgents(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetInternalAgentsPagedAsync(
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

    [HttpGet("{agentId:guid}")]
    [HasPermission(PermissionCodes.AgentView)]
    /// <summary>
    /// Lấy chi tiết một agent nội bộ theo định danh.
    /// <param name="agentId">Định danh agent cần xem.</param>
    /// <returns>Chi tiết agent hoặc lỗi không tìm thấy.</returns>
    /// </summary>
    public async Task<ActionResult<object>> GetInternalAgentDetail(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetInternalAgentDetailAsync(agentId, cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return NotFound(new ApiErrorResponse(
            result.ErrorCode ?? AgentErrorCodes.AgentNotFound,
            result.ErrorMessage ?? "Agent was not found."));
    }

    [HttpPost]
    [HasPermission(PermissionCodes.AgentCreate)]
    /// <summary>
    /// Tạo agent nội bộ từ dữ liệu request đã được bind và validate.
    /// <param name="request">Tên, vai trò và thông tin mô tả của agent.</param>
    /// <returns>Agent mới được tạo hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    public async Task<ActionResult<object>> CreateInternalAgent(CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.CreateInternalAgentAsync(
            userId.Value,
            ClientIp(),
            new CreateAgentCommand(request.Name, request.Role, request.Description, request.Icon),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? AgentErrorCodes.ValidationError,
                result.ErrorMessage ?? "Name and role are required."));
        }

        return CreatedAtAction(nameof(GetInternalAgentDetail), new { agentId = result.Value.Id }, result.Value);
    }

    [HttpPut("{agentId:guid}")]
    [HasPermission(PermissionCodes.AgentUpdate)]
    /// <summary>
    /// Cập nhật thông tin và trạng thái của agent nội bộ.
    /// <param name="agentId">Định danh agent cần cập nhật.</param>
    /// <param name="request">Dữ liệu mới của agent.</param>
    /// <returns>Agent sau cập nhật hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    public async Task<ActionResult<object>> UpdateInternalAgent(
        Guid agentId,
        UpdateAgentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.UpdateInternalAgentAsync(
            agentId,
            userId.Value,
            ClientIp(),
            new UpdateAgentCommand(request.Name, request.Role, request.Description, request.Icon, request.Status),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            if (result.ErrorCode == AgentErrorCodes.AgentNotFound)
            {
                return NotFound(new ApiErrorResponse(
                    result.ErrorCode,
                    result.ErrorMessage ?? "Agent was not found."));
            }

            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? AgentErrorCodes.ValidationError,
                result.ErrorMessage ?? "Invalid update request."));
        }

        return Ok(result.Value);
    }

    [HttpDelete("{agentId:guid}")]
    [HasPermission(PermissionCodes.AgentDelete)]
    /// <summary>
    /// Xóa mềm agent nội bộ và ghi nhận thao tác quản trị.
    /// <param name="agentId">Định danh agent cần xóa.</param>
    /// <returns>Không có nội dung khi xóa thành công.</returns>
    /// </summary>
    public async Task<ActionResult> DeleteInternalAgent(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.DeleteInternalAgentAsync(agentId, userId.Value, ClientIp(), cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorCode == AgentErrorCodes.AgentNotFound)
            {
                return NotFound(new ApiErrorResponse(
                    result.ErrorCode,
                    result.ErrorMessage ?? "Agent was not found."));
            }

            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? AgentErrorCodes.ValidationError,
                result.ErrorMessage ?? "Failed to delete agent."));
        }

        return NoContent();
    }

    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
