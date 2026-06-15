using Demo.Domain.Enums;

namespace Demo.Application.DTOs;

public sealed record AgentListFilters(string? Status, string? Search);
