using Demo.Domain.Interfaces.Service;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Common;

/// <summary>
/// Gom các thao tác cache có fallback để service không lặp lại cùng một mẫu try/catch.
/// </summary>
public static class ApplicationCacheOperations
{
    /// <summary>
    /// Lấy version namespace cache. Trả về null khi Redis lỗi để service đọc dữ liệu từ nguồn chính.
    /// </summary>
    public static async Task<string?> TryGetVersionAsync(
        ICacheVersionService cacheVersionService,
        ILogger logger,
        string namespaceKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            return await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể lấy {CacheLabel} cache version.", cacheLabel);
            return null;
        }
    }

    /// <summary>
    /// Đọc cache typed. Trả về default khi Redis lỗi để không làm gián đoạn request nghiệp vụ.
    /// </summary>
    public static async Task<TValue?> TryGetAsync<TValue>(
        IDistributedCacheService distributedCacheService,
        ILogger logger,
        string cacheKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            return await distributedCacheService.GetAsync<TValue>(cacheKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể đọc {CacheLabel} cache.", cacheLabel);
            return default;
        }
    }

    /// <summary>
    /// Ghi cache typed. Redis lỗi chỉ được log warning vì dữ liệu chính đã được lấy thành công.
    /// </summary>
    public static async Task TrySetAsync<TValue>(
        IDistributedCacheService distributedCacheService,
        ILogger logger,
        string cacheKey,
        TValue value,
        TimeSpan timeToLive,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await distributedCacheService.SetAsync(cacheKey, value, timeToLive, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể ghi {CacheLabel} cache.", cacheLabel);
        }
    }

    /// <summary>
    /// Làm mới version namespace cache. Redis lỗi chỉ được log warning để mutation đã lưu không bị rollback giả.
    /// </summary>
    public static async Task TryRefreshVersionAsync(
        ICacheVersionService cacheVersionService,
        ILogger logger,
        string namespaceKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await cacheVersionService.RefreshVersionAsync(namespaceKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể invalidate {CacheLabel} cache.", cacheLabel);
        }
    }
}
