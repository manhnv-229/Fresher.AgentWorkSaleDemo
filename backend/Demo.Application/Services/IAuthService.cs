using Demo.Application.DTOs.Auth;
using Demo.Application.UseCases.Common;

namespace Demo.Application.Services;

public interface IAuthService
{
    Task<ServiceResult<TokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<TokenResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
