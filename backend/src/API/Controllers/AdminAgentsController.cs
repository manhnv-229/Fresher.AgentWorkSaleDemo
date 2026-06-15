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
[Route("api/admin/agents/internal")]
public sealed class AdminAgentsController(IAgentCatalogService agentService) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.AgentView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetInternalAgents(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await agentService.GetInternalAgentsAsync(
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
    public async Task<ActionResult<object>> CreateInternalAgent(CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await agentService.CreateInternalAgentAsync(
            userId.Value,
            new CreateAgentCommand(request.Name, request.Role, request.Description, request.Icon),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? AgentErrorCodes.ValidationError,
                result.ErrorMessage ?? "Name and role are required."));
        }

        return CreatedAtAction(nameof(GetInternalAgents), routeValues: null, value: result.Value);
    }

    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
