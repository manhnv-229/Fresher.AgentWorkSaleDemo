using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Repository;

public interface IAuditLogQueryRepository
{
    Task<IReadOnlyList<AuditLogEntryRow>> GetFilteredAsync(
        string? search,
        DateTime? createdDateFrom,
        DateTime? createdDateTo,
        IReadOnlyList<string>? actions,
        IReadOnlyList<string>? targetTypes,
        CancellationToken cancellationToken);
}
