using Demo.Domain.Enums;

namespace Demo.Domain.Entities;

public sealed class Agent
{
    public Guid Id
    {
        get; set;
    }
    public Guid? TenantId
    {
        get; set;
    }
    public Guid CreatedByUserId
    {
        get; set;
    }
    public Guid? ModifiedByUserId
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description
    {
        get; set;
    }
    public string? Icon
    {
        get; set;
    }
    public AgentScope Scope { get; set; } = AgentScope.Tenant;
    public AgentStatus Status { get; set; } = AgentStatus.Draft;
    public string Role { get; set; } = string.Empty;
    public DateTime? PublishedAt
    {
        get; set;
    }
    public DateTime? DeletedAt
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

    public Tenant? Tenant
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

    public ICollection<AgentKnowledgeFolder> KnowledgeFolders { get; set; } = new List<AgentKnowledgeFolder>();
    public ICollection<AgentKnowledgeFile> KnowledgeFiles { get; set; } = new List<AgentKnowledgeFile>();
}
