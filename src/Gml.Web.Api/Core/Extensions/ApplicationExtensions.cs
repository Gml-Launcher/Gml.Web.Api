using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text;
using Gml.Core.Launcher;
using Gml.Web.Api.Core.Hubs;
using Gml.Web.Api.Core.Hubs.Controllers;
using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Core.Middlewares;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Launcher;
using Gml.Web.Api.Domains.Settings;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
            TextureEndpoint = textureEndpoint
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
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
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
            .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies().AsEnumerable())
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
            .AddSingleton<AccessTokenService>()
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

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(jwt =>
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
        });

        return builder;
    }

    private static string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? string.Empty;
    }
}
