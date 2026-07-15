using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Authorization;

/// <summary>
/// Mô tả permission code cần được authorization handler kiểm tra.
/// </summary>
public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;
