namespace Demo.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record RefreshTokenRequest(string? RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public sealed record AuthTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string? FullName,
    string Status);

public sealed record AdminUserSummary(
    Guid Id,
    string Email,
    string? FullName,
    string Status,
    DateTime? PasswordChangedAt,
    string? EmployeeCode,
    string? Project,
    string? JobPosition);

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAt,
    string TokenId);

public sealed record RefreshTokenSecret(
    string RawToken,
    string TokenHash);
