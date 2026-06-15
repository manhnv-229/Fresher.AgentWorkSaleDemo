using Demo.Application.Authorization;
using Demo.Application.Features.Agents;
using Demo.Application.Features.Auth;
using Demo.Application.Features.Tenants;
using Demo.Application.UseCases.Common;
using Demo.Infrastructure.Agents;
using Demo.Infrastructure.Auth;
using Demo.Infrastructure.Persistence;
using Demo.Infrastructure.Repositories;
using Demo.Infrastructure.Security;
using Demo.Infrastructure.Tenants;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Demo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = new JwtOptions();
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        jwtOptions.Issuer = jwtSection[nameof(JwtOptions.Issuer)] ?? jwtOptions.Issuer;
        jwtOptions.Audience = jwtSection[nameof(JwtOptions.Audience)] ?? jwtOptions.Audience;
        jwtOptions.SigningKey = jwtSection[nameof(JwtOptions.SigningKey)] ?? jwtOptions.SigningKey;
        jwtOptions.AccessTokenMinutes = int.TryParse(jwtSection[nameof(JwtOptions.AccessTokenMinutes)], out var accessMinutes)
            ? accessMinutes
            : jwtOptions.AccessTokenMinutes;
        jwtOptions.RefreshTokenDays = int.TryParse(jwtSection[nameof(JwtOptions.RefreshTokenDays)], out var refreshDays)
            ? refreshDays
            : jwtOptions.RefreshTokenDays;
        services.AddSingleton(Options.Create(jwtOptions));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<DemoDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthUserRepository, AuthUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddSingleton<IAuthOptions, AuthOptions>();
        services.AddScoped<IAgentCatalogService, AgentCatalogService>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<Demo.Application.Features.Agents.ITenantRepository, Demo.Infrastructure.Agents.TenantRepository>();
        services.AddScoped<ITenantCatalogService, TenantCatalogService>();
        services.AddScoped<ITenantCatalogRepository, Demo.Infrastructure.Tenants.TenantRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddScoped<IAuthSessionValidator, AuthSessionValidator>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
