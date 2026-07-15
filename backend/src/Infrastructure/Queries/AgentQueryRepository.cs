using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

/// <summary>
/// Truy vấn dữ liệu agent theo scope, tenant, bộ lọc và phân trang.
/// Repository chỉ đọc dùng bởi application service để tạo danh sách và chi tiết agent.
/// </summary>
public sealed class AgentQueryRepository(IDbConnectionFactory connectionFactory) : IAgentQueryRepository
{
    public Task<PagedResult<AgentListRow>> GetInternalAgentsPagedAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        return GetAgentsPagedAsync(
            "agents.scope = 'Internal'",
            new DynamicParameters(),
            filters,
            cancellationToken);
    }

    public Task<PagedResult<AgentListRow>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);

        return GetAgentsPagedAsync(
            "agents.scope = 'Tenant' AND agents.tenant_id = @TenantId",
            parameters,
            filters,
            cancellationToken);
    }

    public Task<PagedResult<AgentListRow>> GetExternalAgentsPagedAsync(
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        return GetAgentsPagedAsync(
            "agents.scope = 'Tenant'",
            new DynamicParameters(),
            filters,
            cancellationToken,
            includeTenantColumns: true);
    }

    public async Task<AgentDetailRow?> GetInternalAgentDetailByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, code, name, description, icon, role, scope, status,
                   created_at AS CreatedAt, modified_at AS ModifiedAt, deleted_at AS DeletedAt
            FROM agents
            WHERE id = @AgentId
              AND scope = 'Internal'
              AND deleted_at IS NULL
            LIMIT 1
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AgentDetailRow>(
            new CommandDefinition(sql, new { AgentId = agentId }, cancellationToken: cancellationToken));
    }

    public async Task<AgentDetailRow?> GetTenantAgentDetailByIdAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, code, name, description, icon, role, scope, status,
                   created_at AS CreatedAt, modified_at AS ModifiedAt, deleted_at AS DeletedAt
            FROM agents
            WHERE id = @AgentId
              AND scope = 'Tenant'
              AND tenant_id = @TenantId
              AND deleted_at IS NULL
            LIMIT 1
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AgentDetailRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, AgentId = agentId }, cancellationToken: cancellationToken));
    }

    private async Task<PagedResult<AgentListRow>> GetAgentsPagedAsync(
        string scopeClause,
        DynamicParameters parameters,
        AgentQueryFilters filters,
        CancellationToken cancellationToken,
        bool includeTenantColumns = false)
    {
        var selectTenantColumns = includeTenantColumns
            ? ", agents.tenant_id AS TenantId, tenants.name AS TenantName"
            : string.Empty;
        var joinTenantClause = includeTenantColumns
            ? "LEFT JOIN tenants ON tenants.id = agents.tenant_id"
            : string.Empty;
        var filterClause = BuildFilterClause(filters, parameters);
        var sql = $$"""
            SELECT agents.id, agents.code, agents.name, agents.description, agents.icon, agents.role, agents.scope, agents.status{{selectTenantColumns}}
            FROM agents
            {{joinTenantClause}}
            WHERE {{scopeClause}}
              AND agents.deleted_at IS NULL{{filterClause}}
            ORDER BY agents.name
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*)
            FROM agents
            {{joinTenantClause}}
            WHERE {{scopeClause}}
              AND agents.deleted_at IS NULL{{filterClause}};
            """;

        parameters.Add("PageSize", filters.PageSize);
        parameters.Add("Offset", (filters.Page - 1) * filters.PageSize);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var items = (await grid.ReadAsync<AgentListRow>()).AsList();
        var totalCount = await grid.ReadSingleAsync<int>();

        return new PagedResult<AgentListRow>(
            items,
            filters.Page,
            filters.PageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / filters.PageSize));
    }

    private static string BuildFilterClause(AgentQueryFilters filters, DynamicParameters parameters)
    {
        var sql = new StringBuilder();

        if (filters.Status is not null)
        {
            sql.Append(" AND status = @Status");
            parameters.Add("Status", filters.Status.Value.ToString());
        }

        if (filters.Search is not null)
        {
            sql.Append("""
                 AND (
                    code LIKE @Search
                    OR name LIKE @Search
                    OR description LIKE @Search
                    OR role LIKE @Search
                 )
                """);
            parameters.Add("Search", $"%{filters.Search}%");
        }

        return sql.ToString();
    }
}
