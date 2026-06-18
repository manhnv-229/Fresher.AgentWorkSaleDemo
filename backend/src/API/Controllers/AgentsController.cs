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

[ApiController]
[Route("api/tenants/{tenantId:guid}/agents")]
public sealed class AgentsController(IAgentCatalogService agentService) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.AgentView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetAgents(
        Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetTenantAgentsPagedAsync(
            tenantId,
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
    public async Task<ActionResult<object>> GetAgentDetail(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetTenantAgentDetailAsync(tenantId, agentId, cancellationToken);

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
    public async Task<ActionResult<object>> CreateAgent(Guid tenantId, CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.CreateTenantAgentAsync(
            tenantId,
            userId.Value,
            new CreateAgentCommand(request.Name, request.Role, request.Description, request.Icon),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            if (result.ErrorCode == AgentErrorCodes.TenantNotFound)
            {
                return NotFound(new ApiErrorResponse(
                    result.ErrorCode,
                    result.ErrorMessage ?? "Tenant was not found."));
            }

            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? AgentErrorCodes.ValidationError,
                result.ErrorMessage ?? "Name and role are required."));
        }

        return CreatedAtAction(nameof(GetAgentDetail), new { tenantId, agentId = result.Value.Id }, result.Value);
    }

    [HttpPut("{agentId:guid}")]
    [HasPermission(PermissionCodes.AgentUpdate)]
    public async Task<ActionResult<object>> UpdateAgent(
        Guid tenantId,
        Guid agentId,
        UpdateAgentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.UpdateTenantAgentAsync(
            tenantId,
            agentId,
            userId.Value,
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
    public async Task<ActionResult> DeleteAgent(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.DeleteTenantAgentAsync(tenantId, agentId, userId.Value, cancellationToken);

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
}
