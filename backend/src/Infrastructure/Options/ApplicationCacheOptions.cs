namespace Demo.Infrastructure.Options;

/// <summary>
/// Mô tả TTL cho các read cache ở tầng ứng dụng.
/// </summary>
public sealed class ApplicationCacheOptions
{
    public const string SectionName = "Caching";

    public int PermissionEntrySeconds { get; set; } = 300;

    public int AgentDetailSeconds { get; set; } = 300;

    public int AgentListSeconds { get; set; } = 120;

    public int TenantListSeconds { get; set; } = 600;

    public int TenantDetailSeconds { get; set; } = 600;

    public int KnowledgeExplorerSeconds { get; set; } = 120;

    public int NamespaceVersionSeconds { get; set; } = 604800;
}
