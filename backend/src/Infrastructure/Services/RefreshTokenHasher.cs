using System.Security.Cryptography;
using System.Text;

using Demo.Application.DTOs;
using Demo.Domain.Interfaces.Service;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Tạo hash SHA-256 cho refresh token trước khi lưu hoặc truy vấn trong cơ sở dữ liệu.
/// </summary>
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public RefreshTokenSecret GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(bytes);
        return new RefreshTokenSecret(rawToken, HashToken(rawToken));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
