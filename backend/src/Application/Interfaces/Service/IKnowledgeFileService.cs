using Demo.Application.Common;
using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Service;

public interface IKnowledgeFileService
{
    Task<ServiceResult<KnowledgeFileItem>> UploadFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid userId,
        string? ipAddress,
        KnowledgeUploadContent upload,
        CancellationToken cancellationToken);

    Task<ServiceResult<KnowledgeDownloadResult>> DownloadFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<KnowledgeFileDetail>> GetFileDetailAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<KnowledgeFileItem>> RenameFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        RenameKnowledgeItemCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<KnowledgeFileItem>> MoveFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        MoveKnowledgeItemCommand command,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
