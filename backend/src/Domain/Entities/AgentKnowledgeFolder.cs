namespace Demo.Domain.Entities;

public sealed class AgentKnowledgeFolder
{
    public Guid Id
    {
        get; set;
    }
    public Guid AgentId
    {
        get; set;
    }
    public Guid? ParentFolderId
    {
        get; set;
    }
    public Guid CreatedByUserId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
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
    public User? CreatedByUser
    {
        get; set;
    }
    public AgentKnowledgeFolder? ParentFolder
    {
        get; set;
    }
    public ICollection<AgentKnowledgeFolder> ChildFolders { get; set; } = new List<AgentKnowledgeFolder>();
    public ICollection<AgentKnowledgeFile> Files { get; set; } = new List<AgentKnowledgeFile>();
}
