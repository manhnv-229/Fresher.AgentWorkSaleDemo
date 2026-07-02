using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

/// <summary>
/// Xử lý các thao tác truy vấn explorer tri thức agent: tải cây thư mục, breadcrumb, nội dung thư mục hiện tại, và tìm kiếm file.
/// </summary>
public sealed class KnowledgeExplorerService(
    IAgentRepository agentRepository,
    IAgentKnowledgeRepository knowledgeRepository,
    IDistributedCacheService distributedCacheService,
    ICacheVersionService cacheVersionService,
    IApplicationCachePolicyProvider cachePolicyProvider,
    ILogger<KnowledgeExplorerService> logger) : IKnowledgeExplorerService
{
#region Method

    /// <summary>
    /// Tải trạng thái explorer hoàn chỉnh cho một agent: cây thư mục, breadcrumb, danh sách thư mục con, và danh sách file trong thư mục được chọn.
    /// </summary>
    public async Task<ServiceResult<KnowledgeExplorerResponse>> GetExplorerAsync(
        Guid tenantId,
        Guid agentId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        // Kiểm tra quyền đọc trước khi tải dữ liệu explorer
        var access = await KnowledgeServiceHelper.EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeExplorerResponse>.Failure(access.Value.Code, access.Value.Message);
        }

        if (await TryBuildExplorerCacheKeyAsync(tenantId, agentId, folderId, cancellationToken) is { } cacheKey)
        {
            var cachedResponse = await ApplicationCacheOperations.TryGetAsync<KnowledgeExplorerResponse>(
                distributedCacheService,
                logger,
                cacheKey,
                "knowledge-explorer",
                cancellationToken);
            if (cachedResponse is not null)
            {
                return ServiceResult<KnowledgeExplorerResponse>.Success(cachedResponse);
            }
        }

        // Tải toàn bộ thư mục còn hiệu lực của agent để xây dựng cây và breadcrumb.
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

        // Tải nội dung thư mục hiện tại: thư mục con và file
        var childFolders = await knowledgeRepository.GetChildFoldersAsync(agentId, folderId, cancellationToken);
        var files = await knowledgeRepository.GetFilesAsync(agentId, folderId, cancellationToken);
        var response = new KnowledgeExplorerResponse(
            agentId,
            folderId,
            KnowledgeServiceHelper.BuildTree(folders.Where(folder => folder.DeletedAt is null).ToList(), null),
            KnowledgeServiceHelper.BuildBreadcrumb(folders, selectedFolder),
            childFolders.Select(KnowledgeServiceHelper.MapFolder).ToList(),
            files.Select(KnowledgeServiceHelper.MapFile).ToList());

        if (await TryBuildExplorerCacheKeyAsync(tenantId, agentId, folderId, cancellationToken) is { } explorerCacheKey)
        {
            await ApplicationCacheOperations.TrySetAsync(
                distributedCacheService,
                logger,
                explorerCacheKey,
                response,
                cachePolicyProvider.KnowledgeExplorerTimeToLive,
                "knowledge-explorer",
                cancellationToken);
        }

        return ServiceResult<KnowledgeExplorerResponse>.Success(response);
    }

    /// <summary>
    /// Tìm kiếm tri thức theo một contract backend thống nhất cho cả thư mục và file.
    /// </summary>
    public async Task<ServiceResult<KnowledgeSearchResponse>> SearchAsync(
        Guid tenantId,
        Guid agentId,
        KnowledgeSearchFilters filters,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeSearchResponse>.Failure(access.Value.Code, access.Value.Message);
        }

        if (string.IsNullOrWhiteSpace(filters.Name))
        {
            return ServiceResult<KnowledgeSearchResponse>.Success(new KnowledgeSearchResponse(agentId, [], []));
        }

        if (filters.FolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, filters.FolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeSearchResponse>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var normalizedName = KnowledgeSearchHelper.Normalize(filters.Name);
        var folders = await knowledgeRepository.SearchFoldersAsync(agentId, normalizedName, cancellationToken);
        var files = await knowledgeRepository.SearchFilesAsync(
            agentId,
            normalizedName,
            filters.FolderId,
            filters.CreatedByUserId,
            filters.CreatedFrom,
            filters.CreatedTo,
            cancellationToken);

        return ServiceResult<KnowledgeSearchResponse>.Success(
            new KnowledgeSearchResponse(
                agentId,
                folders.Select(KnowledgeServiceHelper.MapFolder).ToList(),
                files.Select(KnowledgeServiceHelper.MapFile).ToList()));
    }

    /// <summary>
    /// Tạo cache key cho explorer response theo context tenant, agent, và thư mục đang chọn.
    /// </summary>
    private async Task<string?> TryBuildExplorerCacheKeyAsync(
        Guid tenantId,
        Guid agentId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.KnowledgeExplorerNamespace(tenantId, agentId);
        var version = await ApplicationCacheOperations.TryGetVersionAsync(
            cacheVersionService,
            logger,
            namespaceKey,
            $"knowledge-explorer cho agent {agentId}",
            cancellationToken);
        return version is null
            ? null
            : ApplicationCacheKeys.KnowledgeExplorer(tenantId, agentId, folderId, version);
    }

#endregion
}
