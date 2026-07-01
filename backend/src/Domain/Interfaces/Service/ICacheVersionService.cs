namespace Demo.Domain.Interfaces.Service;

/// <summary>
/// Quản lý version token cho các namespace cache để invalidation theo nhóm key.
/// </summary>
public interface ICacheVersionService
{
    /// <summary>
    /// Lấy version token hiện tại của namespace cache.
    /// </summary>
    Task<string> GetVersionAsync(string namespaceKey, CancellationToken cancellationToken);

    /// <summary>
    /// Làm mới version token để vô hiệu hóa toàn bộ key đã phát sinh từ namespace cũ.
    /// </summary>
    Task RefreshVersionAsync(string namespaceKey, CancellationToken cancellationToken);
}
