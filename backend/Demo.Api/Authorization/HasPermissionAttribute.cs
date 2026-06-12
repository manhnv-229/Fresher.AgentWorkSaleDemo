using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
        Policy = $"{PolicyPrefix}{permissionCode}";
    }

    public string PermissionCode { get; }
}
