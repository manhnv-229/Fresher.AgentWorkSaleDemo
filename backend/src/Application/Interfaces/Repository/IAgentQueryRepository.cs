using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Interfaces.Repository;

public interface IAgentQueryRepository
{
    Task<PagedResult<AgentListRow>> GetInternalAgentsPagedAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken);

    Task<PagedResult<AgentListRow>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken);

    Task<AgentDetailRow?> GetInternalAgentDetailByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken);

    Task<AgentDetailRow?> GetTenantAgentDetailByIdAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken);
}
