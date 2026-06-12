using Demo.Application.DTOs.Auth;
using Demo.Domain.Entities;

namespace Demo.Application.Services;

public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(User user, Guid sessionId);
}
