using Demo.Application.Common;
using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Service;

public interface IKnowledgeFolderService
{
    Task<ServiceResult<KnowledgeFolderItem>> CreateFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid userId,
        string? ipAddress,
        CreateKnowledgeFolderCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<KnowledgeFolderItem>> RenameFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        RenameKnowledgeItemCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<KnowledgeFolderItem>> MoveFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        MoveKnowledgeItemCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
