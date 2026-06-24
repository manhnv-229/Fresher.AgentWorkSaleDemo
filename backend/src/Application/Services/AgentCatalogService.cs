using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using AutoMapper;

namespace Demo.Application.Services;

public sealed class AgentCatalogService(
    IAgentQueryRepository agentQueryRepository,
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAuditLogService auditLogService,
    IAuthUserRepository authUserRepository,
    IMapper mapper,
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

        var pagedResult = await agentQueryRepository.GetInternalAgentsPagedAsync(queryFilters, cancellationToken);
        var items = pagedResult.Items.Select(agent => mapper.Map<AgentListItem>(agent)).ToList();
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

        var pagedResult = await agentQueryRepository.GetTenantAgentsPagedAsync(tenantId, queryFilters, cancellationToken);
        var items = pagedResult.Items.Select(agent => mapper.Map<AgentListItem>(agent)).ToList();
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
        var agent = await agentQueryRepository.GetInternalAgentDetailByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentDetailItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        return ServiceResult<AgentDetailItem>.Success(mapper.Map<AgentDetailItem>(agent));
    }

    public async Task<ServiceResult<AgentDetailItem>> GetTenantAgentDetailAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var agent = await agentQueryRepository.GetTenantAgentDetailByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentDetailItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        return ServiceResult<AgentDetailItem>.Success(mapper.Map<AgentDetailItem>(agent));
    }

    public async Task<ServiceResult<AgentListItem>> CreateInternalAgentAsync(
        Guid createdByUserId,
        string? ipAddress,
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

        var userName = await GetUserNameAsync(createdByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.create",
            userName,
            createdByUserId,
            null,
            ipAddress,
            $"Internal agent '{agent.Name}' was created.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    public async Task<ServiceResult<AgentListItem>> CreateTenantAgentAsync(
        Guid tenantId,
        Guid createdByUserId,
        string? ipAddress,
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

        var userName = await GetUserNameAsync(createdByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.create",
            userName,
            createdByUserId,
            tenantId,
            ipAddress,
            $"Tenant agent '{agent.Name}' was created.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    public async Task<ServiceResult<AgentListItem>> UpdateInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
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

        var auditDescription = BuildAgentUpdateDescription("Internal agent", agent, command);
        UpdateAgentFromCommand(agent, command, modifiedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.update",
            userName,
            modifiedByUserId,
            null,
            ipAddress,
            auditDescription,
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    public async Task<ServiceResult<AgentListItem>> UpdateTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
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

        var auditDescription = BuildAgentUpdateDescription("Tenant agent", agent, command);
        UpdateAgentFromCommand(agent, command, modifiedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.update",
            userName,
            modifiedByUserId,
            tenantId,
            ipAddress,
            auditDescription,
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    public async Task<ServiceResult<bool>> DeleteInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
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

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.delete",
            userName,
            modifiedByUserId,
            null,
            ipAddress,
            $"Internal agent '{agent.Name}' was deleted.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
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

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.delete",
            userName,
            modifiedByUserId,
            tenantId,
            ipAddress,
            $"Tenant agent '{agent.Name}' was deleted.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private async Task<string> GetUserNameAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null) return "System";
        var user = await authUserRepository.GetByIdAsync(userId.Value, cancellationToken);
        return user?.FullName ?? user?.Email ?? "Unknown";
    }

    private static string BuildAgentUpdateDescription(string scopeLabel, Agent agent, UpdateAgentCommand command)
    {
        return AuditLogDescriptionBuilder.FormatChangeSummary(
            $"{scopeLabel} '{agent.Name}'",
            new AuditFieldChange("Name", agent.Name, command.Name),
            new AuditFieldChange("Role", agent.Role, command.Role),
            new AuditFieldChange("Description", agent.Description, command.Description),
            new AuditFieldChange("Icon", agent.Icon, command.Icon),
            new AuditFieldChange("Status", agent.Status.ToString(), command.Status));
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
