namespace Demo.Application.Errors;

public static class AuthErrorCodes
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string InactiveUser = "inactive_user";
    public const string InvalidRefreshToken = "invalid_refresh_token";
    public const string InvalidSession = "invalid_session";
    public const string UserNotFound = "user_not_found";
}
