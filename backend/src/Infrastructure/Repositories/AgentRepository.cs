using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

public sealed class AgentRepository(DemoDbContext dbContext) : IAgentRepository
{
    public async Task<IReadOnlyList<Agent>> GetInternalAgentsAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(agent => agent.Scope == AgentScope.Internal);

        query = ApplyFilters(query, filters);

        return await query
            .OrderBy(agent => agent.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> GetTenantAgentsAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(agent => (agent.Scope == AgentScope.Tenant) && (agent.TenantId == tenantId));

        query = ApplyFilters(query, filters);

        return await query
            .OrderBy(agent => agent.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Agent agent)
    {
        dbContext.Agents.Add(agent);
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
