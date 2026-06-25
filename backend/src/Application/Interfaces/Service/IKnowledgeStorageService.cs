using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Service;

public interface IKnowledgeStorageService
{
    string BucketName { get; }

    long MaxUploadBytes { get; }

    Task<KnowledgeStorageUploadResult> UploadAsync(KnowledgeStorageUploadRequest request, CancellationToken cancellationToken);

    Task<KnowledgeStorageDownloadResult> DownloadAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);
}
