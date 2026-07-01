using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

public sealed class UserQueryRepository(IDbConnectionFactory connectionFactory) : IUserQueryRepository
{
    public async Task<PagedResult<AdminUserSummaryRow>> GetFilteredAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("""
            SELECT id, email, full_name AS FullName, status, employee_code AS EmployeeCode, project, job_position AS JobPosition
            FROM users
            WHERE 1 = 1
            """);

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql.Append("""
                 AND LOWER(CONCAT_WS(' ', full_name, employee_code, email, project, job_position)) LIKE @Search
                """);
            parameters.Add("Search", $"%{search.Trim().ToLowerInvariant()}%");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql.Append(" AND status = @Status");
            parameters.Add("Status", status.Trim());
        }

        sql.Append("""
             ORDER BY email
             LIMIT @PageSize OFFSET @Offset;

             SELECT COUNT(*)
             FROM users
             WHERE 1 = 1
            """);

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql.Append("""
                 AND LOWER(CONCAT_WS(' ', full_name, employee_code, email, project, job_position)) LIKE @Search
                """);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql.Append(" AND status = @Status");
        }

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken));

        var rows = (await grid.ReadAsync<AdminUserSummaryRow>()).AsList();
        var totalCount = await grid.ReadSingleAsync<int>();

        return new PagedResult<AdminUserSummaryRow>(
            rows,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling((double)totalCount / pageSize));
    }
}
