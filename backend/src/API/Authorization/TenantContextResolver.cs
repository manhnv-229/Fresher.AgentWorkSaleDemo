namespace Demo.Api.Authorization;

/// <summary>
/// Đọc tenant ID từ route hoặc header của HTTP context để phục vụ phân giải tenant hiện tại.
/// </summary>
public sealed class TenantContextResolver(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// Phân giải tenant ID hiện tại từ request.
    /// <returns>Tenant ID hợp lệ hoặc null nếu request không chứa tenant.</returns>
    /// </summary>
    public Guid? ResolveTenantId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out var routeTenantId))
        {
            return routeTenantId;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            Guid.TryParse(tenantHeader.ToString(), out var headerTenantId))
        {
            return headerTenantId;
        }

        return null;
    }
}
