using Demo.Application.Interfaces.Repository;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Repositories;

/// <summary>
/// Repository truy vấn và cập nhật dữ liệu tri thức agent từ MySQL database. Bao gồm query thư mục, file, search, và uniqueness check.
/// </summary>
public sealed class AgentKnowledgeRepository(DemoDbContext dbContext) : IAgentKnowledgeRepository
{
#region Method

    /// <summary>
    /// Lấy tất cả thư mục active của agent, sắp xếp theo tên. Dùng để xây dựng tree và kiểm tra uniqueness.
    /// </summary>
    public async Task<IReadOnlyList<AgentKnowledgeFolder>> GetFoldersAsync(Guid agentId, CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFolders
            .Include(folder => folder.CreatedByUser)
            .Where(folder => folder.AgentId == agentId && folder.DeletedAt == null)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy thư mục con trực tiếp của một thư mục cha. Dùng để hiển thị nội dung explorer.
    /// </summary>
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

    /// <summary>
    /// Lấy một thư mục theo ID với tùy chọn track changes. Dùng để validate và cập nhật thư mục.
    /// </summary>
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

    /// <summary>
    /// Lấy danh sách thư mục con trực tiếp của một thư mục ancestor. Dùng để xác nhận ràng buộc khi di chuyển/xóa.
    /// </summary>
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

    /// <summary>
    /// Tìm kiếm thư mục theo tên. Dùng cho unified knowledge search để trả kết quả folder từ backend.
    /// </summary>
    public async Task<IReadOnlyList<AgentKnowledgeFolder>> SearchFoldersAsync(
        Guid agentId,
        string? normalizedName,
        CancellationToken cancellationToken)
    {
        var folders = await dbContext.AgentKnowledgeFolders
            .AsNoTracking()
            .Include(folder => folder.CreatedByUser)
            .Where(folder =>
                folder.AgentId == agentId &&
                folder.DeletedAt == null)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return folders;
        }

        return folders
            .Select(folder => new
            {
                Folder = folder,
                Score = KnowledgeSearchHelper.GetMatchScore(normalizedName, folder.NormalizedName)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Folder.CreatedAt)
            .ThenBy(item => item.Folder.Name)
            .Select(item => item.Folder)
            .ToList();
    }

    /// <summary>
    /// Kiểm tra tên thư mục đã tồn tại trong cùng thư mục cha. Dùng để validate uniqueness khi tạo/sửa tên.
    /// </summary>
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

    /// <summary>
    /// Thêm mới thư mục tri thức vào DbContext. Changes sẽ được lưu khi gọi UnitOfWork.SaveChangesAsync.
    /// </summary>
    public void AddFolder(AgentKnowledgeFolder folder)
    {
        dbContext.AgentKnowledgeFolders.Add(folder);
    }

    /// <summary>
    /// Lấy tất cả file active trong một thư mục, sắp xếp theo tên. Dùng để hiển thị nội dung explorer.
    /// </summary>
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

    /// <summary>
    /// Lấy tất cả file active thuộc danh sách thư mục IDs. Dùng để soft-delete file khi xóa subtree thư mục.
    /// </summary>
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

    /// <summary>
    /// Tìm kiếm file theo nhiều tiêu chí: tên, thư mục, người tạo, khoảng ngày tạo. Hỗ trợ query linh hoạt.
    /// </summary>
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

        var files = await query
            .OrderByDescending(file => file.CreatedAt)
            .ThenBy(file => file.Name)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return files;
        }

        return files
            .Select(file => new
            {
                File = file,
                Score = KnowledgeSearchHelper.GetMatchScore(normalizedName, file.NormalizedName)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.File.CreatedAt)
            .ThenBy(item => item.File.Name)
            .Select(item => item.File)
            .ToList();
    }

    /// <summary>
    /// Lấy một file theo ID với tùy chọn track changes. Dùng để validate và cập nhật file.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra tên file đã tồn tại trong cùng thư mục. Dùng để validate uniqueness khi tạo/sửa tên.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra trong cùng thư mục của agent đã có file active trùng nội dung theo checksum hay chưa.
    /// Dùng để chặn upload cùng nội dung trong cùng thư mục nhưng vẫn cho phép ở thư mục khác.
    /// </summary>
    public async Task<bool> ExactFileDuplicateExistsAsync(
        Guid agentId,
        Guid? folderId,
        string checksumSha256,
        long sizeBytes,
        Guid? excludeFileId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFiles.AnyAsync(file =>
            file.AgentId == agentId &&
            file.FolderId == folderId &&
            file.DeletedAt == null &&
            file.Status == AgentKnowledgeFileStatus.Active &&
            (excludeFileId == null || file.Id != excludeFileId.Value) &&
            file.StorageObject != null &&
            file.StorageObject.DeletedAt == null &&
            file.StorageObject.Status == KnowledgeStorageObjectStatus.Active &&
            file.StorageObject.ChecksumSha256 == checksumSha256 &&
            file.StorageObject.SizeBytes == sizeBytes,
            cancellationToken);
    }

    /// <summary>
    /// Tìm storage object đang active có thể tái sử dụng trong cùng agent nhưng ở thư mục khác.
    /// </summary>
    public async Task<KnowledgeStorageObject?> FindReusableStorageObjectAsync(
        Guid agentId,
        Guid? folderId,
        string checksumSha256,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFiles
            .AsNoTracking()
            .Where(file =>
                file.AgentId == agentId &&
                file.FolderId != folderId &&
                file.DeletedAt == null &&
                file.Status == AgentKnowledgeFileStatus.Active &&
                file.StorageObject != null &&
                file.StorageObject.DeletedAt == null &&
                file.StorageObject.Status == KnowledgeStorageObjectStatus.Active &&
                file.StorageObject.ChecksumSha256 == checksumSha256 &&
                file.StorageObject.SizeBytes == sizeBytes)
            .Select(file => file.StorageObject)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Kiểm tra còn file active nào khác trong cùng agent đang tham chiếu storage object này hay không.
    /// </summary>
    public async Task<bool> HasOtherActiveFileReferencesAsync(
        Guid agentId,
        Guid storageObjectId,
        Guid excludeFileId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AgentKnowledgeFiles.AnyAsync(file =>
            file.AgentId == agentId &&
            file.StorageObjectId == storageObjectId &&
            file.Id != excludeFileId &&
            file.DeletedAt == null &&
            file.Status == AgentKnowledgeFileStatus.Active,
            cancellationToken);
    }

    /// <summary>
    /// Thêm mới file tri thức vào DbContext. Changes sẽ được lưu khi gọi UnitOfWork.SaveChangesAsync.
    /// </summary>
    public void AddFile(AgentKnowledgeFile file)
    {
        dbContext.AgentKnowledgeFiles.Add(file);
    }

    /// <summary>
    /// Thêm mới storage object vào DbContext. Changes sẽ được lưu khi gọi UnitOfWork.SaveChangesAsync.
    /// </summary>
    public void AddStorageObject(KnowledgeStorageObject storageObject)
    {
        dbContext.KnowledgeStorageObjects.Add(storageObject);
    }

#endregion
}
