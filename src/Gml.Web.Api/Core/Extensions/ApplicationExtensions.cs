using Gml.Web.Api.Core.Middlewares;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Extensions;

public static class ApplicationExtensions
{
    public static WebApplication RegisterServices(this WebApplication app)
    {
        app.RegisterEndpoints()
            .InitializeDatabase()
            .UseMiddleware<BadRequestExceptionMiddleware>()
            .UseSwagger()
            .UseSwaggerUI();

        return app;
    }

    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        var serverSettings = GetServerSettings(builder,
            out var projectName,
            out var projectDescription,
            out var policyName);

        builder.RegisterOptions(serverSettings);
        builder.RegisterEndpointsInfo(projectName, projectDescription);
        builder.RegisterSystemComponents(policyName);

        return builder;
    }

    private static IConfigurationSection GetServerSettings(WebApplicationBuilder builder, out string projectName,
        out string? projectDescription, out string policyName)
    {
        var serverConfiguration = builder.Configuration.GetSection(nameof(ServerSettings));

        projectName = serverConfiguration.GetValue<string>("ProjectName") ??
                      throw new Exception("Project name not found");
        projectDescription = serverConfiguration.GetValue<string>("ProjectDescription");
        policyName = serverConfiguration.GetValue<string>("PolicyName") ?? throw new Exception("Policy name not found");

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

    private static WebApplicationBuilder RegisterSystemComponents(this WebApplicationBuilder builder, string policyName)
    {
        builder.Services
            .AddDbContext<DatabaseContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("SQLite")))
            .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies().AsEnumerable())
            .RegisterRepositories()
            .RegisterValidators()
            .RegisterCors(policyName);

        return builder;
    }
}