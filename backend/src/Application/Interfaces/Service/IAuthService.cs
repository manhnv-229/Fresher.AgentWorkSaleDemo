using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;

namespace Demo.Domain.Interfaces.Service;

public interface IAuthService
{
    Task<ServiceResult<AuthTokenResult>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<AuthTokenResult>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
