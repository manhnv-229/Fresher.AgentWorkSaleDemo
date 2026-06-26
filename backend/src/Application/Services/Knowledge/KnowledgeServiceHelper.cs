using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Services.Knowledge;

/// <summary>
/// Helper chung cho các service tri thức agent: giải quyết tên actor, kiểm tra quyền truy cập, ánh xạ DTO, xây dựng tree/breadcrumb, xác thực normalization, và xác định content type.
/// </summary>
internal static class KnowledgeServiceHelper
{
#region Method

    public static bool IsInternalScope(Guid tenantId) => tenantId == Guid.Empty;

    public static Guid? NormalizeTenantId(Guid tenantId) => IsInternalScope(tenantId) ? null : tenantId;

    public static string BuildStorageObjectKey(Guid tenantId, Guid agentId, Guid fileId, string extension)
    {
        return IsInternalScope(tenantId)
            ? $"internal/agents/{agentId:N}/knowledge/{fileId:N}{extension}"
            : $"tenants/{tenantId:N}/agents/{agentId:N}/knowledge/{fileId:N}{extension}";
    }

    /// <summary>
    /// Resolve tên actor từ user ID. Ưu tiên FullName, fallback sang Email, và trả về "Unknown" nếu không tìm thấy.
    /// Dùng để hiển thị tên người tạo/sửa trong response và audit log.
    /// </summary>
    public static async Task<string> ResolveActorNameAsync(IAuthUserRepository authUserRepository, Guid userId, CancellationToken cancellationToken)
    {
        var user = await authUserRepository.GetByIdAsync(userId, cancellationToken);
        return user?.FullName ?? user?.Email ?? "Unknown";
    }

    /// <summary>
    /// Kiểm tra agent tồn tại và có thể đọc được. Trả về null nếu hợp lệ, hoặc error code/message nếu không tìm thấy.
    /// </summary>
    public static async Task<(string Code, string Message)?> EnsureReadableAgentAsync(
        IAgentRepository agentRepository,
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var agent = IsInternalScope(tenantId)
            ? await agentRepository.GetInternalAgentByIdAsync(agentId, cancellationToken)
            : await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        return agent is null ? (KnowledgeErrorCodes.AgentNotFound, "Agent was not found.") : null;
    }

    /// <summary>
    /// Kiểm tra tenant tồn tại, không bị khóa, và agent có thể ghi được. Dùng trước khi thực hiện các thao tác tạo/sửa/xóa.
    /// </summary>
    public static async Task<(string Code, string Message)?> EnsureWritableAgentAsync(
        IAgentRepository agentRepository,
        ITenantRepository tenantRepository,
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        if (IsInternalScope(tenantId))
        {
            return await EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
        }

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

    /// <summary>
    /// Ánh xạ entity folder sang DTO với tên người tạo đã resolve trước từ service caller.
    /// </summary>
    public static KnowledgeFolderItem MapFolder(AgentKnowledgeFolder folder, string createdByUserName) => new(
        folder.Id,
        folder.ParentFolderId,
        folder.Name,
        folder.CreatedByUserId,
        createdByUserName,
        folder.CreatedAt,
        folder.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity folder sang DTO. Dùng navigation property CreatedByUser để lấy tên người tạo.
    /// </summary>
    public static KnowledgeFolderItem MapFolder(AgentKnowledgeFolder folder) => new(
        folder.Id,
        folder.ParentFolderId,
        folder.Name,
        folder.CreatedByUserId,
        folder.CreatedByUser?.FullName ?? folder.CreatedByUser?.Email ?? "Unknown",
        folder.CreatedAt,
        folder.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity file sang DTO với tên người tạo đã resolve trước.
    /// </summary>
    public static KnowledgeFileItem MapFile(AgentKnowledgeFile file, string createdByUserName) => new(
        file.Id,
        file.FolderId,
        file.Name,
        file.OriginalName,
        file.Extension,
        file.StorageObject?.ContentType ?? "application/octet-stream",
        file.StorageObject?.SizeBytes ?? 0,
        file.Status.ToString(),
        file.CreatedByUserId,
        createdByUserName,
        file.CreatedAt,
        file.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity file sang DTO. Dùng navigation property CreatedByUser để lấy tên người tạo.
    /// </summary>
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

    /// <summary>
    /// Ánh xạ entity file sang DTO chi tiết với tên người tạo đã resolve trước. Bao gồm thông tin storage bucket và object key.
    /// </summary>
    public static KnowledgeFileDetail MapFileDetail(AgentKnowledgeFile file, string createdByUserName) => new(
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
        createdByUserName,
        file.CreatedAt,
        file.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity file sang DTO chi tiết. Dùng navigation property CreatedByUser để lấy tên người tạo.
    /// </summary>
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

    /// <summary>
    /// Xây dựng cấu trúc cây thư mục từ danh sách phẳng. Đệ quy theo parentFolderId và sắp xếp theo tên.
    /// </summary>
    public static IReadOnlyList<KnowledgeFolderTreeItem> BuildTree(IReadOnlyList<AgentKnowledgeFolder> folders, Guid? parentFolderId)
    {
        return folders
            .Where(folder => folder.ParentFolderId == parentFolderId)
            .OrderBy(folder => folder.Name)
            .Select(folder => new KnowledgeFolderTreeItem(folder.Id, folder.ParentFolderId, folder.Name, BuildTree(folders, folder.Id)))
            .ToList();
    }

    /// <summary>
    /// Xây dựng breadcrumb từ thư mục được chọn lên root. Đảo ngược danh sách để hiển thị đúng thứ tự từ gốc đến lá.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra candidateId có phải là con cháu của ancestorId không. Dùng để xác nhận ràng buộc khi di chuyển/xóa thư mục.
    /// </summary>
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

    /// <summary>
    /// Normalize tên hiển thị: trim whitespace và trả về null nếu rỗng.
    /// </summary>
    public static string? NormalizeDisplayName(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    /// <summary>
    /// Normalize tên để so sánh: trim và chuyển sang uppercase. Dùng cho uniqueness check.
    /// </summary>
    public static string NormalizeName(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// Xác định content type từ extension nếu không có content type từ client. Dùng mapping chuẩn cho các loại file hỗ trợ.
    /// </summary>
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

#endregion
}
