namespace Demo.Application.Features.Agents;

public interface ITenantRepository
{
    Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken);
}
