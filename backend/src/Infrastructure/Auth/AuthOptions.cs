using Demo.Application.Features.Auth;

using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.Auth;

public sealed class AuthOptions(IOptions<JwtOptions> options) : IAuthOptions
{
    public int RefreshTokenDays { get; } = options.Value.RefreshTokenDays;
}
