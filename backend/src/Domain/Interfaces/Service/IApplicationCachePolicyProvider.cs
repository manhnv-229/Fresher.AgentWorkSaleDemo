namespace Demo.Domain.Interfaces.Service;

/// <summary>
/// Cung cấp chính sách TTL cho các capability cache ở tầng ứng dụng.
/// </summary>
public interface IApplicationCachePolicyProvider
{
    TimeSpan PermissionEntryTimeToLive { get; }

    TimeSpan AgentDetailTimeToLive { get; }

    TimeSpan AgentListTimeToLive { get; }

    TimeSpan TenantListTimeToLive { get; }

    TimeSpan TenantDetailTimeToLive { get; }

    TimeSpan KnowledgeExplorerTimeToLive { get; }

    TimeSpan NamespaceVersionTimeToLive { get; }
}
