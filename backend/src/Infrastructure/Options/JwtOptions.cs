namespace Demo.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Demo";
    public string Audience { get; set; } = "Demo";
    public string SigningKey { get; set; } = "development-signing-key-change-me-minimum-32";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
