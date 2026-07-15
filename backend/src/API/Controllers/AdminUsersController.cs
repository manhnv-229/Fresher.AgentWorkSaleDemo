using System.Security.Claims;

using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Authorization;
using Demo.Domain.Interfaces.Service;
using Demo.Domain.Interfaces.Repository;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

/// <summary>
/// Cung cấp các endpoint quản trị tài khoản người dùng.
/// Controller xử lý truy vấn, khóa, mở khóa và cập nhật chức vụ thông qua user management service.
/// </summary>
[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController(IUserManagementService userManagementService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách người dùng quản trị theo bộ lọc và phân trang.
    /// <param name="search">Từ khóa tìm kiếm người dùng.</param>
    /// <param name="status">Trạng thái tài khoản cần lọc.</param>
    /// <param name="page">Số trang bắt đầu từ một.</param>
    /// <param name="pageSize">Số người dùng tối đa trên một trang.</param>
    /// <returns>Danh sách người dùng đã phân trang hoặc lỗi truy vấn.</returns>
    /// </summary>
    [HttpGet]
    [HasPermission(PermissionCodes.UserView)]
    public async Task<ActionResult<PagedResult<AdminUserSummary>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var filters = new MemberListFilters(search, status, Math.Max(1, page ?? 1), Math.Max(1, pageSize ?? 9));
        var result = await userManagementService.GetUsersAsync(filters, cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? AuthErrorCodes.UserNotFound,
            result.ErrorMessage ?? "Could not load users."));
    }

    [HttpPost("{userId:guid}/lock")]
    [HasPermission(PermissionCodes.UserUpdate)]
    /// <summary>
    /// Khóa tài khoản người dùng được chỉ định.
    /// <param name="userId">Định danh người dùng cần khóa.</param>
    /// <returns>Thông tin người dùng sau cập nhật hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    public async Task<ActionResult<AdminUserSummary>> LockUser(Guid userId, CancellationToken cancellationToken)
    {
        return await UpdateUserStatusAsync(
            userId,
            (actorUserId, targetUserId) => userManagementService.LockUserAsync(actorUserId, targetUserId, ClientIp(), cancellationToken));
    }

    [HttpPost("{userId:guid}/unlock")]
    [HasPermission(PermissionCodes.UserUpdate)]
    /// <summary>
    /// Mở khóa tài khoản người dùng được chỉ định.
    /// <param name="userId">Định danh người dùng cần mở khóa.</param>
    /// <returns>Thông tin người dùng sau cập nhật hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    public async Task<ActionResult<AdminUserSummary>> UnlockUser(Guid userId, CancellationToken cancellationToken)
    {
        return await UpdateUserStatusAsync(
            userId,
            (actorUserId, targetUserId) => userManagementService.UnlockUserAsync(actorUserId, targetUserId, ClientIp(), cancellationToken));
    }

    [HttpPut("{userId:guid}/job-position")]
    [HasPermission(PermissionCodes.UserUpdate)]
    /// <summary>
    /// Cập nhật chức vụ của người dùng.
    /// <param name="userId">Định danh người dùng cần cập nhật.</param>
    /// <param name="request">Chức vụ mới của người dùng.</param>
    /// <returns>Thông tin người dùng sau cập nhật hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    public async Task<ActionResult<AdminUserSummary>> UpdateJobPosition(Guid userId, UpdateJobPositionRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId();
        if (actorUserId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await userManagementService.UpdateJobPositionAsync(actorUserId.Value, userId, request.JobPosition, ClientIp(), cancellationToken);
        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        if (result.ErrorCode == AuthErrorCodes.UserNotFound)
        {
            return NotFound(new ApiErrorResponse(result.ErrorCode, result.ErrorMessage ?? "User was not found."));
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? AuthErrorCodes.UserNotFound,
            result.ErrorMessage ?? "Could not update job position."));
    }

    private async Task<ActionResult<AdminUserSummary>> UpdateUserStatusAsync(
        Guid userId,
        Func<Guid, Guid, Task<Demo.Application.Common.ServiceResult<AdminUserSummary>>> action)
    {
        var actorUserId = CurrentUserId();
        if (actorUserId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await action(actorUserId.Value, userId);
        if (result.Succeeded && result.Value is not null)
        {
            return Ok(result.Value);
        }

        if (result.ErrorCode == AuthErrorCodes.UserNotFound)
        {
            return NotFound(new ApiErrorResponse(result.ErrorCode, result.ErrorMessage ?? "User was not found."));
        }

        return BadRequest(new ApiErrorResponse(
            result.ErrorCode ?? AuthErrorCodes.CannotLockSelf,
            result.ErrorMessage ?? "Could not update user status."));
    }

    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
