using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Domain.Interfaces.Service;

public interface IUserManagementService
{
    Task<ServiceResult<PagedResult<AdminUserSummary>>> GetUsersAsync(MemberListFilters? filters, CancellationToken cancellationToken);
    Task<ServiceResult<AdminUserSummary>> LockUserAsync(Guid actorUserId, Guid targetUserId, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<AdminUserSummary>> UnlockUserAsync(Guid actorUserId, Guid targetUserId, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<AdminUserSummary>> UpdateJobPositionAsync(Guid actorUserId, Guid targetUserId, string? jobPosition, string? ipAddress, CancellationToken cancellationToken);
}
