using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services;

public sealed class AgentCatalogService(
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork) : IAgentCatalogService
{
    public async Task<ServiceResult<PagedResult<AgentListItem>>> GetInternalAgentsPagedAsync(
        AgentListFilters filters,
        CancellationToken cancellationToken)
    {
        if (!TryCreateQueryFilters(filters, out var queryFilters))
        {
            return ServiceResult<PagedResult<AgentListItem>>.Failure(
                AgentErrorCodes.ValidationError,
                "Status filter is invalid.");
        }

        var pagedResult = await agentRepository.GetInternalAgentsPagedAsync(queryFilters, cancellationToken);
        var items = pagedResult.Items.Select(MapAgent).ToList();
        var result = new PagedResult<AgentListItem>(
            items,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalCount,
            pagedResult.TotalPages);

        return ServiceResult<PagedResult<AgentListItem>>.Success(result);
    }

    public async Task<ServiceResult<PagedResult<AgentListItem>>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentListFilters filters,
        CancellationToken cancellationToken)
    {
        if (!TryCreateQueryFilters(filters, out var queryFilters))
        {
            return ServiceResult<PagedResult<AgentListItem>>.Failure(
                AgentErrorCodes.ValidationError,
                "Status filter is invalid.");
        }

        var pagedResult = await agentRepository.GetTenantAgentsPagedAsync(tenantId, queryFilters, cancellationToken);
        var items = pagedResult.Items.Select(MapAgent).ToList();
        var result = new PagedResult<AgentListItem>(
            items,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalCount,
            pagedResult.TotalPages);

        return ServiceResult<PagedResult<AgentListItem>>.Success(result);
    }

    public async Task<ServiceResult<AgentDetailItem>> GetInternalAgentDetailAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetInternalAgentDetailByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentDetailItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        return ServiceResult<AgentDetailItem>.Success(MapAgentDetail(agent));
    }

    public async Task<ServiceResult<AgentDetailItem>> GetTenantAgentDetailAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentDetailByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentDetailItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        return ServiceResult<AgentDetailItem>.Success(MapAgentDetail(agent));
    }

    public async Task<ServiceResult<AgentListItem>> CreateInternalAgentAsync(
        Guid createdByUserId,
        CreateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        var agent = CreateAgent(command, tenantId: null, AgentScope.Internal, createdByUserId);
        agentRepository.Add(agent);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AgentListItem>.Success(MapAgent(agent));
    }

    public async Task<ServiceResult<AgentListItem>> CreateTenantAgentAsync(
        Guid tenantId,
        Guid createdByUserId,
        CreateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantLocked,
                "Cannot create agents in a locked tenant.");
        }

        var agent = CreateAgent(command, tenantId, AgentScope.Tenant, createdByUserId);
        agentRepository.Add(agent);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AgentListItem>.Success(MapAgent(agent));
    }

    public async Task<ServiceResult<AgentListItem>> UpdateInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        UpdateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetInternalAgentByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var validation = ValidateUpdateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        UpdateAgentFromCommand(agent, command, modifiedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AgentListItem>.Success(MapAgent(agent));
    }

    public async Task<ServiceResult<AgentListItem>> UpdateTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        UpdateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantLocked,
                "Cannot update agents in a locked tenant.");
        }

        var validation = ValidateUpdateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        UpdateAgentFromCommand(agent, command, modifiedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AgentListItem>.Success(MapAgent(agent));
    }

    public async Task<ServiceResult<bool>> DeleteInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetInternalAgentByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        agent.Status = AgentStatus.Deleted;
        agent.DeletedAt = DateTime.UtcNow;
        agent.ModifiedAt = DateTime.UtcNow;
        agent.ModifiedByUserId = modifiedByUserId;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.TenantLocked,
                "Cannot delete agents in a locked tenant.");
        }

        agent.Status = AgentStatus.Deleted;
        agent.DeletedAt = DateTime.UtcNow;
        agent.ModifiedAt = DateTime.UtcNow;
        agent.ModifiedByUserId = modifiedByUserId;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static Agent CreateAgent(CreateAgentCommand command, Guid? tenantId, AgentScope scope, Guid createdByUserId)
    {
        var id = Guid.NewGuid();
        return new Agent
        {
            Id = id,
            TenantId = tenantId,
            CreatedByUserId = createdByUserId,
            Code = CreateAgentCode(command.Name, id),
            Scope = scope,
            Name = command.Name.Trim(),
            Role = command.Role.Trim(),
            Description = command.Description?.Trim(),
            Icon = command.Icon?.Trim(),
            Status = AgentStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void UpdateAgentFromCommand(Agent agent, UpdateAgentCommand command, Guid modifiedByUserId)
    {
        agent.Name = command.Name.Trim();
        agent.Role = command.Role.Trim();
        agent.Description = command.Description?.Trim();
        agent.Icon = command.Icon?.Trim();
        agent.Status = Enum.Parse<AgentStatus>(command.Status, true);
        agent.ModifiedAt = DateTime.UtcNow;
        agent.ModifiedByUserId = modifiedByUserId;
    }

    private static string CreateAgentCode(string name, Guid id)
    {
        var normalized = new string(name.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        var prefix = string.IsNullOrWhiteSpace(normalized) ? "AGENT" : normalized[..Math.Min(normalized.Length, 10)];
        return $"{prefix}-{id.ToString("N")[..8]}";
    }

    private static AgentListItem MapAgent(Agent agent) =>
        new(
            agent.Id,
            agent.Code,
            agent.Name,
            agent.Description,
            agent.Icon,
            agent.Role,
            agent.Scope.ToString(),
            agent.Status.ToString());

    private static AgentDetailItem MapAgentDetail(Agent agent) =>
        new(
            agent.Id,
            agent.Code,
            agent.Name,
            agent.Description,
            agent.Icon,
            agent.Role,
            agent.Scope.ToString(),
            agent.Status.ToString(),
            agent.CreatedAt,
            agent.ModifiedAt,
            agent.DeletedAt);

    private static ServiceResult<AgentListItem>? ValidateCreateCommand(CreateAgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Role))
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.ValidationError,
                "Name and role are required.");
        }

        return null;
    }

    private static ServiceResult<AgentListItem>? ValidateUpdateCommand(UpdateAgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Role))
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.ValidationError,
                "Name and role are required.");
        }

        if (!Enum.TryParse<AgentStatus>(command.Status, true, out _))
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.InvalidStatus,
                "Status value is invalid.");
        }

        return null;
    }

    private static bool TryCreateQueryFilters(AgentListFilters filters, out AgentQueryFilters queryFilters)
    {
        var hasValidStatus = TryParseStatus(filters.Status, out var status);
        var page = filters.Page ?? 1;
        var pageSize = filters.PageSize ?? 20;
        queryFilters = new AgentQueryFilters(status, NormalizeSearch(filters.Search), page, pageSize);
        return hasValidStatus;
    }

    private static string? NormalizeSearch(string? search)
    {
        var normalized = search?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool TryParseStatus(string? status, out AgentStatus? parsedStatus)
    {
        parsedStatus = null;
        var normalized = status?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (Enum.TryParse<AgentStatus>(normalized, true, out var statusValue))
        {
            parsedStatus = statusValue;
            return true;
        }

        return false;
    }
}
