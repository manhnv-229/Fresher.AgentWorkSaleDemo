using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

[ApiController]
[Route("api/admin/agents/external")]
public sealed class AdminExternalAgentsController(IAgentCatalogService agentService) : ControllerBase
{
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
