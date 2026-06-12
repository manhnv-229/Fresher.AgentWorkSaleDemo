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
[Route("api/tenants")]
public sealed class TenantsController(DemoDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.TenantView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetTenants(CancellationToken cancellationToken)
    {
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code, Status = x.Status.ToString() })
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.TenantCreate)]
    public async Task<ActionResult<object>> CreateTenant(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ApiErrorResponse("validation_error", "Name and code are required."));
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetTenants), new { id = tenant.Id }, new { tenant.Id, tenant.Name, tenant.Code, Status = tenant.Status.ToString() });
    }
}
