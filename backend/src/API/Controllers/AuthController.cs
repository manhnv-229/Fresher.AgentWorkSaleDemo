using System.Security.Claims;

using Demo.Api.Common;
using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Interfaces.Service;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
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

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Resolve(HttpContext, request?.RefreshToken);
        await authService.LogoutAsync(new LogoutRequest(refreshToken), ClientIp(), cancellationToken);
        RefreshTokenCookie.Delete(HttpContext);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
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

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
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

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
