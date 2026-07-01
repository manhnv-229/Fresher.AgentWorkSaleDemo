namespace Demo.Domain.Interfaces.Service;

/// <summary>
/// Cung cấp thao tác cache typed dùng chung với thời gian hết hạn tương đối.
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// Lấy dữ liệu cache theo khóa và giải tuần tự về kiểu mong muốn.
    /// </summary>
    Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Lưu dữ liệu cache theo khóa với thời gian hết hạn tương đối.
    /// </summary>
    Task SetAsync<TValue>(string key, TValue value, TimeSpan timeToLive, CancellationToken cancellationToken);

    /// <summary>
    /// Xóa dữ liệu cache theo khóa.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
