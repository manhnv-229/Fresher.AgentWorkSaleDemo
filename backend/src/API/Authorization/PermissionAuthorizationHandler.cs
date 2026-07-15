using System.Security.Claims;

using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Authorization;

/// <summary>
/// Đánh giá yêu cầu permission bằng user claim, tenant context và permission service.
/// Handler được ASP.NET Core Authorization gọi khi endpoint có policy permission.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService,
    TenantContextResolver tenantContextResolver) : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// Kiểm tra permission của người dùng trong tenant hiện tại và đánh dấu requirement thành công khi hợp lệ.
    /// <param name="context">Context authorization chứa user và resource hiện tại.</param>
    /// <param name="requirement">Permission code endpoint yêu cầu.</param>
    /// <returns>Task hoàn tất sau khi requirement được đánh giá.</returns>
    /// </summary>
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
