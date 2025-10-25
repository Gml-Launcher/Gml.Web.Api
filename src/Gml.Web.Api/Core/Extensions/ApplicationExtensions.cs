using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text;
using Gml.Core.Launcher;
using Gml.Domains.Settings;
using Gml.Web.Api.Core.Authentication;
using Gml.Web.Api.Core.Hubs;
using Gml.Web.Api.Core.Hubs.Controllers;
using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Core.MappingProfiles;
using Gml.Web.Api.Core.Middlewares;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;

namespace Gml.Web.Api.Core.Extensions;

public static class ApplicationExtensions
{
    private static string _policyName = string.Empty;

    public static WebApplication RegisterServices(this WebApplication app)
    {
        var swaggerEnabled = bool.TryParse(GetEnvironmentVariable("SWAGGER_ENABLED"), out var isEnabled) && isEnabled;

        app.UseAuthentication();
        app.UseAuthorization();
        // app.UseRateLimiter();

        app.RegisterEndpoints()
            .UseCors(_policyName)
            .UseMiddleware<BadRequestExceptionMiddleware>()
            .UseMiddleware<PluginRouterMiddleware>();

        if (swaggerEnabled)
        {
            app.UseSwagger().UseSwaggerUI();
        }

        app.MapHealthChecks("/health");

        app.InitializeDatabase();

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider.GetRequiredService<PluginsService>();
        services.RestorePlugins();

        return app;
    }

    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        var serverSettings = GetServerSettings();

        _policyName = serverSettings.PolicyName;

        builder.RegisterEndpointsInfo(serverSettings.ProjectName, serverSettings.ProjectDescription);
        builder.RegisterSystemComponents(serverSettings);
        builder.Services.ConfigureOptions<ProfileHub.ConfigureJsonOptions>();

        return builder;
    }

    private static ServerSettings GetServerSettings()
    {
        var projectName = GetEnvironmentVariable("PROJECT_NAME");
        var marketEndpoint = GetEnvironmentVariable("MARKET_ENDPOINT");
        var projectDescription = GetEnvironmentVariable("PROJECT_DESCRIPTION");
        var policyName = GetEnvironmentVariable("PROJECT_POLICYNAME");
        var projectPath = GetEnvironmentVariable("PROJECT_PATH");
        var securityKey = GetEnvironmentVariable("SECURITY_KEY");
        var swaggerEnabled = bool.TryParse(GetEnvironmentVariable("SWAGGER_ENABLED"), out var isEnabled) && isEnabled;

        var textureEndpoint = GetEnvironmentVariable("SERVICE_TEXTURE_ENDPOINT");

        var jwtIssuer = GetEnvironmentVariable("JWT_ISSUER");
        var jwtAudience = GetEnvironmentVariable("JWT_AUDIENCE");
        var accessMinutesStr = GetEnvironmentVariable("JWT_ACCESS_MINUTES");
        var refreshDaysStr = GetEnvironmentVariable("JWT_REFRESH_DAYS");
        int.TryParse(accessMinutesStr, out var accessMinutes);
        int.TryParse(refreshDaysStr, out var refreshDays);

        return new ServerSettings
        {
            ProjectDescription = projectDescription,
            ProjectName = projectName,
            PolicyName = policyName,
            MarketEndpoint = marketEndpoint,
            IsEnabledApiDocs = swaggerEnabled,
            ProjectVersion = "1.1.0",
            SecurityKey = securityKey,
            ProjectPath = projectPath,
            TextureEndpoint = textureEndpoint,
            JwtIssuer = string.IsNullOrWhiteSpace(jwtIssuer) ? "gml-api" : jwtIssuer,
            JwtAudience = string.IsNullOrWhiteSpace(jwtAudience) ? "gml-clients" : jwtAudience,
            AccessTokenMinutes = accessMinutes > 0 ? accessMinutes : 15,
            RefreshTokenDays = refreshDays > 0 ? refreshDays : 30
        };
    }

    private static WebApplicationBuilder RegisterEndpointsInfo(this WebApplicationBuilder builder,
        string projectName,
        string? projectDescription)
    {
        builder.Services
            .AddEndpointsApiExplorer()
            .RegisterSwagger(projectName, projectDescription);

        return builder;
    }

    private static WebApplicationBuilder RegisterSystemComponents(
        this WebApplicationBuilder builder,
        ServerSettings settings)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = settings.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key
        };

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 209715200; // 200Мб
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 209715200; // 200 MB
        });

        builder.Services
            .AddHttpClient()
            .AddNamedHttpClients(settings.MarketEndpoint)
            .AddMemoryCache()
            .AddDbContext<DatabaseContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("SQLite")))
            .AddAutoMapper(map =>
            {
                map.AddProfile<AuthServerMapper>();
                map.AddProfile<DiscordRpcMapper>();
                map.AddProfile<LauncherMapper>();
                map.AddProfile<ModsMapper>();
                map.AddProfile<NewsMapper>();
                map.AddProfile<PlayerMapper>();
                map.AddProfile<ProfileMapper>();
                map.AddProfile<ServerMapper>();
                map.AddProfile<SettingsMapper>();
                map.AddProfile<SystemIOMapper>();
                map.AddProfile<UserMapper>();
                map.AddProfile<RbacMapper>();
            })
            .ConfigureGmlManager(
                settings.ProjectName,
                settings.SecurityKey,
                settings.ProjectPath,
                settings.TextureEndpoint
            )
            .ConfigureRateLimit()
            .AddHealthChecks().Services
            .AddSingleton(settings)
            .AddSingleton<IAuthServiceFactory, AuthServiceFactory>()
            .AddSingleton<PluginsService>()
            .AddSingleton<PluginAssemblyManager>()
            .AddSingleton<HubEvents>()
            .AddSingleton<ISubject<Settings>, Subject<Settings>>()
            .AddSingleton<PlayersController>()
            .AddSingleton<NotificationController>()
            .AddScoped<ISystemService, SystemService>()
            .AddScoped<ISkinServiceManager, SkinServiceManager>()
            .AddSingleton<IAuthService, AuthService>()
            .AddSingleton<IGitHubService, GitHubService>()
            .AddSingleton<ApplicationContext>()
            .AddSingleton<IAccessTokenService, AccessTokenService>()
            .AddTransient<UndefinedAuthService>()
            .AddTransient<DataLifeEngineAuthService>()
            .AddTransient<UnicoreCMSAuthService>()
            .AddTransient<EasyCabinetAuthService>()
            .AddTransient<CustomEndpointAuthService>()
            .AddTransient<NamelessMCAuthService>()
            .AddTransient<WebMCRAuthService>()
            .AddTransient<AzuriomAuthService>()
            .AddTransient<WordPressAuthService>()
            .AddTransient<AnyAuthService>()
            .RegisterRepositories()
            .RegisterValidators()
            .RegisterCors(settings.PolicyName)
            .AddSignalR();

        builder.Services.AddAuthorization();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "MultiScheme";
            options.DefaultScheme = "MultiScheme";
            options.DefaultChallengeScheme = "MultiScheme";
        })
        .AddPolicyScheme("MultiScheme", "JWT or External App", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    if (token.StartsWith("eyJ"))
                        return JwtBearerDefaults.AuthenticationScheme;
                    else
                        return "ExternalApplication";
                }
                return JwtBearerDefaults.AuthenticationScheme;
            };
        })
        .AddJwtBearer(jwt =>
        {
            jwt.SaveToken = true;
            jwt.TokenValidationParameters = tokenValidationParameters;
            jwt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        context.Token = accessToken;

                    return Task.CompletedTask;
                }
            };
        })
        .AddScheme<AuthenticationSchemeOptions, ExternalApplicationAuthenticationHandler>(
            "ExternalApplication", options => { });

        // RBAC dynamic permission policies and handler
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, Gml.Web.Api.Core.Authorization.DynamicPermissionPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationHandler, Gml.Web.Api.Core.Authorization.PermissionAuthorizationHandler>();

        return builder;
    }

    private static string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? string.Empty;
    }
}
