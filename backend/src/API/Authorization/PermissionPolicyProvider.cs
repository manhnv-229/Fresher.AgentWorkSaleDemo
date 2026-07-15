using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Demo.Api.Authorization;

/// <summary>
/// Tạo authorization policy động từ policy name có tiền tố permission.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    /// <summary>
    /// Phân giải policy name thành policy yêu cầu đăng nhập và permission tương ứng.
    /// <param name="policyName">Tên policy do HasPermissionAttribute tạo ra.</param>
    /// <returns>Policy permission hoặc policy mặc định nếu tên không thuộc hệ thống này.</returns>
    /// </summary>
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
