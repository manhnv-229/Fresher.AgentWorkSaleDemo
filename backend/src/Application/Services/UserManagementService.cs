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
    IUnitOfWork unitOfWork) : IUserManagementService
{
    public async Task<ServiceResult<IReadOnlyList<AdminUserSummary>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await authUserRepository.GetAllAsync(cancellationToken);
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
        return ServiceResult<AdminUserSummary>.Success(MapSummary(user));
    }

    private static AdminUserSummary MapSummary(User user)
    {
        return new AdminUserSummary(
            user.Id,
            user.Email,
            user.FullName,
            user.Status.ToString(),
            user.PasswordChangedAt,
            user.EmployeeCode,
            user.Project,
            user.JobPosition);
    }
}
