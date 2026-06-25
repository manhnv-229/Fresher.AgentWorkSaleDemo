using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services.Knowledge;

public sealed class KnowledgeFolderService(
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAgentKnowledgeRepository knowledgeRepository,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork) : IKnowledgeFolderService
{
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
        await RecordAuditAsync("knowledge.folder.create", userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was created.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(KnowledgeServiceHelper.MapFolder(folder));
    }

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
        await RecordAuditAsync("knowledge.folder.rename", userId, tenantId, ipAddress, $"Knowledge folder '{previousName}' was renamed to '{folder.Name}'.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(KnowledgeServiceHelper.MapFolder(folder));
    }

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

        if (folder.Id == command.TargetFolderId)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidMove, "Folder cannot be moved into itself.");
        }

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
        await RecordAuditAsync("knowledge.folder.move", userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was moved.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(KnowledgeServiceHelper.MapFolder(folder));
    }

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
        await RecordAuditAsync("knowledge.folder.delete", userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was deleted.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private async Task RecordAuditAsync(
        string action,
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
            "System",
            userId,
            tenantId,
            ipAddress,
            description,
            targetType,
            targetId.ToString(),
            cancellationToken);
    }
}
