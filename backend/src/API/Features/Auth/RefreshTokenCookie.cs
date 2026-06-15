using Demo.Application.Features.Auth;

namespace Demo.Api.Features.Auth;

internal static class RefreshTokenCookie
{
    public const string Name = "demo.refresh_token";

    public static void Append(HttpContext httpContext, AuthTokenResult tokenResult)
    {
        httpContext.Response.Cookies.Append(Name, tokenResult.RefreshToken, CreateOptions(httpContext, tokenResult.RefreshTokenExpiresAt));
    }

    public static void Delete(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(Name, CreateDeleteOptions(httpContext));
    }

    public static string? Resolve(HttpContext httpContext, string? requestToken)
    {
        if (!string.IsNullOrWhiteSpace(requestToken))
        {
            return requestToken;
        }

        return httpContext.Request.Cookies.TryGetValue(Name, out var cookieToken) ? cookieToken : null;
    }

    public static TokenResponse ToResponse(AuthTokenResult tokenResult)
    {
        return new TokenResponse(
            tokenResult.AccessToken,
            tokenResult.AccessTokenExpiresAt,
            tokenResult.RefreshTokenExpiresAt);
    }

    private static CookieOptions CreateOptions(HttpContext httpContext, DateTime expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookie(httpContext),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = new DateTimeOffset(expiresAt),
            MaxAge = expiresAt - DateTime.UtcNow
        };
    }

    private static CookieOptions CreateDeleteOptions(HttpContext httpContext)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookie(httpContext),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        };
    }

    private static bool ShouldUseSecureCookie(HttpContext httpContext)
    {
        return httpContext.Request.IsHttps || !httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
    }
}
