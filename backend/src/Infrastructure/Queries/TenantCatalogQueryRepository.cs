using System.Text;

using Dapper;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Repository;

namespace Demo.Infrastructure.Queries;

/// <summary>
/// Truy vấn danh mục tenant và chi tiết tenant từ cơ sở dữ liệu.
/// </summary>
public sealed class TenantCatalogQueryRepository(IDbConnectionFactory connectionFactory) : ITenantCatalogQueryRepository
{
    /// <summary>
    /// Lấy toàn bộ tenant theo thứ tự hiển thị.
    /// <returns>Danh sách tenant.</returns>
    /// </summary>
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

    /// <summary>
    /// Lấy thông tin chi tiết tenant theo định danh.
    /// <param name="tenantId">Định danh tenant cần lấy.</param>
    /// <returns>Chi tiết tenant hoặc null nếu không tồn tại.</returns>
    /// </summary>
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
