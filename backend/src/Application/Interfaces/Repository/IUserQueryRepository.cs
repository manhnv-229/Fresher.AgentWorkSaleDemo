using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Interfaces.Repository;

public interface IUserQueryRepository
{
    Task<PagedResult<AdminUserSummaryRow>> GetFilteredAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
