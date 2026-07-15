using Demo.Application.DTOs;

namespace Demo.Api.Controllers;

/// <summary>
/// Helper quản lý refresh token cookie và ánh xạ token result thành response API.
/// Cookie được dùng trong các endpoint authentication để giảm việc truyền refresh token qua JavaScript.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "demo.refresh_token";

    /// <summary>
    /// Ghi refresh token vào HTTP-only cookie với thời hạn của token.
    /// <param name="httpContext">HTTP context cần ghi response cookie.</param>
    /// <param name="tokenResult">Kết quả token chứa refresh token và thời hạn.</param>
    /// </summary>
    public static void Append(HttpContext httpContext, AuthTokenResult tokenResult)
    {
        httpContext.Response.Cookies.Append(Name, tokenResult.RefreshToken, CreateOptions(httpContext, tokenResult.RefreshTokenExpiresAt));
    }

    /// <summary>
    /// Xóa refresh token cookie khỏi response.
    /// <param name="httpContext">HTTP context cần xóa cookie.</param>
    /// </summary>
    public static void Delete(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(Name, CreateDeleteOptions(httpContext));
    }

    /// <summary>
    /// Lấy refresh token ưu tiên từ body request, sau đó fallback sang cookie.
    /// <param name="httpContext">HTTP context chứa cookie của client.</param>
    /// <param name="requestToken">Refresh token được gửi trong body, nếu có.</param>
    /// <returns>Refresh token tìm được hoặc null nếu không có.</returns>
    /// </summary>
    public static string? Resolve(HttpContext httpContext, string? requestToken)
    {
        if (!string.IsNullOrWhiteSpace(requestToken))
        {
            return requestToken;
        }

        return httpContext.Request.Cookies.TryGetValue(Name, out var cookieToken) ? cookieToken : null;
    }

    /// <summary>
    /// Chuyển kết quả token nội bộ thành response contract của API.
    /// <param name="tokenResult">Kết quả token cần ánh xạ.</param>
    /// <returns>Response chứa access token và các thời điểm hết hạn.</returns>
    /// </summary>
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
