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
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetTenantAgentsAsync(
            tenantId,
            new AgentListFilters(status, search),
            cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? AgentErrorCodes.ValidationError,
            result.ErrorMessage ?? "Status filter is invalid."));
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

        return CreatedAtAction(nameof(GetAgents), new
        {
            tenantId
        }, result.Value);
    }

    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
