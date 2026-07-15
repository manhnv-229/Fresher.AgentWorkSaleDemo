using Demo.Domain.Interfaces.Service;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Băm và xác minh mật khẩu bằng thuật toán BCrypt cho luồng xác thực người dùng.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
