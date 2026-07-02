using System.Text.Json;

using Demo.Domain.Interfaces.Service;

using Microsoft.Extensions.Caching.Distributed;

namespace Demo.Infrastructure.Services;

public sealed class RedisDistributedCacheService(IDistributedCache distributedCache) : IDistributedCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Lấy dữ liệu cache và giải tuần tự về kiểu yêu cầu.
    /// </summary>
    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken)
    {
        var payload = await distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TValue>(payload, SerializerOptions);
    }

    /// <summary>
    /// Lưu dữ liệu cache typed với thời gian sống tương đối.
    /// </summary>
    public async Task SetAsync<TValue>(string key, TValue value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        if (timeToLive <= TimeSpan.Zero)
        {
            return;
        }

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = timeToLive
        };

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await distributedCache.SetStringAsync(key, payload, options, cancellationToken);
    }

    /// <summary>
    /// Xóa dữ liệu cache theo khóa.
    /// </summary>
    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        return distributedCache.RemoveAsync(key, cancellationToken);
    }
}
