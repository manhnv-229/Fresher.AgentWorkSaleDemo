using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

/// <summary>
/// Truy vấn sự tồn tại và thông tin tenant phục vụ các nghiệp vụ agent.
/// </summary>
public sealed class AgentTenantRepository(DemoDbContext dbContext) : ITenantRepository
{
    /// <summary>
    /// Kiểm tra tenant có tồn tại để xác thực scope nghiệp vụ.
    /// <param name="tenantId">Định danh tenant cần kiểm tra.</param>
    /// <returns>True nếu tenant tồn tại.</returns>
    /// </summary>
    public Task<bool> ExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }

    /// <summary>
    /// Lấy tenant theo định danh.
    /// <param name="tenantId">Định danh tenant cần lấy.</param>
    /// <returns>Tenant hoặc null nếu không tồn tại.</returns>
    /// </summary>
    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .FirstOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);
    }
}
