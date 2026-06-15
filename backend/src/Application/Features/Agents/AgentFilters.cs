using Demo.Domain.Enums;

namespace Demo.Application.Features.Agents;

public sealed record AgentListFilters(string? Status, string? Search);

public sealed record AgentQueryFilters(AgentStatus? Status, string? Search);
