using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.Authorization;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Api.Features.Agents;

[ApiController]
[Route("api/tenants/{tenantId:guid}/agents")]
public sealed class AgentsController(DemoDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.AgentView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetAgents(
        Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NormalizeSearch(search);
        if (!TryParseStatus(status, out var parsedStatus))
        {
            return BadRequest(new ApiErrorResponse("validation_error", "Status filter is invalid."));
        }

        var query = dbContext.Agents
            .AsNoTracking()
            .Where(x => x.Scope == AgentScope.Tenant && x.TenantId == tenantId);

        if (parsedStatus is not null)
        {
            query = query.Where(x => x.Status == parsedStatus.Value);
        }

        if (normalizedSearch is not null)
        {
            query = query.Where(x =>
                x.Name.Contains(normalizedSearch) ||
                (x.Description != null && x.Description.Contains(normalizedSearch)) ||
                x.Role.Contains(normalizedSearch));
        }

        var agents = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.Icon,
                x.Role,
                Scope = x.Scope.ToString(),
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return Ok(agents);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.AgentCreate)]
    public async Task<ActionResult<object>> CreateAgent(Guid tenantId, CreateAgentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new ApiErrorResponse("validation_error", "Name and role are required."));
        }

        var tenantExists = await dbContext.Tenants.AnyAsync(x => x.Id == tenantId, cancellationToken);
        if (!tenantExists)
        {
            return NotFound(new ApiErrorResponse("tenant_not_found", "Tenant was not found."));
        }

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = AgentScope.Tenant,
            Name = request.Name.Trim(),
            Role = request.Role.Trim(),
            Description = request.Description?.Trim(),
            Icon = request.Icon?.Trim(),
            Status = AgentStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAgents), new { tenantId }, new
        {
            agent.Id,
            agent.Name,
            agent.Description,
            agent.Icon,
            agent.Role,
            Scope = agent.Scope.ToString(),
            Status = agent.Status.ToString()
        });
    }

    private static string? NormalizeSearch(string? search)
    {
        var normalized = search?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool TryParseStatus(string? status, out AgentStatus? parsedStatus)
    {
        parsedStatus = null;
        var normalized = status?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (Enum.TryParse<AgentStatus>(normalized, true, out var statusValue))
        {
            parsedStatus = statusValue;
            return true;
        }

        return false;
    }
}
