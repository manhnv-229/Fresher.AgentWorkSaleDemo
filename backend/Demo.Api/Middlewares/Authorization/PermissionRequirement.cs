using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Middlewares.Authorization;

public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;
