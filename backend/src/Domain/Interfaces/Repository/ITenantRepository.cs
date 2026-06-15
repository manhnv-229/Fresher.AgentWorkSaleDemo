namespace Demo.Domain.Interfaces.Repository;

public interface ITenantRepository
{
    Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken);
}
