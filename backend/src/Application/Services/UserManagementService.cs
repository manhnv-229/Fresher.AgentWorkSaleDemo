using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

public sealed class UserManagementService(
    IUserQueryRepository userQueryRepository,
    IAuthUserRepository authUserRepository,
    IUserSessionRepository userSessionRepository,
    IAuditLogService auditLogService,
    ICacheVersionService cacheVersionService,
    IMapper mapper,
    ILogger<UserManagementService> logger,
    IUnitOfWork unitOfWork) : IUserManagementService
{
    #region Method

    /// <summary>
    /// Lấy danh sách người dùng theo bộ lọc quản trị.
    /// </summary>
    public async Task<ServiceResult<PagedResult<AdminUserSummary>>> GetUsersAsync(MemberListFilters? filters, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, filters?.Page ?? 1);
        var pageSize = Math.Max(1, filters?.PageSize ?? 9);
        var users = await userQueryRepository.GetFilteredAsync(filters?.Search, filters?.Status, page, pageSize, cancellationToken);
        return ServiceResult<PagedResult<AdminUserSummary>>.Success(
            new PagedResult<AdminUserSummary>(
                users.Items.Select(user => mapper.Map<AdminUserSummary>(user)).ToList(),
                users.Page,
                users.PageSize,
                users.TotalCount,
                users.TotalPages));
    }

    /// <summary>
    /// Khóa tài khoản người dùng 
    /// </summary>
    public Task<ServiceResult<AdminUserSummary>> LockUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        return SetUserStatusAsync(actorUserId, targetUserId, AccountStatus.Locked, ipAddress, "AccountLocked", cancellationToken);
    }

    /// <summary>
    /// Mở khóa tài khoản người dùng 
    /// </summary>
    public Task<ServiceResult<AdminUserSummary>> UnlockUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        return SetUserStatusAsync(actorUserId, targetUserId, AccountStatus.Active, ipAddress, "AccountUnlocked", cancellationToken);
    }

    /// <summary>
    /// Cập nhật trạng thái tài khoản và đồng bộ các phiên đăng nhập liên quan.
    /// </summary>
    private async Task<ServiceResult<AdminUserSummary>> SetUserStatusAsync(
        Guid actorUserId,
        Guid targetUserId,
        AccountStatus targetStatus,
        string? ipAddress,
        string revokeReason,
        CancellationToken cancellationToken)
    {
        if (actorUserId == targetUserId && targetStatus == AccountStatus.Locked)
        {
            return ServiceResult<AdminUserSummary>.Failure(AuthErrorCodes.CannotLockSelf, "You cannot lock your own account.");
        }

        var user = await authUserRepository.GetForUpdateByIdAsync(targetUserId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<AdminUserSummary>.Failure(AuthErrorCodes.UserNotFound, "User was not found.");
        }

        var actorUser = await authUserRepository.GetByIdAsync(actorUserId, cancellationToken);
        var actorName = actorUser?.FullName ?? actorUser?.Email ?? "Unknown";

        user.Status = targetStatus;
        user.ModifiedAt = DateTime.UtcNow;

        if (targetStatus == AccountStatus.Locked)
        {
            // Khi khóa tài khoản, toàn bộ phiên còn hiệu lực phải bị thu hồi ngay để tránh tiếp tục sử dụng.
            var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            foreach (var session in activeSessions)
            {
                session.RevokedAt ??= DateTime.UtcNow;
                session.RevokedByIp = ipAddress;
                session.ReasonRevoked = revokeReason;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidatePermissionCacheAsync(user.Id, cancellationToken);

        var action = targetStatus == AccountStatus.Locked ? "user.lock" : "user.unlock";
        var description = targetStatus == AccountStatus.Locked
            ? $"Tài khoản '{user.FullName ?? user.Email}' đã bị khóa."
            : $"Tài khoản '{user.FullName ?? user.Email}' đã được mở khóa.";

        await auditLogService.RecordAsync(
            action,
            actorName,
            actorUserId,
            null,
            ipAddress,
            description,
            "User",
            user.Id.ToString(),
            cancellationToken);

        return ServiceResult<AdminUserSummary>.Success(mapper.Map<AdminUserSummary>(user));
    }

    /// <summary>
    /// Cập nhật chức danh công việc của người dùng và ghi audit log cho thay đổi đó.
    /// </summary>
    public async Task<ServiceResult<AdminUserSummary>> UpdateJobPositionAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? jobPosition,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await authUserRepository.GetForUpdateByIdAsync(targetUserId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<AdminUserSummary>.Failure(AuthErrorCodes.UserNotFound, "User was not found.");
        }

        var actorUser = await authUserRepository.GetByIdAsync(actorUserId, cancellationToken);
        var actorName = actorUser?.FullName ?? actorUser?.Email ?? "Unknown";

        var oldJobPosition = user.JobPosition;
        user.JobPosition = jobPosition;
        user.ModifiedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var description = AuditLogDescriptionBuilder.FormatChangeSummary(
            $"User '{user.FullName ?? user.Email}'",
            new AuditFieldChange("JobPosition", oldJobPosition, jobPosition));

        await auditLogService.RecordAsync(
            "user.update_job_position",
            actorName,
            actorUserId,
            null,
            ipAddress,
            description,
            "User",
            user.Id.ToString(),
            cancellationToken);

        return ServiceResult<AdminUserSummary>.Success(mapper.Map<AdminUserSummary>(user));
    }

    /// <summary>
    /// Invalidate permission cache của người dùng khi trạng thái tài khoản thay đổi.
    /// </summary>
    private async Task InvalidatePermissionCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await cacheVersionService.RefreshVersionAsync(ApplicationCacheKeys.PermissionNamespace(userId), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể invalidate permission cache của user {UserId}.", userId);
        }
    }

    #endregion
}
