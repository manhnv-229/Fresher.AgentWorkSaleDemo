using System.Security.Cryptography;

using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

namespace Demo.Application.Services;

/// <summary>
/// Legacy service tích hợp cho quản lý tri thức agent: explorer, CRUD thư mục, upload/download/search file.
/// Được giữ lại để backward compatibility. Ưu tiên sử dụng split services mới: KnowledgeExplorerService, KnowledgeFolderService, KnowledgeFileService.
/// </summary>
public sealed class AgentKnowledgeService(
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAgentKnowledgeRepository knowledgeRepository,
    IKnowledgeStorageService storageService,
    IAuditLogService auditLogService,
    IAuthUserRepository authUserRepository,
    IUnitOfWork unitOfWork) : IAgentKnowledgeService
{
#region Declaration

    /// <summary>
    /// Danh sách phần mở rộng file được phép upload.
    /// </summary>
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".xlsx",
        ".pptx",
        ".txt",
        ".png",
        ".jpg",
        ".jpeg"
    };

#endregion

#region Method

    /// <summary>
    /// Tải trạng thái explorer hoàn chỉnh cho agent: cây thư mục, breadcrumb, thư mục con, và file trong thư mục được chọn.
    /// </summary>
    public async Task<ServiceResult<KnowledgeExplorerResponse>> GetExplorerAsync(
        Guid tenantId,
        Guid agentId,
        Guid? folderId,
        CancellationToken cancellationToken)
    {
        var access = await EnsureReadableAgentAsync(tenantId, agentId, cancellationToken);
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
            BuildTree(folders.Where(folder => folder.DeletedAt is null).ToList(), null),
            BuildBreadcrumb(folders, selectedFolder),
            childFolders.Select(MapFolder).ToList(),
            files.Select(MapFile).ToList());

        return ServiceResult<KnowledgeExplorerResponse>.Success(response);
    }

    /// <summary>
    /// Tìm kiếm file tri thức theo nhiều tiêu chí.
    /// </summary>
    public async Task<ServiceResult<IReadOnlyList<KnowledgeFileItem>>> SearchFilesAsync(
        Guid tenantId,
        Guid agentId,
        KnowledgeSearchFilters filters,
        CancellationToken cancellationToken)
    {
        var access = await EnsureReadableAgentAsync(tenantId, agentId, cancellationToken);
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
            NormalizeName(filters.Name),
            filters.FolderId,
            filters.CreatedByUserId,
            filters.CreatedFrom,
            filters.CreatedTo,
            cancellationToken);

        return ServiceResult<IReadOnlyList<KnowledgeFileItem>>.Success(files.Select(MapFile).ToList());
    }

    /// <summary>
    /// Tạo mới thư mục tri thức agent.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFolderItem>> CreateFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid userId,
        string? ipAddress,
        CreateKnowledgeFolderCommand command,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var name = NormalizeDisplayName(command.Name);
        if (name is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidName, "Folder name is required.");
        }

        if (command.ParentFolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, command.ParentFolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Parent folder was not found.");
        }

        var normalizedName = NormalizeName(name);
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
        var actorUser = await authUserRepository.GetByIdAsync(userId, cancellationToken);
        var actorName = actorUser?.FullName ?? actorUser?.Email ?? "Unknown";
        await RecordAuditAsync("knowledge.folder.create", userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was created.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(MapFolder(folder, actorName));
    }

    /// <summary>
    /// Đổi tên thư mục tri thức.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFolderItem>> RenameFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        RenameKnowledgeItemCommand command,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var folder = await knowledgeRepository.GetFolderAsync(agentId, folderId, trackChanges: true, cancellationToken);
        if (folder is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var ownerAccess = EnsureFolderOwnerAccess(folder, userId);
        if (ownerAccess is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(ownerAccess.Value.Code, ownerAccess.Value.Message);
        }

        var name = NormalizeDisplayName(command.Name);
        if (name is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidName, "Folder name is required.");
        }

        var normalizedName = NormalizeName(name);
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

        return ServiceResult<KnowledgeFolderItem>.Success(MapFolder(folder));
    }

    /// <summary>
    /// Di chuyển thư mục đến thư mục đích. Kiểm tra ràng buộc con cháu và trùng tên.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFolderItem>> MoveFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        MoveKnowledgeItemCommand command,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var folder = await knowledgeRepository.GetFolderAsync(agentId, folderId, trackChanges: true, cancellationToken);
        if (folder is null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var ownerAccess = EnsureFolderOwnerAccess(folder, userId);
        if (ownerAccess is not null)
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(ownerAccess.Value.Code, ownerAccess.Value.Message);
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

            if (IsDescendant(allFolders, folder.Id, target.Id))
            {
                return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.InvalidMove, "Folder cannot be moved into its descendant.");
            }
        }

        var normalizedName = NormalizeName(folder.Name);
        if (await knowledgeRepository.FolderNameExistsAsync(agentId, command.TargetFolderId, normalizedName, folder.Id, cancellationToken))
        {
            return ServiceResult<KnowledgeFolderItem>.Failure(KnowledgeErrorCodes.ValidationError, "A folder with the same name already exists in the target folder.");
        }

        folder.ParentFolderId = command.TargetFolderId;
        folder.ModifiedByUserId = userId;
        folder.ModifiedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync("knowledge.folder.move", userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was moved.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<KnowledgeFolderItem>.Success(MapFolder(folder));
    }

    /// <summary>
    /// Xóa mềm toàn bộ subtree thư mục và file thuộc subtree.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteFolderAsync(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<bool>.Failure(access.Value.Code, access.Value.Message);
        }

        var folder = await knowledgeRepository.GetFolderAsync(agentId, folderId, trackChanges: true, cancellationToken);
        if (folder is null)
        {
            return ServiceResult<bool>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var ownerAccess = EnsureFolderOwnerAccess(folder, userId);
        if (ownerAccess is not null)
        {
            return ServiceResult<bool>.Failure(ownerAccess.Value.Code, ownerAccess.Value.Message);
        }

        var allFolders = await knowledgeRepository.GetFoldersAsync(agentId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var descendant in allFolders.Where(item => item.Id == folder.Id || IsDescendant(allFolders, folder.Id, item.Id)))
        {
            descendant.DeletedAt = now;
            descendant.ModifiedAt = now;
            descendant.ModifiedByUserId = userId;
        }

        var folderIds = allFolders
            .Where(item => item.Id == folder.Id || IsDescendant(allFolders, folder.Id, item.Id))
            .Select(item => item.Id)
            .ToHashSet();
        var files = await knowledgeRepository.SearchFilesAsync(agentId, null, null, null, null, null, cancellationToken);
        foreach (var file in files.Where(file => file.FolderId is not null && folderIds.Contains(file.FolderId.Value)))
        {
            file.DeletedAt = now;
            file.ModifiedAt = now;
            file.ModifiedByUserId = userId;
            file.Status = AgentKnowledgeFileStatus.Deleted;
            if (file.StorageObject is not null)
            {
                file.StorageObject.DeletedAt = now;
                file.StorageObject.Status = KnowledgeStorageObjectStatus.Deleted;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync("knowledge.folder.delete", userId, tenantId, ipAddress, $"Knowledge folder '{folder.Name}' was deleted.", "AgentKnowledgeFolder", folder.Id, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Upload file tri thức lên MinIO và lưu metadata vào database.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFileItem>> UploadFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid userId,
        string? ipAddress,
        KnowledgeUploadContent upload,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(access.Value.Code, access.Value.Message);
        }

        if (upload.Length <= 0)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.EmptyFile, "File is empty.");
        }

        if (upload.Length > storageService.MaxUploadBytes)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FileTooLarge, "File exceeds the configured upload size limit.");
        }

        var originalName = Path.GetFileName(upload.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.UnsupportedFileType, "File type is not supported.");
        }

        var name = NormalizeDisplayName(Path.GetFileNameWithoutExtension(originalName));
        if (name is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.InvalidName, "File name is required.");
        }

        if (upload.FolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, upload.FolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var displayName = $"{name}{extension}";
        var normalizedName = NormalizeName(displayName);
        await using var buffered = new MemoryStream();
        await upload.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;
        var checksum = Convert.ToHexString(SHA256.HashData(buffered)).ToLowerInvariant();
        buffered.Position = 0;

        // Chặn upload cùng nội dung trong cùng thư mục để tránh tạo thêm metadata/object trùng tại một vị trí.
        if (await knowledgeRepository.ExactFileDuplicateExistsAsync(agentId, upload.FolderId, checksum, upload.Length, null, cancellationToken))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.ValidationError, "An identical file already exists in this folder.");
        }

        var fileId = Guid.NewGuid();
        var contentType = NormalizeContentType(upload.ContentType, extension);
        var storageObject = await knowledgeRepository.FindReusableStorageObjectAsync(agentId, upload.FolderId, checksum, upload.Length, cancellationToken);
        if (storageObject is null)
        {
            var objectKey = $"tenants/{tenantId:N}/agents/{agentId:N}/knowledge/{fileId:N}{extension}";
            KnowledgeStorageUploadResult storageResult;
            try
            {
                storageResult = await storageService.UploadAsync(
                    new KnowledgeStorageUploadRequest(
                        buffered,
                        storageService.BucketName,
                        objectKey,
                        contentType,
                        upload.Length,
                        checksum),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.StorageUnavailable, exception.Message);
            }

            storageObject = new KnowledgeStorageObject
            {
                Id = Guid.NewGuid(),
                StorageBucket = storageResult.Bucket,
                StorageObjectKey = storageResult.ObjectKey,
                StorageEtag = storageResult.ETag,
                StorageVersionId = storageResult.VersionId,
                ChecksumSha256 = checksum,
                SizeBytes = upload.Length,
                ContentType = contentType,
                Status = KnowledgeStorageObjectStatus.Active,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            knowledgeRepository.AddStorageObject(storageObject);
        }

        var file = new AgentKnowledgeFile
        {
            Id = fileId,
            AgentId = agentId,
            FolderId = upload.FolderId,
            StorageObjectId = storageObject.Id,
            StorageObject = storageObject,
            Name = displayName,
            NormalizedName = normalizedName,
            OriginalName = originalName,
            Extension = extension.TrimStart('.'),
            Status = AgentKnowledgeFileStatus.Active,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        knowledgeRepository.AddFile(file);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var actorUser = await authUserRepository.GetByIdAsync(userId, cancellationToken);
        var actorName = actorUser?.FullName ?? actorUser?.Email ?? "Unknown";
        await RecordAuditAsync("knowledge.file.upload", userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was uploaded.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(MapFile(file, actorName));
    }

    /// <summary>
    /// Tải file tri thức từ MinIO về.
    /// </summary>
    public async Task<ServiceResult<KnowledgeDownloadResult>> DownloadFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var access = await EnsureReadableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeDownloadResult>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: false, cancellationToken);
        if (file?.StorageObject is null)
        {
            return ServiceResult<KnowledgeDownloadResult>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        try
        {
            var download = await storageService.DownloadAsync(file.StorageObject.StorageBucket, file.StorageObject.StorageObjectKey, cancellationToken);
            return ServiceResult<KnowledgeDownloadResult>.Success(new KnowledgeDownloadResult(
                download.Content,
                file.Name,
                download.ContentType,
                download.SizeBytes ?? file.StorageObject.SizeBytes));
        }
        catch (Exception exception)
        {
            return ServiceResult<KnowledgeDownloadResult>.Failure(KnowledgeErrorCodes.StorageUnavailable, exception.Message);
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết file tri thức.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFileDetail>> GetFileDetailAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var access = await EnsureReadableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFileDetail>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: false, cancellationToken);
        if (file?.StorageObject is null)
        {
            return ServiceResult<KnowledgeFileDetail>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        return ServiceResult<KnowledgeFileDetail>.Success(MapFileDetail(file));
    }

    /// <summary>
    /// Đổi tên file tri thức.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFileItem>> RenameFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        RenameKnowledgeItemCommand command,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: true, cancellationToken);
        if (file is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        var name = NormalizeDisplayName(command.Name);
        if (name is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.InvalidName, "File name is required.");
        }

        var extension = file.Extension.StartsWith('.') ? file.Extension : $".{file.Extension}";
        var displayName = Path.HasExtension(name) ? name : $"{name}{extension}";
        var normalizedName = NormalizeName(displayName);
        if (await knowledgeRepository.FileNameExistsAsync(agentId, file.FolderId, normalizedName, file.Id, cancellationToken))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.ValidationError, "A file with the same name already exists.");
        }

        var previousName = file.Name;
        file.Name = displayName;
        file.NormalizedName = normalizedName;
        file.ModifiedByUserId = userId;
        file.ModifiedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync("knowledge.file.rename", userId, tenantId, ipAddress, $"Knowledge file '{previousName}' was renamed to '{file.Name}'.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(MapFile(file));
    }

    /// <summary>
    /// Di chuyển file đến thư mục đích.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFileItem>> MoveFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        MoveKnowledgeItemCommand command,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: true, cancellationToken);
        if (file is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        if (command.TargetFolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, command.TargetFolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Target folder was not found.");
        }

        if (await knowledgeRepository.FileNameExistsAsync(agentId, command.TargetFolderId, file.NormalizedName, file.Id, cancellationToken))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.ValidationError, "A file with the same name already exists in the target folder.");
        }

        var currentChecksum = file.StorageObject?.ChecksumSha256;
        var currentSize = file.StorageObject?.SizeBytes ?? 0;
        if (currentChecksum is not null &&
            await knowledgeRepository.ExactFileDuplicateExistsAsync(agentId, command.TargetFolderId, currentChecksum, currentSize, file.Id, cancellationToken))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.ValidationError, "An identical file already exists in the target folder.");
        }

        file.FolderId = command.TargetFolderId;
        file.ModifiedByUserId = userId;
        file.ModifiedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync("knowledge.file.move", userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was moved.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(MapFile(file));
    }

    /// <summary>
    /// Xóa mềm file tri thức và xóa vật lý object khỏi MinIO.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var access = await EnsureWritableAgentAsync(tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<bool>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: true, cancellationToken);
        if (file?.StorageObject is null)
        {
            return ServiceResult<bool>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        var now = DateTime.UtcNow;
        var hasOtherReferences = await knowledgeRepository.HasOtherActiveFileReferencesAsync(agentId, file.StorageObjectId, file.Id, cancellationToken);

        file.Status = AgentKnowledgeFileStatus.Deleted;
        file.DeletedAt = now;
        file.ModifiedAt = now;
        file.ModifiedByUserId = userId;
        if (!hasOtherReferences)
        {
            file.StorageObject.DeletedAt = now;
            file.StorageObject.Status = KnowledgeStorageObjectStatus.Deleted;
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!hasOtherReferences)
        {
            try
            {
                await storageService.DeleteAsync(file.StorageObject.StorageBucket, file.StorageObject.StorageObjectKey, cancellationToken);
            }
            catch
            {
                // Soft-delete metadata remains the source of truth even if physical cleanup is delayed.
            }
        }

        await RecordAuditAsync("knowledge.file.delete", userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was deleted.", "AgentKnowledgeFile", file.Id, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

#endregion

#region Declaration

    /// <summary>
    /// Kiểm tra agent tồn tại và có thể đọc được.
    /// </summary>
    private async Task<(string Code, string Message)?> EnsureReadableAgentAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        return agent is null ? (KnowledgeErrorCodes.AgentNotFound, "Agent was not found.") : null;
    }

    /// <summary>
    /// Kiểm tra tenant tồn tại, không bị khóa, và agent có thể ghi được.
    /// </summary>
    private async Task<(string Code, string Message)?> EnsureWritableAgentAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return (KnowledgeErrorCodes.TenantNotFound, "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return (KnowledgeErrorCodes.TenantLocked, "Cannot modify knowledge in a locked tenant.");
        }

        return await EnsureReadableAgentAsync(tenantId, agentId, cancellationToken);
    }

    /// <summary>
    /// Ghi audit log với tên actor được resolve từ authenticated user.
    /// </summary>
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
        var user = await authUserRepository.GetByIdAsync(userId, cancellationToken);
        await auditLogService.RecordAsync(
            action,
            user?.FullName ?? user?.Email ?? "Unknown",
            userId,
            tenantId,
            ipAddress,
            description,
            targetType,
            targetId.ToString(),
            cancellationToken);
    }

    /// <summary>
    /// Ánh xạ entity folder sang DTO với tên người tạo đã resolve trước.
    /// </summary>
    private static KnowledgeFolderItem MapFolder(AgentKnowledgeFolder folder, string createdByUserName) => new(
        folder.Id,
        folder.ParentFolderId,
        folder.Name,
        folder.CreatedByUserId,
        createdByUserName,
        folder.CreatedAt,
        folder.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity folder sang DTO.
    /// </summary>
    private static KnowledgeFolderItem MapFolder(AgentKnowledgeFolder folder) => new(
        folder.Id,
        folder.ParentFolderId,
        folder.Name,
        folder.CreatedByUserId,
        folder.CreatedByUser?.FullName ?? folder.CreatedByUser?.Email ?? "Unknown",
        folder.CreatedAt,
        folder.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity file sang DTO với tên người tạo đã resolve trước.
    /// </summary>
    private static KnowledgeFileItem MapFile(AgentKnowledgeFile file, string createdByUserName) => new(
        file.Id,
        file.FolderId,
        file.Name,
        file.OriginalName,
        file.Extension,
        file.StorageObject?.ContentType ?? "application/octet-stream",
        file.StorageObject?.SizeBytes ?? 0,
        file.Status.ToString(),
        file.CreatedByUserId,
        createdByUserName,
        file.CreatedAt,
        file.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity file sang DTO.
    /// </summary>
    private static KnowledgeFileItem MapFile(AgentKnowledgeFile file) => new(
        file.Id,
        file.FolderId,
        file.Name,
        file.OriginalName,
        file.Extension,
        file.StorageObject?.ContentType ?? "application/octet-stream",
        file.StorageObject?.SizeBytes ?? 0,
        file.Status.ToString(),
        file.CreatedByUserId,
        file.CreatedByUser?.FullName ?? file.CreatedByUser?.Email ?? "Unknown",
        file.CreatedAt,
        file.ModifiedAt);

    /// <summary>
    /// Ánh xạ entity file sang DTO chi tiết.
    /// </summary>
    private static KnowledgeFileDetail MapFileDetail(AgentKnowledgeFile file) => new(
        file.Id,
        file.FolderId,
        file.Name,
        file.OriginalName,
        file.Extension,
        file.StorageObject?.ContentType ?? "application/octet-stream",
        file.StorageObject?.SizeBytes ?? 0,
        file.Status.ToString(),
        file.StorageObject?.StorageBucket ?? string.Empty,
        file.StorageObject?.StorageObjectKey ?? string.Empty,
        file.CreatedByUserId,
        file.CreatedByUser?.FullName ?? file.CreatedByUser?.Email ?? "Unknown",
        file.CreatedAt,
        file.ModifiedAt);

    /// <summary>
    /// Xây dựng cấu trúc cây thư mục từ danh sách phẳng.
    /// </summary>
    private static IReadOnlyList<KnowledgeFolderTreeItem> BuildTree(IReadOnlyList<AgentKnowledgeFolder> folders, Guid? parentFolderId)
    {
        return folders
            .Where(folder => folder.ParentFolderId == parentFolderId)
            .OrderBy(folder => folder.Name)
            .Select(folder => new KnowledgeFolderTreeItem(folder.Id, folder.ParentFolderId, folder.Name, BuildTree(folders, folder.Id)))
            .ToList();
    }

    /// <summary>
    /// Xây dựng breadcrumb từ thư mục được chọn lên root.
    /// </summary>
    private static IReadOnlyList<KnowledgeBreadcrumbItem> BuildBreadcrumb(
        IReadOnlyList<AgentKnowledgeFolder> folders,
        AgentKnowledgeFolder? selectedFolder)
    {
        if (selectedFolder is null)
        {
            return [];
        }

        var byId = folders.ToDictionary(folder => folder.Id);
        var path = new List<KnowledgeBreadcrumbItem>();
        var current = selectedFolder;
        while (current is not null)
        {
            path.Add(new KnowledgeBreadcrumbItem(current.Id, current.Name));
            current = current.ParentFolderId is not null && byId.TryGetValue(current.ParentFolderId.Value, out var parent)
                ? parent
                : null;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Kiểm tra candidateId có phải là con cháu của ancestorId không.
    /// </summary>
    private static bool IsDescendant(IReadOnlyList<AgentKnowledgeFolder> folders, Guid ancestorId, Guid candidateId)
    {
        var byId = folders.ToDictionary(folder => folder.Id);
        var currentId = candidateId;
        while (byId.TryGetValue(currentId, out var current) && current.ParentFolderId is not null)
        {
            if (current.ParentFolderId == ancestorId)
            {
                return true;
            }

            currentId = current.ParentFolderId.Value;
        }

        return false;
    }

    /// <summary>
    /// Normalize tên hiển thị: trim whitespace và trả về null nếu rỗng.
    /// </summary>
    private static string? NormalizeDisplayName(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    /// <summary>
    /// Normalize tên để so sánh: trim và chuyển sang uppercase.
    /// </summary>
    private static string NormalizeName(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static (string Code, string Message)? EnsureFolderOwnerAccess(AgentKnowledgeFolder folder, Guid userId)
    {
        return folder.CreatedByUserId == userId
            ? null
            : (KnowledgeErrorCodes.FolderOwnerRequired, "Only the folder creator can rename, move, or delete this folder.");
    }

    /// <summary>
    /// Xác định content type từ extension nếu không có content type từ client.
    /// </summary>
    private static string NormalizeContentType(string contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

#endregion
}
