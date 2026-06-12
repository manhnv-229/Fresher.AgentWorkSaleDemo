using Demo.Api.Middlewares.Authorization;
using Demo.Api.Requests;
using Demo.Api.Responses;
using Demo.Application.Authorization;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/agents")]
public sealed class AgentsController(DemoDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.AgentView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetAgents(Guid tenantId, CancellationToken cancellationToken)
    {
        var agents = await dbContext.Agents
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description, x.Role, Status = x.Status.ToString() })
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
            Name = request.Name.Trim(),
            Role = request.Role.Trim(),
            Description = request.Description,
            Status = AgentStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAgents), new { tenantId }, new { agent.Id, agent.Name, agent.Description, agent.Role, Status = agent.Status.ToString() });
    }
}
