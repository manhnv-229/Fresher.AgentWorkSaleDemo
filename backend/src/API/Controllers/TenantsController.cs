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

    [HttpGet("{tenantId:guid}")]
    [HasPermission(PermissionCodes.TenantView)]
    public async Task<ActionResult<object>> GetTenantById(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await tenantCatalogService.GetTenantByIdAsync(tenantId, cancellationToken);

        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return NotFound(new ApiErrorResponse(
            result.ErrorCode ?? TenantErrorCodes.TenantNotFound,
            result.ErrorMessage ?? "Tenant not found."));
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

        return CreatedAtAction(nameof(GetTenantById), new
        {
            tenantId = result.Value.Id
        }, result.Value);
    }

    [HttpPut("{tenantId:guid}")]
    [HasPermission(PermissionCodes.TenantUpdate)]
    public async Task<ActionResult<object>> UpdateTenant(
        Guid tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tenantCatalogService.UpdateTenantAsync(
            tenantId,
            new UpdateTenantCommand(request.Name, request.Code),
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            if (result.ErrorCode == TenantErrorCodes.TenantNotFound)
            {
                return NotFound(new ApiErrorResponse(
                    result.ErrorCode,
                    result.ErrorMessage ?? "Tenant not found."));
            }

            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? TenantErrorCodes.ValidationError,
                result.ErrorMessage ?? "Invalid update request."));
        }

        return Ok(result.Value);
    }

    [HttpPost("{tenantId:guid}/lock")]
    [HasPermission(PermissionCodes.TenantUpdate)]
    public async Task<ActionResult<object>> LockTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await tenantCatalogService.LockTenantAsync(tenantId, cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            if (result.ErrorCode == TenantErrorCodes.TenantNotFound)
            {
                return NotFound(new ApiErrorResponse(
                    result.ErrorCode,
                    result.ErrorMessage ?? "Tenant not found."));
            }

            return BadRequest(new ApiErrorResponse(
                result.ErrorCode ?? TenantErrorCodes.InvalidStatusTransition,
                result.ErrorMessage ?? "Cannot lock tenant."));
        }

        return Ok(result.Value);
    }
}
