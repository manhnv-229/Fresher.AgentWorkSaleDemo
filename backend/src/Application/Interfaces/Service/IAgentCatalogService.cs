using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Domain.Interfaces.Service;

public interface IAgentCatalogService
{
    Task<ServiceResult<PagedResult<AgentListItem>>> GetInternalAgentsPagedAsync(
        AgentListFilters filters,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResult<AgentListItem>>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentListFilters filters,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentDetailItem>> GetInternalAgentDetailAsync(
        Guid agentId,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentDetailItem>> GetTenantAgentDetailAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentListItem>> CreateInternalAgentAsync(
        Guid createdByUserId,
        string? ipAddress,
        CreateAgentCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentListItem>> CreateTenantAgentAsync(
        Guid tenantId,
        Guid createdByUserId,
        string? ipAddress,
        CreateAgentCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentListItem>> UpdateInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        UpdateAgentCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<AgentListItem>> UpdateTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        UpdateAgentCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
