namespace Demo.Infrastructure.Caching;

/// <summary>
/// Lưu trạng thái session đang còn hiệu lực để tái sử dụng trong Redis.
/// </summary>
public sealed class AuthSessionCacheEntry
{
    public Guid UserId { get; set; }

    public Guid SessionId { get; set; }

    public DateTime ExpiresAt { get; set; }
}
