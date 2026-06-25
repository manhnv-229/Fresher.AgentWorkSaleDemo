using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Domain.Interfaces.Repository;

namespace Demo.Application.Services.Knowledge;

/// <summary>
/// Xử lý các thao tác truy vấn explorer tri thức agent: tải cây thư mục, breadcrumb, nội dung thư mục hiện tại, và tìm kiếm file.
/// </summary>
public sealed class KnowledgeExplorerService(
    IAgentRepository agentRepository,
    IAgentKnowledgeRepository knowledgeRepository) : IKnowledgeExplorerService
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

        // Tải toàn bộ thư mục活着 của agent để xây dựng cây và breadcrumb
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

        return ServiceResult<KnowledgeExplorerResponse>.Success(response);
    }

    /// <summary>
    /// Tìm kiếm file tri thức theo tên, thư mục, người tạo, hoặc khoảng ngày tạo.
    /// </summary>
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

        // Xác thực thư mục tồn tại nếu người dùng chỉ định tìm trong thư mục cụ thể
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

#endregion
}
