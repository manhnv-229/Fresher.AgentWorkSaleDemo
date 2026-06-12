namespace Demo.Api.Middlewares.Authorization;

public sealed class TenantContextResolver(IHttpContextAccessor httpContextAccessor)
{
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
