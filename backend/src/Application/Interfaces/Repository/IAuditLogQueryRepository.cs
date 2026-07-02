using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Interfaces.Repository;

public interface IAuditLogQueryRepository
{
    Task<PagedResult<AuditLogEntryRow>> GetFilteredAsync(
        string? search,
        DateTime? createdDateFrom,
        DateTime? createdDateTo,
        IReadOnlyList<string>? actions,
        IReadOnlyList<string>? targetTypes,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
