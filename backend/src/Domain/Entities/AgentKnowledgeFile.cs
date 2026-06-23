using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class AgentKnowledgeFile
{
    public Guid Id
    {
        get; set;
    }
    public Guid AgentId
    {
        get; set;
    }
    public Guid? FolderId
    {
        get; set;
    }
    public Guid StorageObjectId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public AgentKnowledgeFileStatus Status { get; set; } = AgentKnowledgeFileStatus.Active;
    public Guid CreatedByUserId
    {
        get; set;
    }
    public Guid? ModifiedByUserId
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime? ModifiedAt
    {
        get; set;
    }
    public DateTime? DeletedAt
    {
        get; set;
    }

    public Agent? Agent
    {
        get; set;
    }
    public AgentKnowledgeFolder? Folder
    {
        get; set;
    }
    public KnowledgeStorageObject? StorageObject
    {
        get; set;
    }
    public User? CreatedByUser
    {
        get; set;
    }
    public User? ModifiedByUser
    {
        get; set;
    }
}
