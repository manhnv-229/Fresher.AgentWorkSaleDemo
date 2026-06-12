using Demo.Domain.Entities;

namespace Demo.Application.Features.Agents;

public interface IAgentRepository
{
    Task<IReadOnlyList<Agent>> GetInternalAgentsAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Agent>> GetTenantAgentsAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken);

    void Add(Agent agent);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
