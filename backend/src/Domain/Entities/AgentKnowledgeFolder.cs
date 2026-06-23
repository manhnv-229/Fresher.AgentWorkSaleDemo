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
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
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
    public AgentKnowledgeFolder? ParentFolder
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

    public ICollection<AgentKnowledgeFolder> ChildFolders { get; set; } = new List<AgentKnowledgeFolder>();
    public ICollection<AgentKnowledgeFile> Files { get; set; } = new List<AgentKnowledgeFile>();
}
