using Demo.Application.DTOs.Auth;

namespace Demo.Application.Services;

public interface IRefreshTokenHasher
{
    RefreshTokenSecret GenerateToken();
    string HashToken(string token);
}
