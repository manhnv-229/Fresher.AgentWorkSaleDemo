using Demo.Application.DTOs;

namespace Demo.Domain.Interfaces.Service;

public interface IRefreshTokenHasher
{
    RefreshTokenSecret GenerateToken();
    string HashToken(string rawToken);
}
