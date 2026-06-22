using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLogEntry>> GetFilteredAsync(
        string? search,
        DateTime? createdDateFrom,
        DateTime? createdDateTo,
        IReadOnlyList<string>? actions,
        IReadOnlyList<string>? targetTypes,
        CancellationToken cancellationToken);

    void Add(AuditLogEntry entry);
}
