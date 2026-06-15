using Demo.Domain.Options;

using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.Options;

public sealed class AuthOptions(IOptions<JwtOptions> options) : IAuthOptions
{
    public int RefreshTokenDays { get; } = options.Value.RefreshTokenDays;
}
