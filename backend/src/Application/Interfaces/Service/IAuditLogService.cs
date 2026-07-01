using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Domain.Interfaces.Service;

public interface IAuditLogService
{
    Task<ServiceResult<PagedResult<AuditLogEntryResponse>>> GetAuditLogsAsync(AuditLogFilterRequest? filter, CancellationToken cancellationToken);

    Task RecordAsync(
        string action,
        string userName,
        Guid? userId,
        Guid? tenantId,
        string? ipAddress,
        string description,
        string? targetType,
        string? targetId,
        CancellationToken cancellationToken);
}
