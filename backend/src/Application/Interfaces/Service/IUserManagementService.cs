using Demo.Application.Common;
using Demo.Application.DTOs;

namespace Demo.Domain.Interfaces.Service;

public interface IUserManagementService
{
    Task<ServiceResult<IReadOnlyList<AdminUserSummary>>> GetUsersAsync(CancellationToken cancellationToken);
    Task<ServiceResult<AdminUserSummary>> LockUserAsync(Guid actorUserId, Guid targetUserId, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<AdminUserSummary>> UnlockUserAsync(Guid actorUserId, Guid targetUserId, string? ipAddress, CancellationToken cancellationToken);
}
