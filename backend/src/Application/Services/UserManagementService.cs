using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services;

public sealed class UserManagementService(
    IAuthUserRepository authUserRepository,
    IUserSessionRepository userSessionRepository,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork) : IUserManagementService
{
    public async Task<ServiceResult<IReadOnlyList<AdminUserSummary>>> GetUsersAsync(MemberListFilters? filters, CancellationToken cancellationToken)
    {
        var users = await authUserRepository.GetFilteredAsync(filters?.Search, filters?.Status, cancellationToken);
        return ServiceResult<IReadOnlyList<AdminUserSummary>>.Success(users.Select(MapSummary).ToList());
    }

    public Task<ServiceResult<AdminUserSummary>> LockUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        return SetUserStatusAsync(actorUserId, targetUserId, AccountStatus.Locked, ipAddress, "AccountLocked", cancellationToken);
    }

    public Task<ServiceResult<AdminUserSummary>> UnlockUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        return SetUserStatusAsync(actorUserId, targetUserId, AccountStatus.Active, ipAddress, "AccountUnlocked", cancellationToken);
    }

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
            var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            foreach (var session in activeSessions)
            {
                session.RevokedAt ??= DateTime.UtcNow;
                session.RevokedByIp = ipAddress;
                session.ReasonRevoked = revokeReason;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

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

        return ServiceResult<AdminUserSummary>.Success(MapSummary(user));
    }

    private static AdminUserSummary MapSummary(User user)
    {
        return new AdminUserSummary(
            user.Id,
            user.Email,
            user.FullName,
            user.Status.ToString(),
            user.EmployeeCode,
            user.Project,
            user.JobPosition);
    }

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

        return ServiceResult<AdminUserSummary>.Success(MapSummary(user));
    }
}
