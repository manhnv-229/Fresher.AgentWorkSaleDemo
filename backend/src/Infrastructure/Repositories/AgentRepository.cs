using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

/// <summary>
/// Quản lý thao tác ghi và truy vấn agent trong DbContext.
/// Repository hỗ trợ agent nội bộ và agent thuộc tenant với các ràng buộc scope tương ứng.
/// </summary>
public sealed class AgentRepository(DemoDbContext dbContext) : IAgentRepository
{
    /// <summary>
    /// Lấy danh sách agent nội bộ theo bộ lọc và phân trang.
    /// <param name="filters">Bộ lọc trạng thái, tìm kiếm và phân trang.</param>
    /// <returns>Kết quả phân trang agent nội bộ.</returns>
    /// </summary>
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

    /// <summary>
    /// Lấy danh sách agent của tenant theo bộ lọc và phân trang.
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="filters">Bộ lọc trạng thái, tìm kiếm và phân trang.</param>
    /// <returns>Kết quả phân trang agent của tenant.</returns>
    /// </summary>
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

    /// <summary>
    /// Lấy chi tiết agent nội bộ ở chế độ chỉ đọc.
    /// <param name="agentId">Định danh agent cần lấy.</param>
    /// <returns>Agent tương ứng hoặc null nếu không tồn tại.</returns>
    /// </summary>
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

    /// <summary>
    /// Lấy chi tiết agent thuộc tenant ở chế độ chỉ đọc.
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent cần lấy.</param>
    /// <returns>Agent tương ứng hoặc null nếu không tồn tại.</returns>
    /// </summary>
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

    /// <summary>
    /// Lấy agent nội bộ ở chế độ tracking để phục vụ cập nhật.
    /// <param name="agentId">Định danh agent cần lấy.</param>
    /// <returns>Agent đang được tracking hoặc null nếu không tồn tại.</returns>
    /// </summary>
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

    /// <summary>
    /// Lấy agent tenant ở chế độ tracking để phục vụ cập nhật.
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent cần lấy.</param>
    /// <returns>Agent đang được tracking hoặc null nếu không tồn tại.</returns>
    /// </summary>
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

    /// <summary>
    /// Đăng ký agent mới vào DbContext để lưu trong unit of work.
    /// <param name="agent">Entity agent cần thêm.</param>
    /// </summary>
    public void Add(Agent agent)
    {
        dbContext.Agents.Add(agent);
    }

    /// <summary>
    /// Đăng ký agent bị xóa trong DbContext.
    /// <param name="agent">Entity agent cần xóa.</param>
    /// </summary>
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
