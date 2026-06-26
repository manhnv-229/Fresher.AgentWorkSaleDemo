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

namespace Demo.Application.Services.Knowledge;

/// <summary>
/// Xử lý các thao tác ghi đối với file tri thức agent: upload, tải xuống, xem chi tiết, đổi tên, di chuyển, và xóa mềm file.
/// Bao gồm xử lý lỗi storage typed (unreachable/timed-out/rejected) và ánh xạ tên actor cho audit.
/// </summary>
public sealed class KnowledgeFileService(
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAgentKnowledgeRepository knowledgeRepository,
    IKnowledgeStorageService storageService,
    IAuthUserRepository authUserRepository,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork) : IKnowledgeFileService
{
#region Declaration

    /// <summary>
    /// Danh sách phần mở rộng file được phép upload. Dùng để xác thực loại file trước khi lưu vào storage.
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
    /// Upload file tri thức lên MinIO và lưu metadata vào database. Kiểm tra quyền ghi, dung lượng, loại file,
    /// chặn trùng nội dung trong cùng thư mục, và tái sử dụng storage object khi cùng agent đã có nội dung đó ở thư mục khác.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFileItem>> UploadFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid userId,
        string? ipAddress,
        KnowledgeUploadContent upload,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
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

        var name = KnowledgeServiceHelper.NormalizeDisplayName(Path.GetFileNameWithoutExtension(originalName));
        if (name is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.InvalidName, "File name is required.");
        }

        // Xác thực thư mục tồn tại nếu có chỉ định
        if (upload.FolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, upload.FolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var displayName = $"{name}{extension}";
        var normalizedName = KnowledgeServiceHelper.NormalizeName(displayName);
        // Buffer file content để tính checksum SHA256 và upload lên storage
        await using var buffered = new MemoryStream();
        await upload.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;
        var checksum = Convert.ToHexString(SHA256.HashData(buffered)).ToLowerInvariant();
        buffered.Position = 0;

        // Chặn upload cùng nội dung trong cùng thư mục để không tạo thêm metadata/object trùng tại một vị trí.
        if (await knowledgeRepository.ExactFileDuplicateExistsAsync(agentId, upload.FolderId, checksum, upload.Length, null, cancellationToken))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.ValidationError, "An identical file already exists in this folder.");
        }

        var fileId = Guid.NewGuid();
        var contentType = KnowledgeServiceHelper.NormalizeContentType(upload.ContentType, extension);
        var storageObject = await knowledgeRepository.FindReusableStorageObjectAsync(agentId, upload.FolderId, checksum, upload.Length, cancellationToken);
        if (storageObject is null)
        {
            var objectKey = KnowledgeServiceHelper.BuildStorageObjectKey(tenantId, agentId, fileId, extension);
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("Could not reach MinIO"))
            {
                return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.StorageUnreachable, "Storage service is unreachable. Check network connectivity and MinIO endpoint configuration.");
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.StorageTimedOut, "Storage operation timed out. Check storage timeout configuration and network latency.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("returned"))
            {
                return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.StorageRejected, $"Storage rejected the request: {ex.Message}");
            }
            catch (Exception)
            {
                return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.StorageUnavailable, "Could not upload file to storage.");
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
        // Resolve tên actor trước khi trả response vì navigation CreatedByUser chưa được nạp trên entity vừa tạo
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.file.upload", actorName, userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was uploaded.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(KnowledgeServiceHelper.MapFile(file, actorName));
    }

    /// <summary>
    /// Tải file tri thức từ MinIO về. Trả về nội dung file, tên hiển thị, content type, và dung lượng.
    /// Xử lý lỗi storage unreachable và timed-out.
    /// </summary>
    public async Task<ServiceResult<KnowledgeDownloadResult>> DownloadFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("Could not reach MinIO"))
        {
            return ServiceResult<KnowledgeDownloadResult>.Failure(KnowledgeErrorCodes.StorageUnreachable, "Storage service is unreachable.");
        }
        catch (OperationCanceledException)
        {
            return ServiceResult<KnowledgeDownloadResult>.Failure(KnowledgeErrorCodes.StorageTimedOut, "Storage download timed out.");
        }
        catch (Exception)
        {
            return ServiceResult<KnowledgeDownloadResult>.Failure(KnowledgeErrorCodes.StorageUnavailable, "Could not download file from storage.");
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết file tri thức bao gồm metadata storage và thông tin người tạo.
    /// </summary>
    public async Task<ServiceResult<KnowledgeFileDetail>> GetFileDetailAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureReadableAgentAsync(agentRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFileDetail>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: false, cancellationToken);
        if (file?.StorageObject is null)
        {
            return ServiceResult<KnowledgeFileDetail>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        return ServiceResult<KnowledgeFileDetail>.Success(KnowledgeServiceHelper.MapFileDetail(file));
    }

    /// <summary>
    /// Đổi tên file tri thức. Kiểm tra trùng tên trong cùng thư mục và ghi nhận audit log với tên actor đã resolve.
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
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
        if (access is not null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(access.Value.Code, access.Value.Message);
        }

        var file = await knowledgeRepository.GetFileAsync(agentId, fileId, trackChanges: true, cancellationToken);
        if (file is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FileNotFound, "File was not found.");
        }

        var name = KnowledgeServiceHelper.NormalizeDisplayName(command.Name);
        if (name is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.InvalidName, "File name is required.");
        }

        // Giữ nguyên extension của file gốc nếu người dùng không nhập
        var extension = file.Extension.StartsWith('.') ? file.Extension : $".{file.Extension}";
        var displayName = Path.HasExtension(name) ? name : $"{name}{extension}";
        var normalizedName = KnowledgeServiceHelper.NormalizeName(displayName);
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
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.file.rename", actorName, userId, tenantId, ipAddress, $"Knowledge file '{previousName}' was renamed to '{file.Name}'.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(KnowledgeServiceHelper.MapFile(file, actorName));
    }

    /// <summary>
    /// Di chuyển file đến thư mục đích. Kiểm tra thư mục đích tồn tại và trùng tên trong thư mục đích.
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
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
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
        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.file.move", actorName, userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was moved.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(KnowledgeServiceHelper.MapFile(file, actorName));
    }

    /// <summary>
    /// Xóa mềm file tri thức. Chỉ xóa vật lý object khỏi MinIO khi file đang xóa là active reference cuối cùng của storage object trong agent.
    /// Nếu xóa vật lý thất bại, metadata vẫn được giữ làm source of truth để rollback an toàn.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteFileAsync(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var access = await KnowledgeServiceHelper.EnsureWritableAgentAsync(agentRepository, tenantRepository, tenantId, agentId, cancellationToken);
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

        var actorName = await KnowledgeServiceHelper.ResolveActorNameAsync(authUserRepository, userId, cancellationToken);
        await RecordAuditAsync("knowledge.file.delete", actorName, userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was deleted.", "AgentKnowledgeFile", file.Id, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Ghi audit log cho các thao tác file với tên actor đã resolve từ authenticated user.
    /// </summary>
    private async Task RecordAuditAsync(
        string action,
        string actorName,
        Guid userId,
        Guid tenantId,
        string? ipAddress,
        string description,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        await auditLogService.RecordAsync(
            action,
            actorName,
            userId,
            KnowledgeServiceHelper.NormalizeTenantId(tenantId),
            ipAddress,
            description,
            targetType,
            targetId.ToString(),
            cancellationToken);
    }

#endregion
}
