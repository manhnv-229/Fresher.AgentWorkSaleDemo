using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Service;
using Demo.Domain.Entities;
using Demo.Infrastructure.Options;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Demo.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public JwtTokenResult CreateAccessToken(User user, Guid sessionId, IEnumerable<string> permissionCodes)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var tokenId = Guid.NewGuid().ToString("N");
        var distinctPermissionCodes = permissionCodes
            .Where(permissionCode => !string.IsNullOrWhiteSpace(permissionCode))
            .Select(permissionCode => permissionCode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName ?? user.Email),
            new(JwtRegisteredClaimNames.Jti, tokenId)
        };

        if (distinctPermissionCodes.Length > 0)
        {
            // Ghi cả claim lặp và scope chuỗi để frontend đọc được ổn định, đồng thời vẫn tương thích với các JWT inspector phổ biến.
            claims.AddRange(distinctPermissionCodes.Select(permissionCode => new Claim("permission", permissionCode)));
            claims.Add(new Claim("scope", string.Join(' ', distinctPermissionCodes)));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, tokenId);
    }
}
