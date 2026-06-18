using Demo.Domain.Enums;

namespace Demo.Domain.Interfaces.Repository;

public sealed record AgentQueryFilters(AgentStatus? Status, string? Search, int Page = 1, int PageSize = 20);
