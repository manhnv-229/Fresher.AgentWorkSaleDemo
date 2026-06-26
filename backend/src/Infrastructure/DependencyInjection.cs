using Demo.Domain.Options;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using Demo.Application.Services;
using Demo.Application.Services.Knowledge;
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
using System.Net;
using System.Net.Http.Headers;

namespace Demo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DotEnvLoader.LoadNearest();
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
        services.AddScoped<IAgentKnowledgeService, AgentKnowledgeService>();
        services.AddScoped<IKnowledgeExplorerService, KnowledgeExplorerService>();
        services.AddScoped<IKnowledgeFolderService, KnowledgeFolderService>();
        services.AddScoped<IKnowledgeFileService, KnowledgeFileService>();
        services.AddScoped<IAgentQueryRepository, AgentQueryRepository>();
        services.AddScoped<ITenantCatalogQueryRepository, TenantCatalogQueryRepository>();
        services.AddScoped<IUserQueryRepository, UserQueryRepository>();
        services.AddScoped<IAuditLogQueryRepository, AuditLogQueryRepository>();
        services.AddScoped<IAgentKnowledgeRepository, AgentKnowledgeRepository>();
        var knowledgeStorageOptions = CreateKnowledgeStorageOptions();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(knowledgeStorageOptions));
        services.AddSingleton<HttpClient>(_ => CreateKnowledgeStorageHttpClient(knowledgeStorageOptions));
        services.AddScoped<IKnowledgeStorageService, MinioKnowledgeStorageService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static KnowledgeStorageOptions CreateKnowledgeStorageOptions()
    {
        return new KnowledgeStorageOptions
        {
            Endpoint = GetString("S3_ENDPOINT", "localhost"),
            Port = GetInt("S3_PORT", 9000),
            AccessKey = GetString("S3_ACCESS_KEY", "minioadmin"),
            SecretKey = GetString("S3_SECRET_KEY", "minioadmin123"),
            Bucket = GetString("S3_BUCKET", "knowledge"),
            Region = GetString("S3_REGION", "us-east-1"),
            UseSsl = GetBool("S3_USE_SSL", false),
            ForcePathStyle = GetBool("S3_FORCE_PATH_STYLE", true),
            MaxUploadBytes = GetLong("S3_MAX_UPLOAD_BYTES", 25 * 1024 * 1024),
            RequestTimeoutSeconds = GetInt("S3_REQUEST_TIMEOUT_SECONDS", 15),
            ConnectTimeoutSeconds = GetInt("S3_CONNECT_TIMEOUT_SECONDS", 5)
        };
    }

    private static HttpClient CreateKnowledgeStorageHttpClient(KnowledgeStorageOptions options)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds),
            Expect100ContinueTimeout = TimeSpan.Zero,
            AutomaticDecompression = DecompressionMethods.None
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds)
        };
        client.DefaultRequestHeaders.ExpectContinue = false;
        client.DefaultRequestHeaders.ConnectionClose = false;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        return client;
    }

    private static string GetString(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static int GetInt(string key, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;
    }

    private static long GetLong(string key, long fallback)
    {
        return long.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;
    }

    private static bool GetBool(string key, bool fallback)
    {
        return bool.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;
    }
}

internal static class DotEnvLoader
{
    public static void LoadNearest()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                var envPath = Path.Combine(directory.FullName, ".env");
                if (File.Exists(envPath))
                {
                    LoadFile(envPath);
                    return;
                }

                directory = directory.Parent;
            }
        }
    }

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            var key = line[..equalsIndex].Trim();
            var value = line[(equalsIndex + 1)..].Trim().Trim('"');
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
