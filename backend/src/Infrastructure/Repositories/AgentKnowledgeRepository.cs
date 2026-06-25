using Demo.Application.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

public sealed class AgentKnowledgeRepository(DemoDbContext dbContext) : IAgentKnowledgeRepository
{
    public async Task<IReadOnlyList<AgentKnowledgeFolder>> GetFoldersAsync(Guid agentId, CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFolders
            .Include(folder => folder.CreatedByUser)
            .Where(folder => folder.AgentId == agentId && folder.DeletedAt == null)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentKnowledgeFolder>> GetChildFoldersAsync(
        Guid agentId,
        Guid? parentFolderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFolders
            .AsNoTracking()
            .Include(folder => folder.CreatedByUser)
            .Where(folder =>
                folder.AgentId == agentId &&
                folder.ParentFolderId == parentFolderId &&
                folder.DeletedAt == null)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentKnowledgeFolder?> GetFolderAsync(
        Guid agentId,
        Guid folderId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AgentKnowledgeFolders
            .Include(folder => folder.CreatedByUser)
            .Where(folder =>
                folder.AgentId == agentId &&
                folder.Id == folderId &&
                folder.DeletedAt == null);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentKnowledgeFolder>> GetDescendantFoldersAsync(
        Guid agentId,
        Guid ancestorFolderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFolders
            .AsNoTracking()
            .Where(folder =>
                folder.AgentId == agentId &&
                folder.ParentFolderId == ancestorFolderId &&
                folder.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> FolderNameExistsAsync(
        Guid agentId,
        Guid? parentFolderId,
        string normalizedName,
        Guid? excludeFolderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFolders.AnyAsync(folder =>
            folder.AgentId == agentId &&
            folder.ParentFolderId == parentFolderId &&
            folder.NormalizedName == normalizedName &&
            folder.DeletedAt == null &&
            (excludeFolderId == null || folder.Id != excludeFolderId.Value),
            cancellationToken);
    }

    public void AddFolder(AgentKnowledgeFolder folder)
    {
        dbContext.AgentKnowledgeFolders.Add(folder);
    }

    public async Task<IReadOnlyList<AgentKnowledgeFile>> GetFilesAsync(
        Guid agentId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFiles
            .AsNoTracking()
            .Include(file => file.CreatedByUser)
            .Include(file => file.StorageObject)
            .Where(file =>
                file.AgentId == agentId &&
                file.FolderId == folderId &&
                file.DeletedAt == null &&
                file.Status == AgentKnowledgeFileStatus.Active)
            .OrderBy(file => file.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentKnowledgeFile>> GetFilesByFolderIdsAsync(
        Guid agentId,
        IReadOnlyList<Guid> folderIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFiles
            .AsNoTracking()
            .Include(file => file.StorageObject)
            .Where(file =>
                file.AgentId == agentId &&
                file.FolderId.HasValue &&
                folderIds.Contains(file.FolderId.Value) &&
                file.DeletedAt == null &&
                file.Status == AgentKnowledgeFileStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentKnowledgeFile>> SearchFilesAsync(
        Guid agentId,
        string? normalizedName,
        Guid? folderId,
        Guid? createdByUserId,
        DateTime? createdFrom,
        DateTime? createdTo,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AgentKnowledgeFiles
            .Include(file => file.CreatedByUser)
            .Include(file => file.StorageObject)
            .Where(file =>
                file.AgentId == agentId &&
                file.DeletedAt == null &&
                file.Status == AgentKnowledgeFileStatus.Active);

        if (!string.IsNullOrWhiteSpace(normalizedName))
        {
            query = query.Where(file => file.NormalizedName.Contains(normalizedName));
        }

        if (folderId is not null)
        {
            query = query.Where(file => file.FolderId == folderId.Value);
        }

        if (createdByUserId is not null)
        {
            query = query.Where(file => file.CreatedByUserId == createdByUserId.Value);
        }

        if (createdFrom is not null)
        {
            query = query.Where(file => file.CreatedAt >= createdFrom.Value);
        }

        if (createdTo is not null)
        {
            query = query.Where(file => file.CreatedAt < createdTo.Value);
        }

        return await query
            .OrderByDescending(file => file.CreatedAt)
            .ThenBy(file => file.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentKnowledgeFile?> GetFileAsync(
        Guid agentId,
        Guid fileId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AgentKnowledgeFiles
            .Include(file => file.CreatedByUser)
            .Include(file => file.StorageObject)
            .Where(file =>
                file.AgentId == agentId &&
                file.Id == fileId &&
                file.DeletedAt == null &&
                file.Status == AgentKnowledgeFileStatus.Active);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> FileNameExistsAsync(
        Guid agentId,
        Guid? folderId,
        string normalizedName,
        Guid? excludeFileId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFiles.AnyAsync(file =>
            file.AgentId == agentId &&
            file.FolderId == folderId &&
            file.NormalizedName == normalizedName &&
            file.DeletedAt == null &&
            file.Status == AgentKnowledgeFileStatus.Active &&
            (excludeFileId == null || file.Id != excludeFileId.Value),
            cancellationToken);
    }

    public void AddFile(AgentKnowledgeFile file)
    {
        dbContext.AgentKnowledgeFiles.Add(file);
    }

    public void AddStorageObject(KnowledgeStorageObject storageObject)
    {
        dbContext.KnowledgeStorageObjects.Add(storageObject);
    }
}
