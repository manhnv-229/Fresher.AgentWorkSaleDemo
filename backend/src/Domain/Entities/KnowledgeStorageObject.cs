using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class KnowledgeStorageObject
{
    public Guid Id
    {
        get; set;
    }
    public string StorageBucket { get; set; } = string.Empty;
    public string StorageObjectKey { get; set; } = string.Empty;
    public string? StorageEtag
    {
        get; set;
    }
    public string? StorageVersionId
    {
        get; set;
    }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public KnowledgeStorageObjectStatus Status { get; set; } = KnowledgeStorageObjectStatus.PendingUpload;
    public Guid CreatedByUserId
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? DeletedAt
    {
        get; set;
    }

    public User? CreatedByUser
    {
        get; set;
    }

    public ICollection<AgentKnowledgeFile> Files { get; set; } = new List<AgentKnowledgeFile>();
}
