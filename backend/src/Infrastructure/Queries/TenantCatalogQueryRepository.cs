using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

public sealed class TenantCatalogQueryRepository(IDbConnectionFactory connectionFactory) : ITenantCatalogQueryRepository
{
    public async Task<IReadOnlyList<TenantListRow>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, name, code, status
            FROM tenants
            ORDER BY name
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<TenantListRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<TenantDetailRow?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, name, code, status, created_at AS CreatedAt, modified_at AS ModifiedAt
            FROM tenants
            WHERE id = @TenantId
            LIMIT 1
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<TenantDetailRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));
    }
}
