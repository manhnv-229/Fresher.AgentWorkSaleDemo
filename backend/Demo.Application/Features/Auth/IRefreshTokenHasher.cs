namespace Demo.Application.Features.Auth;

public interface IRefreshTokenHasher
{
    RefreshTokenSecret GenerateToken();
    string HashToken(string token);
}
