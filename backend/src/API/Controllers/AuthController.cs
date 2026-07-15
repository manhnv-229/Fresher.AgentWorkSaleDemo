using System.Security.Claims;

using Demo.Api.Common;
using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

/// <summary>
/// Cung cấp các endpoint xác thực và quản lý phiên đăng nhập của người dùng.
/// Controller được sử dụng bởi client đăng nhập, làm mới token, đăng xuất và đổi mật khẩu.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    #region Method

    /// <summary>
    /// Đăng nhập và ghi refresh token về cookie HTTP-only khi xác thực thành công.
    /// <param name="request">Thông tin đăng nhập do client gửi.</param>
    /// <returns>Access token và thông tin hết hạn, hoặc lỗi xác thực.</returns>
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, ClientIp(), cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            RefreshTokenCookie.Append(HttpContext, result.Value);
            return Ok(RefreshTokenCookie.ToResponse(result.Value));
        }

        return Unauthorized(new ApiErrorResponse(result.ErrorCode ?? AuthErrorCodes.InvalidCredentials, result.ErrorMessage ?? "Login failed."));
    }

    /// <summary>
    /// Làm mới access token từ refresh token lấy ưu tiên từ cookie an toàn.
    /// <param name="request">Refresh token trong body nếu client không gửi cookie.</param>
    /// <returns>Access token mới và refresh token đã xoay vòng, hoặc lỗi phiên.</returns>
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Resolve(HttpContext, request?.RefreshToken);
        var result = await authService.RefreshAsync(new RefreshTokenRequest(refreshToken), ClientIp(), cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            RefreshTokenCookie.Append(HttpContext, result.Value);
            return Ok(RefreshTokenCookie.ToResponse(result.Value));
        }

        return Unauthorized(new ApiErrorResponse(result.ErrorCode ?? AuthErrorCodes.InvalidRefreshToken, result.ErrorMessage ?? "Refresh failed."));
    }

    /// <summary>
    /// Đăng xuất phiên hiện tại và xóa refresh token cookie phía trình duyệt.
    /// <param name="request">Refresh token trong body nếu cần dùng thay cho cookie.</param>
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Resolve(HttpContext, request?.RefreshToken);
        await authService.LogoutAsync(new LogoutRequest(refreshToken), ClientIp(), cancellationToken);
        RefreshTokenCookie.Delete(HttpContext);
        return NoContent();
    }

    /// <summary>
    /// Đổi mật khẩu cho người dùng đang đăng nhập.
    /// <param name="request">Mật khẩu hiện tại và mật khẩu mới.</param>
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        // Chấp nhận cả claim tùy biến lẫn NameIdentifier để tương thích với nhiều nguồn token.
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await authService.ChangePasswordAsync(userId, request, ClientIp(), cancellationToken);
        if (result.Succeeded)
        {
            RefreshTokenCookie.Delete(HttpContext);
            return NoContent();
        }

        if (result.ErrorCode == AuthErrorCodes.UserNotFound)
        {
            return NotFound(new ApiErrorResponse(result.ErrorCode, result.ErrorMessage ?? "User was not found."));
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? AuthErrorCodes.InvalidCurrentPassword,
            result.ErrorMessage ?? "Password change failed."));
    }

    /// <summary>
    /// Trả về hồ sơ cơ bản của người dùng hiện tại.
    /// <returns>Thông tin người dùng tương ứng với access token hiện tại.</returns>
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        // Chấp nhận cả claim tùy biến lẫn NameIdentifier để tương thích với nhiều nguồn token.
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return NotFound(new ApiErrorResponse(result.ErrorCode ?? AuthErrorCodes.UserNotFound, result.ErrorMessage ?? "User was not found."));
    }

    /// <summary>
    /// Lấy địa chỉ IP của client hiện tại để phục vụ audit và quản lý phiên.
    /// </summary>
    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    #endregion
}
