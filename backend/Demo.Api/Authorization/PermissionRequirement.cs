using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Authorization;

public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;
