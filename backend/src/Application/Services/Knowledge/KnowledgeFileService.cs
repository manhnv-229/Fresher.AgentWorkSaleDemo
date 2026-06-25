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

public sealed class KnowledgeFileService(
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAgentKnowledgeRepository knowledgeRepository,
    IKnowledgeStorageService storageService,
    IAuditLogService auditLogService,
    IUnitOfWork unitOfWork) : IKnowledgeFileService
{
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

        if (upload.FolderId is not null &&
            await knowledgeRepository.GetFolderAsync(agentId, upload.FolderId.Value, trackChanges: false, cancellationToken) is null)
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.FolderNotFound, "Folder was not found.");
        }

        var displayName = $"{name}{extension}";
        var normalizedName = KnowledgeServiceHelper.NormalizeName(displayName);
        if (await knowledgeRepository.FileNameExistsAsync(agentId, upload.FolderId, normalizedName, null, cancellationToken))
        {
            return ServiceResult<KnowledgeFileItem>.Failure(KnowledgeErrorCodes.ValidationError, "A file with the same name already exists.");
        }

        await using var buffered = new MemoryStream();
        await upload.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;
        var checksum = Convert.ToHexString(SHA256.HashData(buffered)).ToLowerInvariant();
        buffered.Position = 0;

        var fileId = Guid.NewGuid();
        var objectKey = $"tenants/{tenantId:N}/agents/{agentId:N}/knowledge/{fileId:N}{extension}";
        KnowledgeStorageUploadResult storageResult;
        try
        {
            storageResult = await storageService.UploadAsync(
                new KnowledgeStorageUploadRequest(
                    buffered,
                    storageService.BucketName,
                    objectKey,
                    KnowledgeServiceHelper.NormalizeContentType(upload.ContentType, extension),
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

        var storageObject = new KnowledgeStorageObject
        {
            Id = Guid.NewGuid(),
            StorageBucket = storageResult.Bucket,
            StorageObjectKey = storageResult.ObjectKey,
            StorageEtag = storageResult.ETag,
            StorageVersionId = storageResult.VersionId,
            ChecksumSha256 = checksum,
            SizeBytes = upload.Length,
            ContentType = KnowledgeServiceHelper.NormalizeContentType(upload.ContentType, extension),
            Status = KnowledgeStorageObjectStatus.Active,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
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

        knowledgeRepository.AddStorageObject(storageObject);
        knowledgeRepository.AddFile(file);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync("knowledge.file.upload", userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was uploaded.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(KnowledgeServiceHelper.MapFile(file));
    }

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
        await RecordAuditAsync("knowledge.file.rename", userId, tenantId, ipAddress, $"Knowledge file '{previousName}' was renamed to '{file.Name}'.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(KnowledgeServiceHelper.MapFile(file));
    }

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

        file.FolderId = command.TargetFolderId;
        file.ModifiedByUserId = userId;
        file.ModifiedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync("knowledge.file.move", userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was moved.", "AgentKnowledgeFile", file.Id, cancellationToken);

        return ServiceResult<KnowledgeFileItem>.Success(KnowledgeServiceHelper.MapFile(file));
    }

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

        file.Status = AgentKnowledgeFileStatus.Deleted;
        file.DeletedAt = DateTime.UtcNow;
        file.ModifiedAt = DateTime.UtcNow;
        file.ModifiedByUserId = userId;
        file.StorageObject.DeletedAt = DateTime.UtcNow;
        file.StorageObject.Status = KnowledgeStorageObjectStatus.Deleted;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await storageService.DeleteAsync(file.StorageObject.StorageBucket, file.StorageObject.StorageObjectKey, cancellationToken);
        }
        catch
        {
            // Soft-delete metadata remains the source of truth even if physical cleanup is delayed.
        }

        await RecordAuditAsync("knowledge.file.delete", userId, tenantId, ipAddress, $"Knowledge file '{file.Name}' was deleted.", "AgentKnowledgeFile", file.Id, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

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
        await auditLogService.RecordAsync(
            action,
            "System",
            userId,
            tenantId,
            ipAddress,
            description,
            targetType,
            targetId.ToString(),
            cancellationToken);
    }
}
