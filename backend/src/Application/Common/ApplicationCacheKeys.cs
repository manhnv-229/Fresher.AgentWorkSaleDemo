namespace Demo.Application.Common;

/// <summary>
/// Chuẩn hóa key và namespace cho application read cache.
/// </summary>
public static class ApplicationCacheKeys
{
    public static string PermissionNamespace(Guid userId)
    {
        return $"app-cache:ns:permission:{userId:N}";
    }

    public static string PermissionSet(Guid userId, string version)
    {
        return $"app-cache:permission:set:{userId:N}:v:{version}";
    }

    public static string PermissionCheck(Guid userId, Guid? tenantId, string permissionCode, string version)
    {
        return $"app-cache:permission:check:{userId:N}:{FormatGuid(tenantId)}:{NormalizePart(permissionCode)}:v:{version}";
    }

    public static string AgentDetailNamespace(Guid? tenantId, Guid agentId)
    {
        return $"app-cache:ns:agent-detail:{FormatGuid(tenantId)}:{agentId:N}";
    }

    public static string AgentDetail(Guid? tenantId, Guid agentId, string version)
    {
        return $"app-cache:agent-detail:{FormatGuid(tenantId)}:{agentId:N}:v:{version}";
    }

    public static string AgentListNamespace(Guid? tenantId)
    {
        return $"app-cache:ns:agent-list:{FormatGuid(tenantId)}";
    }

    public static string AgentList(Guid? tenantId, string status, int pageSize, string version)
    {
        return $"app-cache:agent-list:{FormatGuid(tenantId)}:{status}:{pageSize}:v:{version}";
    }

    public static string TenantListNamespace()
    {
        return "app-cache:ns:tenant-list";
    }

    public static string TenantList(string version)
    {
        return $"app-cache:tenant-list:v:{version}";
    }

    public static string TenantDetailNamespace(Guid tenantId)
    {
        return $"app-cache:ns:tenant-detail:{tenantId:N}";
    }

    public static string TenantDetail(Guid tenantId, string version)
    {
        return $"app-cache:tenant-detail:{tenantId:N}:v:{version}";
    }

    public static string KnowledgeExplorerNamespace(Guid tenantId, Guid agentId)
    {
        return $"app-cache:ns:knowledge-explorer:{tenantId:N}:{agentId:N}";
    }

    public static string KnowledgeExplorer(Guid tenantId, Guid agentId, Guid? folderId, string version)
    {
        return $"app-cache:knowledge-explorer:{tenantId:N}:{agentId:N}:{FormatGuid(folderId)}:v:{version}";
    }

    private static string FormatGuid(Guid? value)
    {
        return value?.ToString("N") ?? "none";
    }

    private static string NormalizePart(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "none"
            : value.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}
