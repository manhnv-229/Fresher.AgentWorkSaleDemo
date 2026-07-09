using System.Security.Cryptography;

using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

using Microsoft.Extensions.Logging;

using Moq;

namespace Demo.Application.Tests.Services;

/// <summary>
/// Kiểm tra các luồng upload và di chuyển file tri thức theo checksum, storage object và folder đích.
/// </summary>
public sealed class KnowledgeFileServiceTests
{
    [Fact]
    public async Task UploadFileAsync_ShouldRejectDuplicateChecksumInSameFolder()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var context = CreateContext(agentId, userId);
        var upload = CreateUpload("Handbook.pdf", "application/pdf", "same-content", folderId);
        var checksum = CalculateChecksum("same-content");

        context.KnowledgeRepository
            .Setup(repository => repository.GetFolderAsync(agentId, folderId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentKnowledgeFolder { Id = folderId, AgentId = agentId, Name = "Folder A" });
        context.KnowledgeRepository
            .Setup(repository => repository.ExactFileDuplicateExistsAsync(agentId, folderId, checksum, upload.Length, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await context.Service.UploadFileAsync(Guid.Empty, agentId, userId, "127.0.0.1", upload, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(KnowledgeErrorCodes.ValidationError);
        context.StorageService.Verify(service => service.UploadAsync(It.IsAny<KnowledgeStorageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        context.KnowledgeRepository.Verify(repository => repository.AddStorageObject(It.IsAny<KnowledgeStorageObject>()), Times.Never);
        context.KnowledgeRepository.Verify(repository => repository.AddFile(It.IsAny<AgentKnowledgeFile>()), Times.Never);
        context.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadFileAsync_ShouldUploadNewStorageObject_WhenNoReusableObjectExists()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = CreateContext(agentId, userId);
        var upload = CreateUpload("Handbook.pdf", "", "brand-new-content");
        var checksum = CalculateChecksum("brand-new-content");

        context.KnowledgeRepository
            .Setup(repository => repository.ExactFileDuplicateExistsAsync(agentId, null, checksum, upload.Length, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        context.KnowledgeRepository
            .Setup(repository => repository.FindReusableStorageObjectAsync(agentId, null, checksum, upload.Length, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStorageObject?)null);
        context.StorageService
            .Setup(service => service.UploadAsync(It.IsAny<KnowledgeStorageUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeStorageUploadResult("knowledge", "internal/object.pdf", "etag-1", "version-1"));

        KnowledgeStorageObject? createdStorageObject = null;
        AgentKnowledgeFile? createdFile = null;
        context.KnowledgeRepository
            .Setup(repository => repository.AddStorageObject(It.IsAny<KnowledgeStorageObject>()))
            .Callback<KnowledgeStorageObject>(storageObject => createdStorageObject = storageObject);
        context.KnowledgeRepository
            .Setup(repository => repository.AddFile(It.IsAny<AgentKnowledgeFile>()))
            .Callback<AgentKnowledgeFile>(file => createdFile = file);

        var result = await context.Service.UploadFileAsync(Guid.Empty, agentId, userId, "127.0.0.1", upload, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        createdStorageObject.Should().NotBeNull();
        createdStorageObject!.ChecksumSha256.Should().Be(checksum);
        createdStorageObject.ContentType.Should().Be("application/pdf");
        createdStorageObject.StorageBucket.Should().Be("knowledge");
        createdStorageObject.StorageObjectKey.Should().Be("internal/object.pdf");

        createdFile.Should().NotBeNull();
        createdFile!.StorageObject.Should().BeSameAs(createdStorageObject);
        createdFile.Name.Should().Be("Handbook.pdf");
        createdFile.Extension.Should().Be("pdf");
        createdFile.NormalizedName.Should().Be("HANDBOOK.PDF");

        context.StorageService.Verify(
            service => service.UploadAsync(
                It.Is<KnowledgeStorageUploadRequest>(request =>
                    request.Bucket == "knowledge" &&
                    request.ContentType == "application/pdf" &&
                    request.SizeBytes == upload.Length &&
                    request.PayloadSha256 == checksum),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.CacheVersionService.Verify(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        context.AuditLogService.Verify(
            service => service.RecordAsync(
                "knowledge.file.upload",
                "Uploader Name",
                userId,
                null,
                "127.0.0.1",
                It.Is<string>(value => value.Contains("Handbook.pdf", StringComparison.Ordinal)),
                "AgentKnowledgeFile",
                createdFile.Id.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_ShouldReuseExistingStorageObject_WhenChecksumMatches()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingStorageObject = new KnowledgeStorageObject
        {
            Id = Guid.NewGuid(),
            StorageBucket = "knowledge",
            StorageObjectKey = "reused/object.pdf",
            ChecksumSha256 = CalculateChecksum("reused-content"),
            SizeBytes = 14,
            ContentType = "application/pdf",
            Status = KnowledgeStorageObjectStatus.Active
        };
        var context = CreateContext(agentId, userId);
        var upload = CreateUpload("Handbook.pdf", "application/pdf", "reused-content");
        var checksum = CalculateChecksum("reused-content");

        context.KnowledgeRepository
            .Setup(repository => repository.ExactFileDuplicateExistsAsync(agentId, null, checksum, upload.Length, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        context.KnowledgeRepository
            .Setup(repository => repository.FindReusableStorageObjectAsync(agentId, null, checksum, upload.Length, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStorageObject);

        AgentKnowledgeFile? createdFile = null;
        context.KnowledgeRepository
            .Setup(repository => repository.AddFile(It.IsAny<AgentKnowledgeFile>()))
            .Callback<AgentKnowledgeFile>(file => createdFile = file);

        var result = await context.Service.UploadFileAsync(Guid.Empty, agentId, userId, null, upload, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        createdFile.Should().NotBeNull();
        createdFile!.StorageObjectId.Should().Be(existingStorageObject.Id);
        createdFile.StorageObject.Should().BeSameAs(existingStorageObject);
        context.StorageService.Verify(service => service.UploadAsync(It.IsAny<KnowledgeStorageUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        context.KnowledgeRepository.Verify(repository => repository.AddStorageObject(It.IsAny<KnowledgeStorageObject>()), Times.Never);
        context.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveFileAsync_ShouldMoveFileToTargetFolder_WhenTargetIsValid()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var targetFolderId = Guid.NewGuid();
        var file = new AgentKnowledgeFile
        {
            Id = fileId,
            AgentId = agentId,
            FolderId = null,
            Name = "Handbook.pdf",
            NormalizedName = "HANDBOOK.PDF",
            CreatedByUserId = userId,
            Status = AgentKnowledgeFileStatus.Active,
            StorageObject = new KnowledgeStorageObject
            {
                Id = Guid.NewGuid(),
                ChecksumSha256 = "checksum-1",
                SizeBytes = 128
            }
        };
        var context = CreateContext(agentId, userId);

        context.KnowledgeRepository
            .Setup(repository => repository.GetFileAsync(agentId, fileId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        context.KnowledgeRepository
            .Setup(repository => repository.GetFolderAsync(agentId, targetFolderId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentKnowledgeFolder { Id = targetFolderId, AgentId = agentId, Name = "Target" });
        context.KnowledgeRepository
            .Setup(repository => repository.FileNameExistsAsync(agentId, targetFolderId, file.NormalizedName, file.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        context.KnowledgeRepository
            .Setup(repository => repository.ExactFileDuplicateExistsAsync(agentId, targetFolderId, "checksum-1", 128, file.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await context.Service.MoveFileAsync(
            Guid.Empty,
            agentId,
            fileId,
            userId,
            "10.0.0.5",
            new MoveKnowledgeItemCommand(targetFolderId),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        file.FolderId.Should().Be(targetFolderId);
        file.ModifiedByUserId.Should().Be(userId);
        file.ModifiedAt.Should().NotBeNull();
        context.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        context.CacheVersionService.Verify(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        context.AuditLogService.Verify(
            service => service.RecordAsync(
                "knowledge.file.move",
                "Uploader Name",
                userId,
                null,
                "10.0.0.5",
                It.Is<string>(value => value.Contains("Handbook.pdf", StringComparison.Ordinal)),
                "AgentKnowledgeFile",
                fileId.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MoveFileAsync_ShouldReject_WhenIdenticalFileExistsInTargetFolder()
    {
        var agentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var targetFolderId = Guid.NewGuid();
        var file = new AgentKnowledgeFile
        {
            Id = fileId,
            AgentId = agentId,
            FolderId = null,
            Name = "Handbook.pdf",
            NormalizedName = "HANDBOOK.PDF",
            CreatedByUserId = userId,
            Status = AgentKnowledgeFileStatus.Active,
            StorageObject = new KnowledgeStorageObject
            {
                Id = Guid.NewGuid(),
                ChecksumSha256 = "checksum-dup",
                SizeBytes = 256
            }
        };
        var context = CreateContext(agentId, userId);

        context.KnowledgeRepository
            .Setup(repository => repository.GetFileAsync(agentId, fileId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        context.KnowledgeRepository
            .Setup(repository => repository.GetFolderAsync(agentId, targetFolderId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentKnowledgeFolder { Id = targetFolderId, AgentId = agentId, Name = "Target" });
        context.KnowledgeRepository
            .Setup(repository => repository.FileNameExistsAsync(agentId, targetFolderId, file.NormalizedName, file.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        context.KnowledgeRepository
            .Setup(repository => repository.ExactFileDuplicateExistsAsync(agentId, targetFolderId, "checksum-dup", 256, file.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await context.Service.MoveFileAsync(
            Guid.Empty,
            agentId,
            fileId,
            userId,
            null,
            new MoveKnowledgeItemCommand(targetFolderId),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(KnowledgeErrorCodes.ValidationError);
        file.FolderId.Should().BeNull();
        context.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.AuditLogService.Verify(
            service => service.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static KnowledgeFileServiceTestContext CreateContext(Guid agentId, Guid userId)
    {
        var agentRepository = new Mock<IAgentRepository>();
        var tenantRepository = new Mock<ITenantRepository>();
        var knowledgeRepository = new Mock<IAgentKnowledgeRepository>();
        var storageService = new Mock<IKnowledgeStorageService>();
        var previewConverter = new Mock<IKnowledgePreviewConverter>();
        var authUserRepository = new Mock<IAuthUserRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var cacheVersionService = new Mock<ICacheVersionService>();
        var logger = new Mock<ILogger<KnowledgeFileService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        agentRepository
            .Setup(repository => repository.GetInternalAgentByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Agent { Id = agentId, Scope = AgentScope.Internal, Status = AgentStatus.Active });
        storageService.SetupGet(service => service.MaxUploadBytes).Returns(1024 * 1024);
        storageService.SetupGet(service => service.BucketName).Returns("knowledge");
        authUserRepository
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, FullName = "Uploader Name", Email = "uploader@example.com" });
        cacheVersionService
            .Setup(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new KnowledgeFileServiceTestContext(
            new KnowledgeFileService(
                agentRepository.Object,
                tenantRepository.Object,
                knowledgeRepository.Object,
                storageService.Object,
                previewConverter.Object,
                authUserRepository.Object,
                auditLogService.Object,
                cacheVersionService.Object,
                logger.Object,
                unitOfWork.Object),
            agentRepository,
            tenantRepository,
            knowledgeRepository,
            storageService,
            authUserRepository,
            auditLogService,
            cacheVersionService,
            unitOfWork);
    }

    private static KnowledgeUploadContent CreateUpload(string fileName, string contentType, string content, Guid? folderId = null)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new KnowledgeUploadContent(new MemoryStream(bytes), fileName, contentType, bytes.Length, folderId);
    }

    private static string CalculateChecksum(string content)
    {
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }

    private sealed record KnowledgeFileServiceTestContext(
        KnowledgeFileService Service,
        Mock<IAgentRepository> AgentRepository,
        Mock<ITenantRepository> TenantRepository,
        Mock<IAgentKnowledgeRepository> KnowledgeRepository,
        Mock<IKnowledgeStorageService> StorageService,
        Mock<IAuthUserRepository> AuthUserRepository,
        Mock<IAuditLogService> AuditLogService,
        Mock<ICacheVersionService> CacheVersionService,
        Mock<IUnitOfWork> UnitOfWork);
}
