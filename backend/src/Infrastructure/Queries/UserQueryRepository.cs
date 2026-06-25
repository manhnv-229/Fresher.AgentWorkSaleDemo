using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

public sealed class UserQueryRepository(IDbConnectionFactory connectionFactory) : IUserQueryRepository
{
    public async Task<IReadOnlyList<AdminUserSummaryRow>> GetFilteredAsync(
        string? search,
        string? status,
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

        sql.Append(" ORDER BY email");

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AdminUserSummaryRow>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
