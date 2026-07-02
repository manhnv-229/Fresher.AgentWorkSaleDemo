using Demo.Application.DTOs;
using Demo.Domain.Entities;

namespace Demo.Domain.Interfaces.Service;

public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(User user, Guid sessionId, IEnumerable<string> permissionCodes);
}
