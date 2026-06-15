using Demo.Domain.Enums;

namespace Demo.Domain.Interfaces.Repository;

public sealed record AgentQueryFilters(AgentStatus? Status, string? Search);
