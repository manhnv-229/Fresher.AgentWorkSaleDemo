using Microsoft.AspNetCore.Authorization;

namespace Demo.Api.Authorization;

/// <summary>
/// Attribute yêu cầu endpoint được cấp một mã quyền cụ thể trước khi thực thi.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    /// <summary>
    /// Khởi tạo yêu cầu authorization cho một permission code.
    /// <param name="permissionCode">Mã quyền endpoint yêu cầu.</param>
    /// </summary>
    public HasPermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
        Policy = $"{PolicyPrefix}{permissionCode}";
    }

    public string PermissionCode
    {
        get;
    }
}
