using Demo.Domain.Options;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using Demo.Application.Services;
using Demo.Application.Mapping;
using Demo.Infrastructure.Persistence;
using Demo.Infrastructure.Repositories;
using Demo.Infrastructure.Queries;
using Demo.Infrastructure.Services;
using Demo.Infrastructure.Options;

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtOptions));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<DemoDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        services.AddSingleton<IDbConnectionFactory>(_ => new MySqlDbConnectionFactory(connectionString));
        services.AddAutoMapper(typeof(BackendDataAccessProfile).Assembly);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAuthUserRepository, AuthUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddSingleton<IAuthOptions, AuthOptions>();
        services.AddScoped<IAgentCatalogService, AgentCatalogService>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<ITenantRepository, AgentTenantRepository>();
        services.AddScoped<ITenantCatalogService, TenantCatalogService>();
        services.AddScoped<ITenantCatalogRepository, TenantCatalogRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddScoped<IAuthSessionValidator, AuthSessionValidator>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAgentQueryRepository, AgentQueryRepository>();
        services.AddScoped<ITenantCatalogQueryRepository, TenantCatalogQueryRepository>();
        services.AddScoped<IUserQueryRepository, UserQueryRepository>();
        services.AddScoped<IAuditLogQueryRepository, AuditLogQueryRepository>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
