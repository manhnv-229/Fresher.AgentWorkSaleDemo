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
    public async Task<ServiceResult<IReadOnlyList<AgentListItem>>> GetInternalAgentsAsync(
        AgentListFilters filters,
        CancellationToken cancellationToken)
    {
        if (!TryCreateQueryFilters(filters, out var queryFilters))
        {
            return ServiceResult<IReadOnlyList<AgentListItem>>.Failure(
                AgentErrorCodes.ValidationError,
                "Status filter is invalid.");
        }

        var agents = await agentRepository.GetInternalAgentsAsync(queryFilters, cancellationToken);
        return ServiceResult<IReadOnlyList<AgentListItem>>.Success(agents.Select(MapAgent).ToList());
    }

    public async Task<ServiceResult<IReadOnlyList<AgentListItem>>> GetTenantAgentsAsync(
        Guid tenantId,
        AgentListFilters filters,
        CancellationToken cancellationToken)
    {
        if (!TryCreateQueryFilters(filters, out var queryFilters))
        {
            return ServiceResult<IReadOnlyList<AgentListItem>>.Failure(
                AgentErrorCodes.ValidationError,
                "Status filter is invalid.");
        }

        var agents = await agentRepository.GetTenantAgentsAsync(tenantId, queryFilters, cancellationToken);
        return ServiceResult<IReadOnlyList<AgentListItem>>.Success(agents.Select(MapAgent).ToList());
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

        var tenantExists = await tenantRepository.ExistsAsync(tenantId, cancellationToken);
        if (!tenantExists)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        var agent = CreateAgent(command, tenantId, AgentScope.Tenant, createdByUserId);
        agentRepository.Add(agent);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AgentListItem>.Success(MapAgent(agent));
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

    private static bool TryCreateQueryFilters(AgentListFilters filters, out AgentQueryFilters queryFilters)
    {
        var hasValidStatus = TryParseStatus(filters.Status, out var status);
        queryFilters = new AgentQueryFilters(status, NormalizeSearch(filters.Search));
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
