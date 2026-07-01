using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services.Knowledge;

/// <summary>
/// Xử lý các thao tác ghi đối với thư mục tri thức agent: tạo, đổi tên, di chuyển, và xóa mềm (soft delete) toàn bộ subtree.
/// </summary>
public sealed class KnowledgeFolderService(
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAgentKnowledgeRepository knowledgeRepository,
    IAuthUserRepository authUserRepository,
    IAuditLogService auditLogService,
    ICacheVersionService cacheVersionService,
    ILogger<KnowledgeFolderService> logger,
    IUnitOfWork unitOfWork) : IKnowledgeFolderService
{
#region Method

    /// <summary>
    /// Tạo mới một thư mục tri thức agent. Kiểm tra quyền ghi, tên hợp lệ, và trùng tên trong cùng thư mục cha trước khi persist.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFolderItem>> CreateFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid userId,
        string? ipAddress,
        CreateKnowledgeFolderCommand command,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var name = KnowledgeServiceHelper.NormalizeDisplayName(command.Name);
        if (name is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidName, "Folder name is required.");
        }

        // Xác thực thư mục cha tồn tại nếu có chỉ định
        if (command.ParentFolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, command.ParentFolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Parent folder was not found.");
        }

        var normalizedName = KnowledgeServiceHelper.NormalizeName(name);
        if (await knowledgeRepository.FolderNameExistsAsync(agentId, command.ParentFolderId, normalizedName, null, cancellationToken))
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.ValidationError, "A folder with the same name already exists.");
        }

        var folder = new AgentKnowledgeFolder
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            ParentFolderId = command.ParentFolderId,
            Name = name,
            NormalizedName = normalizedName,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        knowledgeRepository.AddFolder(folder);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateKnowledgeExplorerCacheAsync(tenantId, agentId, cancellationToken);
        // Resolve tên actor trước khi trả response vì navigation CreatedByUser chưa được nạp trên entity vừa tạo
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.folder.create", actorName, userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was created.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(KnowledgeServiceHelper.MapFolder(folder, actorName));
    }

    /// <summary>
    /// Đổi tên thư mục tri thức. Kiểm tra trùng tên trong cùng thư mục cha và ghi nhận audit log với tên actor đã resolve.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFolderItem>> RenameFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        RenameKnowledgeItemCommand command,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var folder = await knowledgeRepository.GetFolderAsync(agentId, folderId, trackChanges: true, cancellationToken);
        if (folder is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var ownerAccess = EnsureFolderOwnerAccess(folder, userId);
        if (ownerAccess is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(ownerAccess.Value.Code, ownerAccess.Value.Message);
        }

        var name = KnowledgeServiceHelper.NormalizeDisplayName(command.Name);
        if (name is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidName, "Folder name is required.");
        }

        var normalizedName = KnowledgeServiceHelper.NormalizeName(name);
        if (await knowledgeRepository.FolderNameExistsAsync(agentId, folder.ParentFolderId, normalizedName, folder.Id, cancellationToken))
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.ValidationError, "A folder with the same name already exists.");
        }

        var previousName = folder.Name;
        folder.Name = name;
        folder.NormalizedName = normalizedName;
        folder.ModifiedByUserId = userId;
        folder.ModifiedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateKnowledgeExplorerCacheAsync(tenantId, agentId, cancellationToken);
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.folder.rename", actorName, userId, tenantId, ipAddress, $"Knowledge folder '{previousName}' was renamed to '{folder.Name}'.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(KnowledgeServiceHelper.MapFolder(folder, actorName));
    }

    /// <summary>
    /// Di chuyển thư mục đến thư mục đích. Kiểm tra không di chuyển vào chính nó hoặc con cháu, và trùng tên trong thư mục đích.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFolderItem>> MoveFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        MoveKnowledgeItemCommand command,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var folder = await knowledgeRepository.GetFolderAsync(agentId, folderId, trackChanges: true, cancellationToken);
        if (folder is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var ownerAccess = EnsureFolderOwnerAccess(folder, userId);
        if (ownerAccess is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(ownerAccess.Value.Code, ownerAccess.Value.Message);
        }

        if (folder.Id == command.TargetFolderId)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidMove, "Folder cannot be moved into itself.");
        }

        // Kiểm tra thư mục đích tồn tại và không phải là con cháu của thư mục đang di chuyển
        var allFolders = await knowledgeRepository.GetFoldersAsync(agentId, cancellationToken);
        if (command.TargetFolderId is not null)
        {
            var target = allFolders.FirstOrDefault(item => item.Id == command.TargetFolderId.Value && item.DeletedAt is null);
            if (target is null)
            {
                return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Target folder was not found.");
            }

            if (KnowledgeServiceHelper.IsDescendant(allFolders, folder.Id, target.Id))
            {
                return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidMove, "Folder cannot be moved into its descendant.");
            }
        }

        var normalizedName = KnowledgeServiceHelper.NormalizeName(folder.Name);
        if (await knowledgeRepository.FolderNameExistsAsync(agentId, command.TargetFolderId, normalizedName, folder.Id, cancellationToken))
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.ValidationError, "A folder with the same name already exists in the target folder.");
        }

        folder.ParentFolderId = command.TargetFolderId;
        folder.ModifiedByUserId = userId;
        folder.ModifiedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateKnowledgeExplorerCacheAsync(tenantId, agentId, cancellationToken);
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.folder.move", actorName, userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was moved.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(KnowledgeServiceHelper.MapFolder(folder, actorName));
    }

    /// <summary>
    /// Xóa mềm toàn bộ subtree thư mục: đánh dấu deleted cho thư mục, tất cả con cháu, và file thuộc subtree.
    /// Không xóa vật lý object storage để giữ an toàn rollback.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<bool>.Failure(access.Value.Code, access.Value.Message);
        }

        var folder = await knowledgeRepository.GetFolderAsync(agentId, folderId, trackChanges: true, cancellationToken);
        if (folder is null)
        {
            return ServiceResult<bool>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var ownerAccess = EnsureFolderOwnerAccess(folder, userId);
        if (ownerAccess is not null)
        {
            return ServiceResult<bool>.Failure(ownerAccess.Value.Code, ownerAccess.Value.Message);
        }

        // Soft delete toàn bộ subtree: folder con và file thuộc subtree
        var allFolders = await knowledgeRepository.GetFoldersAsync(agentId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var descendant in allFolders.Where(item => item.Id == folder.Id || KnowledgeServiceHelper.IsDescendant(allFolders, folder.Id, item.Id)))
        {
            descendant.DeletedAt = now;
            descendant.ModifiedAt = now;
            descendant.ModifiedByUserId = userId;
        }

        var folderIds = allFolders
            .Where(item => item.Id == folder.Id || KnowledgeServiceHelper.IsDescendant(allFolders, folder.Id, item.Id))
            .Select(item => item.Id)
            .ToHashSet();
        var files = await knowledgeRepository.GetFilesByFolderIdsAsync(agentId, folderIds.ToList(), cancellationToken);
        foreach (var file in files)
        {
            file.DeletedAt = now;
            file.ModifiedAt = now;
            file.ModifiedByUserId = userId;
            file.Status = Domain.Enums.AgentKnowledgeFileStatus.Deleted;
            if (file.StorageObject is not null)
            {
                file.StorageObject.DeletedAt = now;
                file.StorageObject.Status = Domain.Enums.KnowledgeStorageObjectStatus.Deleted;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateKnowledgeExplorerCacheAsync(tenantId, agentId, cancellationToken);
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.folder.delete", actorName, userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was deleted.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Ghi audit log cho các thao tác folder với tên actor đã resolve từ authenticated user.
    /// </summary>
    private async Task RecordAuditAsync(
        string action,
        string actorName,
        Guid userId,
        Guid tenantId,
        string? ipAddress,
        string description,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        await auditLogService.RecordAsync(
            action,
            actorName,
            userId,
            KnowledgeServiceHelper.NormalizeTenantId(tenantId),
            ipAddress,
            description,
            targetType,
            targetId.ToString(),
            cancellationToken);
    }

    private static (string Code, string Message)? EnsureFolderOwnerAccess(AgentKnowledgeFolder folder, Guid userId)
    {
        return folder.CreatedByUserId == userId
            ? null
            : (KnowledgeErrorCodes.FolderOwnerRequired, "Only the folder creator can rename, move, or delete this folder.");
    }

    /// <summary>
    /// Invalidate explorer cache của agent sau các thao tác thay đổi cây thư mục.
    /// </summary>
    private async Task InvalidateKnowledgeExplorerCacheAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        try
        {
            await cacheVersionService.RefreshVersionAsync(
                ApplicationCacheKeys.KnowledgeExplorerNamespace(tenantId, agentId),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể invalidate knowledge-explorer cache cho agent {AgentId}.", agentId);
        }
    }

#endregion
}
