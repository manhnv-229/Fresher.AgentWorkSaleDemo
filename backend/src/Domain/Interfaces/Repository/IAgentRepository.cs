using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Repository;

public interface IAgentRepository
{
    Task<PagedResult<Agent>> GetInternalAgentsPagedAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken);

    Task<PagedResult<Agent>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken);

    Task<Agent?> GetInternalAgentDetailByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken);

    Task<Agent?> GetTenantAgentDetailByIdAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken);

    Task<Agent?> GetInternalAgentByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken);

    Task<Agent?> GetTenantAgentByIdAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken);

    void Add(Agent agent);

    void Remove(Agent agent);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
