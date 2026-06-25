using Demo.Domain.Entities;

namespace Demo.Application.Interfaces.Repository;

public interface IAgentKnowledgeRepository
{
    Task<IReadOnlyList<AgentKnowledgeFolder>> GetFoldersAsync(Guid agentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentKnowledgeFolder>> GetChildFoldersAsync(Guid agentId, Guid? parentFolderId, CancellationToken cancellationToken);

    Task<AgentKnowledgeFolder?> GetFolderAsync(Guid agentId, Guid folderId, bool trackChanges, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentKnowledgeFolder>> GetDescendantFoldersAsync(Guid agentId, Guid ancestorFolderId, CancellationToken cancellationToken);

    Task<bool> FolderNameExistsAsync(Guid agentId, Guid? parentFolderId, string normalizedName, Guid? excludeFolderId, CancellationToken cancellationToken);

    void AddFolder(AgentKnowledgeFolder folder);

    Task<IReadOnlyList<AgentKnowledgeFile>> GetFilesAsync(Guid agentId, Guid? folderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentKnowledgeFile>> GetFilesByFolderIdsAsync(Guid agentId, IReadOnlyList<Guid> folderIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentKnowledgeFile>> SearchFilesAsync(
        Guid agentId,
        string? normalizedName,
        Guid? folderId,
        Guid? createdByUserId,
        DateTime? createdFrom,
        DateTime? createdTo,
        CancellationToken cancellationToken);

    Task<AgentKnowledgeFile?> GetFileAsync(Guid agentId, Guid fileId, bool trackChanges, CancellationToken cancellationToken);

    Task<bool> FileNameExistsAsync(Guid agentId, Guid? folderId, string normalizedName, Guid? excludeFileId, CancellationToken cancellationToken);

    Task<bool> ExactFileDuplicateExistsAsync(
        Guid agentId,
        Guid? folderId,
        string checksumSha256,
        long sizeBytes,
        CancellationToken cancellationToken);

    void AddFile(AgentKnowledgeFile file);

    void AddStorageObject(KnowledgeStorageObject storageObject);
}
