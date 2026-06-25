using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

public sealed class AuditLogQueryRepository(IDbConnectionFactory connectionFactory) : IAuditLogQueryRepository
{
    public async Task<IReadOnlyList<AuditLogEntryRow>> GetFilteredAsync(
        string? search,
        DateTime? createdDateFrom,
        DateTime? createdDateTo,
        IReadOnlyList<string>? actions,
        IReadOnlyList<string>? targetTypes,
        CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("""
            SELECT id, action, user_name AS UserName, created_at AS CreatedAt, target_type AS TargetType, description
            FROM audit_logs
            WHERE 1 = 1
            """);

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql.Append("""
                 AND LOWER(CONCAT_WS(' ', action, user_name, description)) LIKE @Search
                """);
            parameters.Add("Search", $"%{search.Trim().ToLowerInvariant()}%");
        }

        if (createdDateFrom.HasValue)
        {
            sql.Append(" AND created_at >= @CreatedDateFrom");
            parameters.Add("CreatedDateFrom", createdDateFrom.Value);
        }

        if (createdDateTo.HasValue)
        {
            sql.Append(" AND created_at < @CreatedDateTo");
            parameters.Add("CreatedDateTo", createdDateTo.Value);
        }

        if (actions is { Count: > 0 })
        {
            sql.Append(" AND action IN @Actions");
            parameters.Add("Actions", actions);
        }

        if (targetTypes is { Count: > 0 })
        {
            sql.Append(" AND target_type IN @TargetTypes");
            parameters.Add("TargetTypes", targetTypes);
        }

        sql.Append(" ORDER BY created_at DESC");

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AuditLogEntryRow>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
