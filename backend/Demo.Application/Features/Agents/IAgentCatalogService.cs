using Demo.Application.UseCases.Common;

namespace Demo.Application.Features.Agents;

public interface IAgentCatalogService
{
    Task<ServiceResult<IReadOnlyList<AgentListItem>>> GetInternalAgentsAsync(
        AgentListFilters filters,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<AgentListItem>>> GetTenantAgentsAsync(
        Guid tenantId,
        AgentListFilters filters,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentListItem>> CreateInternalAgentAsync(
        CreateAgentCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentListItem>> CreateTenantAgentAsync(
        Guid tenantId,
        CreateAgentCommand command,
        CancellationToken cancellationToken);
}
