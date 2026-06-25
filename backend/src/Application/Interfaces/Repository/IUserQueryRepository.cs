using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Repository;

public interface IUserQueryRepository
{
    Task<IReadOnlyList<AdminUserSummaryRow>> GetFilteredAsync(string? search, string? status, CancellationToken cancellationToken);
}
