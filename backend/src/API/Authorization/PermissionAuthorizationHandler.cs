using System.Security.Claims;

using Demo.Application.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Authorization;

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService,
    TenantContextResolver tenantContextResolver) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue("userId") ??
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var tenantId = tenantContextResolver.ResolveTenantId();
        if (await permissionService.HasPermissionAsync(userId, tenantId, requirement.PermissionCode, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
