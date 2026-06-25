using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Services.Knowledge;

internal static class KnowledgeServiceHelper
{
    public static async Task<(string Code, string Message)?> EnsureReadableAgentAsync(
        IAgentRepository agentRepository,
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        return agent is null ? (KnowledgeErrorCodes.AgentNotFound, "Agent was not found.") : null;
    }

    public static async Task<(string Code, string Message)?> EnsureWritableAgentAsync(
        IAgentRepository agentRepository,
        ITenantRepository tenantRepository,
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return (KnowledgeErrorCodes.TenantNotFound, "Tenant was not found.");
        }

        if (tenant.Status == Domain.Enums.TenantStatus.Locked)
        {
            return (KnowledgeErrorCodes.TenantLocked, "Cannot modify knowledge in a locked tenant.");
        }

        return await EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
    }

    public static KnowledgeFolderItem MapFolder(AgentKnowledgeFolder folder) => new(
        folder.Id,
        folder.ParentFolderId,
        folder.Name,
        folder.CreatedByUserId,
        folder.CreatedByUser?.FullName ?? folder.CreatedByUser?.Email ?? "Unknown",
        folder.CreatedAt,
        folder.ModifiedAt);

    public static KnowledgeFileItem MapFile(AgentKnowledgeFile file) => new(
        file.Id,
        file.FolderId,
        file.Name,
        file.OriginalName,
        file.Extension,
        file.StorageObject?.ContentType ?? "application/octet-stream",
        file.StorageObject?.SizeBytes ?? 0,
        file.Status.ToString(),
        file.CreatedByUserId,
        file.CreatedByUser?.FullName ?? file.CreatedByUser?.Email ?? "Unknown",
        file.CreatedAt,
        file.ModifiedAt);

    public static KnowledgeFileDetail MapFileDetail(AgentKnowledgeFile file) => new(
        file.Id,
        file.FolderId,
        file.Name,
        file.OriginalName,
        file.Extension,
        file.StorageObject?.ContentType ?? "application/octet-stream",
        file.StorageObject?.SizeBytes ?? 0,
        file.Status.ToString(),
        file.StorageObject?.StorageBucket ?? string.Empty,
        file.StorageObject?.StorageObjectKey ?? string.Empty,
        file.CreatedByUserId,
        file.CreatedByUser?.FullName ?? file.CreatedByUser?.Email ?? "Unknown",
        file.CreatedAt,
        file.ModifiedAt);

    public static IReadOnlyList<KnowledgeFolderTreeItem> BuildTree(IReadOnlyList<AgentKnowledgeFolder> folders, Guid? parentFolderId)
    {
        return folders
            .Where(folder => folder.ParentFolderId == parentFolderId)
            .OrderBy(folder => folder.Name)
            .Select(folder => new KnowledgeFolderTreeItem(folder.Id, folder.ParentFolderId, folder.Name, BuildTree(folders, folder.Id)))
            .ToList();
    }

    public static IReadOnlyList<KnowledgeBreadcrumbItem> BuildBreadcrumb(
        IReadOnlyList<AgentKnowledgeFolder> folders,
        AgentKnowledgeFolder? selectedFolder)
    {
        if (selectedFolder is null)
        {
            return [];
        }

        var byId = folders.ToDictionary(folder => folder.Id);
        var path = new List<KnowledgeBreadcrumbItem>();
        var current = selectedFolder;
        while (current is not null)
        {
            path.Add(new KnowledgeBreadcrumbItem(current.Id, current.Name));
            current = current.ParentFolderId is not null && byId.TryGetValue(current.ParentFolderId.Value, out var parent)
                ? parent
                : null;
        }

        path.Reverse();
        return path;
    }

    public static bool IsDescendant(IReadOnlyList<AgentKnowledgeFolder> folders, Guid ancestorId, Guid candidateId)
    {
        var byId = folders.ToDictionary(folder => folder.Id);
        var currentId = candidateId;
        while (byId.TryGetValue(currentId, out var current) && current.ParentFolderId is not null)
        {
            if (current.ParentFolderId == ancestorId)
            {
                return true;
            }

            currentId = current.ParentFolderId.Value;
        }

        return false;
    }

    public static string? NormalizeDisplayName(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string NormalizeName(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    public static string NormalizeContentType(string contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
