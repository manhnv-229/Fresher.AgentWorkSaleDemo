using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

/// <summary>
/// Truy vấn audit log theo bộ lọc và phân trang từ cơ sở dữ liệu.
/// </summary>
public sealed class AuditLogQueryRepository(IDbConnectionFactory connectionFactory) : IAuditLogQueryRepository
{
    public async Task<PagedResult<AuditLogEntryRow>> GetFilteredAsync(
        string? search,
        DateTime? createdDateFrom,
        DateTime? createdDateTo,
        IReadOnlyList<string>? actions,
        IReadOnlyList<string>? targetTypes,
        int page,
        int pageSize,
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

        sql.Append("""
             ORDER BY created_at DESC
             LIMIT @PageSize OFFSET @Offset;

             SELECT COUNT(*)
             FROM audit_logs
             WHERE 1 = 1
            """);

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql.Append("""
                 AND LOWER(CONCAT_WS(' ', action, user_name, description)) LIKE @Search
                """);
        }

        if (createdDateFrom.HasValue)
        {
            sql.Append(" AND created_at >= @CreatedDateFrom");
        }

        if (createdDateTo.HasValue)
        {
            sql.Append(" AND created_at < @CreatedDateTo");
        }

        if (actions is { Count: > 0 })
        {
            sql.Append(" AND action IN @Actions");
        }

        if (targetTypes is { Count: > 0 })
        {
            sql.Append(" AND target_type IN @TargetTypes");
        }

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken));

        var rows = (await grid.ReadAsync<AuditLogEntryRow>()).AsList();
        var totalCount = await grid.ReadSingleAsync<int>();

        return new PagedResult<AuditLogEntryRow>(
            rows,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / pageSize));
    }
}
