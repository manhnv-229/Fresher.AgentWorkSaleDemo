namespace Demo.Infrastructure.Caching;

/// <summary>
/// Chuẩn hóa quy ước khóa cache dùng trong hạ tầng.
/// </summary>
internal static class CacheKeys
{
    /// <summary>
    /// Tạo khóa cache cho trạng thái session xác thực.
    /// </summary>
    public static string AuthSession(Guid userId, Guid sessionId)
    {
        return $"auth-session:{userId:N}:{sessionId:N}";
    }
}
