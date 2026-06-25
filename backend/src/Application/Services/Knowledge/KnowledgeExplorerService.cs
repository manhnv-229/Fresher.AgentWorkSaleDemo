using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Services.Knowledge;

public sealed class KnowledgeExplorerService(
    IAgentRepository agentRepository,
    IAgentKnowledgeRepository knowledgeRepository) : IKnowledgeExplorerService
{
    public async Task<ServiceResult<KnowledgeExplorerResponse>> GetExplorerAsync(
        Guid tenantId,
        Guid agentId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeExplorerResponse>.Failure(access.Value.Code, access.Value.Message);
        }

        var folders = await knowledgeRepository.GetFoldersAsync(agentId, cancellationToken);
        AgentKnowledgeFolder? selectedFolder = null;
        if (folderId is not null)
        {
            selectedFolder = folders.FirstOrDefault(folder => folder.Id == folderId.Value && folder.DeletedAt is null);
            if (selectedFolder is null)
            {
                return ServiceResult<KnowledgeExplorerResponse>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
            }
        }

        var childFolders = await knowledgeRepository.GetChildFoldersAsync(agentId, folderId, cancellationToken);
        var files = await knowledgeRepository.GetFilesAsync(agentId, folderId, cancellationToken);
        var response = new KnowledgeExplorerResponse(
            agentId,
            folderId,
            KnowledgeServiceHelper.BuildTree(folders.Where(folder => folder.DeletedAt is null).ToList(), null),
            KnowledgeServiceHelper.BuildBreadcrumb(folders, selectedFolder),
            childFolders.Select(KnowledgeServiceHelper.MapFolder).ToList(),
            files.Select(KnowledgeServiceHelper.MapFile).ToList());

        return ServiceResult<KnowledgeExplorerResponse>.Success(response);
    }

    public async Task<ServiceResult<IReadOnlyList<KnowledgeFileItem>>> SearchFilesAsync(
        Guid tenantId,
        Guid agentId,
        KnowledgeSearchFilters filters,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<IReadOnlyList<KnowledgeFileItem>>.Failure(access.Value.Code, access.Value.Message);
        }

        if (filters.FolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, filters.FolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<IReadOnlyList<KnowledgeFileItem>>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var files = await knowledgeRepository.SearchFilesAsync(
            agentId,
            KnowledgeServiceHelper.NormalizeName(filters.Name),
            filters.FolderId,
            filters.CreatedByUserId,
            filters.CreatedFrom,
            filters.CreatedTo,
            cancellationToken);

        return ServiceResult<IReadOnlyList<KnowledgeFileItem>>.Success(files.Select(KnowledgeServiceHelper.MapFile).ToList());
    }
}
