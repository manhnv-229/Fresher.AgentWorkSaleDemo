namespace Demo.Infrastructure.Options;

/// <summary>
/// Mô tả cấu hình kết nối Redis cho distributed cache của ứng dụng.
/// </summary>
public sealed class RedisCacheOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 6379;

    public string? Password { get; set; }

    public bool UseSsl { get; set; }

    public int ConnectTimeoutSeconds { get; set; } = 5;

    public int SyncTimeoutSeconds { get; set; } = 5;

    public string InstanceName { get; set; } = "demo:";
}
