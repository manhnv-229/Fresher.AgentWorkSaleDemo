using Demo.Domain.Interfaces.Service;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Quản lý version token namespace bằng Redis để invalidation theo nhóm cache key.
/// </summary>
public sealed class CacheVersionService(
    IDistributedCacheService distributedCacheService,
    IApplicationCachePolicyProvider cachePolicyProvider) : ICacheVersionService
{
    /// <summary>
    /// Lấy version token hiện hành của namespace cache.
    /// </summary>
    public async Task<string> GetVersionAsync(string namespaceKey, CancellationToken cancellationToken)
    {
        var version = await distributedCacheService.GetAsync<string>(namespaceKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        version = CreateVersionToken();
        await distributedCacheService.SetAsync(
            namespaceKey,
            version,
            cachePolicyProvider.NamespaceVersionTimeToLive,
            cancellationToken);

        return version;
    }

    /// <summary>
    /// Làm mới version token để các key cũ không còn được dùng lại.
    /// </summary>
    public Task RefreshVersionAsync(string namespaceKey, CancellationToken cancellationToken)
    {
        return distributedCacheService.SetAsync(
            namespaceKey,
            CreateVersionToken(),
            cachePolicyProvider.NamespaceVersionTimeToLive,
            cancellationToken);
    }

    private static string CreateVersionToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}
