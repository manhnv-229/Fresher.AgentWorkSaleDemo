using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

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
}
