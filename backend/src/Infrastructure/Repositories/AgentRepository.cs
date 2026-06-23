using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

public sealed class AgentRepository(DemoDbContext dbContext) : IAgentRepository
{
    public async Task<PagedResult<Agent>> GetInternalAgentsPagedAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(agent => agent.Scope == AgentScope.Internal);

        query = ApplyFilters(query, filters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(agent => agent.Name)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Agent>(
            items,
            filters.Page,
            filters.PageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / filters.PageSize));
    }

    public async Task<PagedResult<Agent>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(agent => agent.Scope == AgentScope.Tenant && agent.TenantId == tenantId);

        query = ApplyFilters(query, filters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(agent => agent.Name)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Agent>(
            items,
            filters.Page,
            filters.PageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / filters.PageSize));
    }

    public async Task<Agent?> GetInternalAgentDetailByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                agent => agent.Id == agentId
                    && agent.Scope == AgentScope.Internal
                    && agent.DeletedAt == null,
                cancellationToken);
    }

    public async Task<Agent?> GetTenantAgentDetailByIdAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                agent => agent.Id == agentId
                    && agent.Scope == AgentScope.Tenant
                    && agent.TenantId == tenantId
                    && agent.DeletedAt == null,
                cancellationToken);
    }

    public async Task<Agent?> GetInternalAgentByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Agents
            .FirstOrDefaultAsync(
                agent => agent.Id == agentId
                    && agent.Scope == AgentScope.Internal
                    && agent.DeletedAt == null,
                cancellationToken);
    }

    public async Task<Agent?> GetTenantAgentByIdAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Agents
            .FirstOrDefaultAsync(
                agent => agent.Id == agentId
                    && agent.Scope == AgentScope.Tenant
                    && agent.TenantId == tenantId
                    && agent.DeletedAt == null,
                cancellationToken);
    }

    public void Add(Agent agent)
    {
        dbContext.Agents.Add(agent);
    }

    public void Remove(Agent agent)
    {
        dbContext.Agents.Remove(agent);
    }

    private static IQueryable<Agent> ApplyFilters(IQueryable<Agent> query, AgentQueryFilters filters)
    {
        if (filters.Status is not null)
        {
            query = query.Where(agent => agent.Status == filters.Status.Value);
        }

        if (filters.Search is not null)
        {
            query = query.Where(agent =>
                agent.Code.Contains(filters.Search) ||
                agent.Name.Contains(filters.Search) ||
                ((agent.Description != null) && agent.Description.Contains(filters.Search)) ||
                agent.Role.Contains(filters.Search));
        }

        return query;
    }
}
