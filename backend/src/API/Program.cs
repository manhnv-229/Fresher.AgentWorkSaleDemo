using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Demo.Api.Authorization;
using Demo.Domain.Interfaces.Service;
using Demo.Infrastructure;
using Demo.Infrastructure.Persistence;
using Demo.Infrastructure.Options;
using Demo.Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return uri.Host is "localhost" or "127.0.0.1" or "[::1]";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Description = "Paste JWT access token here. Do not include the 'Bearer ' prefix."
        };

        var bearerRequirement = new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }
            ] = []
        };

        foreach (var (path, pathItem) in document.Paths)
        {
            if (path is "/api/auth/login" or "/api/auth/refresh-token" or "/api/auth/logout")
            {
                continue;
            }

            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(bearerRequirement);
            }
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantContextResolver>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue("userId") ??
                    context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var sessionIdValue = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sid) ??
                    context.Principal?.FindFirstValue("sid");

                if (!Guid.TryParse(userIdValue, out var userId) ||
                    !Guid.TryParse(sessionIdValue, out var sessionId))
                {
                    context.Fail("Access token is missing a valid session.");
                    return;
                }

                try
                {
                    var validator = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionValidator>();
                    if (!await validator.IsSessionActiveAsync(userId, sessionId, context.HttpContext.RequestAborted))
                    {
                        context.Fail("Session is no longer active.");
                    }
                }
                catch (Exception exception)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearerEvents");

                    logger.LogError(exception, "Session validation failed.");
                    context.Fail("Session validation failed.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    if (app.Configuration.GetValue<bool>("Database:EnsureCreatedOnStartup") ||
        app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DemoDbContext>();

        if (app.Configuration.GetValue<bool>("Database:EnsureCreatedOnStartup"))
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        if (app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
        }
    }
}

app.UseCors("LocalFrontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Ok(new
{
    name = "Demo API",
    status = "Running",
    endpoints = new[]
    {
        "GET /health",
        "POST /api/auth/login",
        "POST /api/auth/refresh-token",
        "POST /api/auth/logout",
        "GET /api/auth/me",
        "GET /api/tenants",
        "POST /api/tenants",
        "GET /api/admin/agents/internal",
        "POST /api/admin/agents/internal",
        "GET /api/tenants/{tenantId}/agents",
        "POST /api/tenants/{tenantId}/agents",
        "GET /swagger",
        "GET /openapi/v1.json"
    }
}));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", utcNow = DateTime.UtcNow }));
app.MapGet("/swagger", () => Results.Content("""
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>Demo API Swagger</title>
      <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css" />
      <style>
        body { margin: 0; background: #f7f8fa; }
        .swagger-ui .topbar { display: none; }
      </style>
    </head>
    <body>
      <div id="swagger-ui"></div>
      <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
      <script>
        window.ui = SwaggerUIBundle({
          url: '/openapi/v1.json',
          dom_id: '#swagger-ui',
          deepLinking: true,
          persistAuthorization: true
        });
      </script>
    </body>
    </html>
    """, "text/html"));
app.MapControllers();

app.Run();
