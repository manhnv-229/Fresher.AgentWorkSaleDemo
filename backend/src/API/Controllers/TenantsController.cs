using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Api.DTOs;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

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

        return CreatedAtAction(nameof(GetTenants), new
        {
            id = result.Value.Id
        }, result.Value);
    }
}
