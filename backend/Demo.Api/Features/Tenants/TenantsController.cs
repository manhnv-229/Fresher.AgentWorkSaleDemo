using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.Authorization;
using Demo.Application.Features.Tenants;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Features.Tenants;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(ITenantCatalogService tenantCatalogService) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.TenantView)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetTenants(CancellationToken cancellationToken)
    {
        var result = await tenantCatalogService.GetTenantsAsync(cancellationToken);
        return Ok(result.Value ?? []);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.TenantCreate)]
    public async Task<ActionResult<object>> CreateTenant(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantCatalogService.CreateTenantAsync(
            new CreateTenantCommand(request.Name, request.Code),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? TenantErrorCodes.ValidationError,
                result.ErrorMessage ?? "Name and code are required."));
        }

        return CreatedAtAction(nameof(GetTenants), new { id = result.Value.Id }, result.Value);
    }
}
