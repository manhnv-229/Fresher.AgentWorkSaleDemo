using Demo.Application.Features.Agents;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Agents;

public sealed class AgentRepository(DemoDbContext dbContext) : IAgentRepository
{
    public async Task<IReadOnlyList<Agent>> GetInternalAgentsAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(x => x.Scope == AgentScope.Internal);

        query = ApplyFilters(query, filters);

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> GetTenantAgentsAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(x => x.Scope == AgentScope.Tenant && x.TenantId == tenantId);

        query = ApplyFilters(query, filters);

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Agent agent)
    {
        dbContext.Agents.Add(agent);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Agent> ApplyFilters(IQueryable<Agent> query, AgentQueryFilters filters)
    {
        if (filters.Status is not null)
        {
            query = query.Where(x => x.Status == filters.Status.Value);
        }

        if (filters.Search is not null)
        {
            query = query.Where(x =>
                x.Name.Contains(filters.Search) ||
                (x.Description != null && x.Description.Contains(filters.Search)) ||
                x.Role.Contains(filters.Search));
        }

        return query;
    }
}
