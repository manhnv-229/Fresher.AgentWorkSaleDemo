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
    public Guid CreatedByUserId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Extension
    {
        get; set;
    }
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes
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
    public User? CreatedByUser
    {
        get; set;
    }
}
