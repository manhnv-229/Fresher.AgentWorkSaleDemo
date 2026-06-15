using Demo.Domain.Entities;

namespace Demo.Application.Features.Auth;

public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(User user, Guid sessionId);
}
