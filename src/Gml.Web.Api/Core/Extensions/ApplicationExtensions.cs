using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text;
using Gml.Core.Launcher;
using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Core.Middlewares;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Launcher;
using Gml.Web.Api.Domains.Settings;
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
        app.UseAuthentication();
        app.UseAuthorization();

        app.RegisterEndpoints()
            .UseCors(_policyName)
            .UseMiddleware<BadRequestExceptionMiddleware>()
            .UseMiddleware<PluginMiddleware>()
            .UseSwagger()
            .UseSwaggerUI();

        app.InitializeDatabase();

        return app;
    }

    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        var serverSettings = GetServerSettings(builder,
            out var projectName,
            out var projectDescription,
            out var policyName,
            out var projectPath,
            out var secretKey
        );

        _policyName = policyName;

        builder.RegisterOptions(serverSettings);
        builder.RegisterEndpointsInfo(projectName, projectDescription);
        builder.RegisterSystemComponents(policyName, projectName, projectPath, secretKey);

        return builder;
    }

    private static IConfigurationSection GetServerSettings(
        WebApplicationBuilder builder,
        out string projectName,
        out string? projectDescription,
        out string policyName,
        out string projectPath,
        out string secretKey
    )
    {
        var serverConfiguration = builder.Configuration.GetSection(nameof(ServerSettings));

        projectName = serverConfiguration.GetValue<string>("ProjectName") ??
                      throw new Exception("Project name not found");
        projectDescription = serverConfiguration.GetValue<string>("ProjectDescription");
        policyName = serverConfiguration.GetValue<string>("PolicyName") ?? throw new Exception("Policy name not found");
        projectPath = serverConfiguration.GetValue<string>("ProjectPath") ?? string.Empty;
        secretKey = serverConfiguration.GetValue<string>("SecretKey") ?? string.Empty;

        return serverConfiguration;
    }

    public static WebApplicationBuilder RegisterOptions(this WebApplicationBuilder builder,
        IConfigurationSection serverSettings)
    {
        builder.Services
            .AddOptions<ServerSettings>()
            .Bind(serverSettings);

        return builder;
    }

    private static WebApplicationBuilder RegisterEndpointsInfo(this WebApplicationBuilder builder, string projectName,
        string? projectDescription)
    {
        builder.Services
            .AddEndpointsApiExplorer()
            .RegisterSwagger(projectName, projectDescription);

        return builder;
    }

    private static WebApplicationBuilder RegisterSystemComponents(this WebApplicationBuilder builder,
        string policyName,
        string projectName,
        string projectPath,
        string secretKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

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
            .AddNamedHttpClients()
            .AddDbContext<DatabaseContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("SQLite")))
            .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies().AsEnumerable())
            .AddSingleton<IGmlManager>(_ =>
            {
                var manager = new GmlManager(new GmlSettings(projectName, projectPath));

                manager.RestoreSettings<LauncherVersion>();

                return manager;
            })
            .AddSingleton<IAuthServiceFactory, AuthServiceFactory>()
            .AddSingleton<ISubject<Settings>, Subject<Settings>>()
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
            .AddTransient<AzuriomAuthService>()
            .AddTransient<AnyAuthService>()
            .RegisterRepositories()
            .RegisterValidators()
            .RegisterCors(policyName)
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
}
