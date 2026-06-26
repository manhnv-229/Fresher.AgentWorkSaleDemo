using Demo.Application.Common;
using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Service;

public interface IKnowledgeExplorerService
{
    Task<ServiceResult<KnowledgeExplorerResponse>> GetExplorerAsync(Guid tenantId, Guid agentId, Guid? folderId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<KnowledgeFileItem>>> SearchFilesAsync(
        Guid tenantId,
        Guid agentId,
        KnowledgeSearchFilters filters,
        CancellationToken cancellationToken);
}
