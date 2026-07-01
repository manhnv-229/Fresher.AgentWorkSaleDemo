using Demo.Domain.Interfaces.Service;
using Demo.Infrastructure.Options;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Ánh xạ cấu hình TTL sang chính sách cache dùng trong application services.
/// </summary>
public sealed class ApplicationCachePolicyProvider(ApplicationCacheOptions options) : IApplicationCachePolicyProvider
{
    public TimeSpan PermissionEntryTimeToLive { get; } = ToTimeSpan(options.PermissionEntrySeconds, 300);

    public TimeSpan AgentDetailTimeToLive { get; } = ToTimeSpan(options.AgentDetailSeconds, 300);

    public TimeSpan AgentListTimeToLive { get; } = ToTimeSpan(options.AgentListSeconds, 120);

    public TimeSpan TenantListTimeToLive { get; } = ToTimeSpan(options.TenantListSeconds, 600);

    public TimeSpan TenantDetailTimeToLive { get; } = ToTimeSpan(options.TenantDetailSeconds, 600);

    public TimeSpan KnowledgeExplorerTimeToLive { get; } = ToTimeSpan(options.KnowledgeExplorerSeconds, 120);

    public TimeSpan NamespaceVersionTimeToLive { get; } = ToTimeSpan(options.NamespaceVersionSeconds, 604800);

    private static TimeSpan ToTimeSpan(int seconds, int fallbackSeconds)
    {
        return TimeSpan.FromSeconds(Math.Max(1, seconds <= 0 ? fallbackSeconds : seconds));
    }
}
