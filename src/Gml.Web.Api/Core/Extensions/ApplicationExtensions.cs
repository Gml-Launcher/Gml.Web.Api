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
        var configuration = ConfigurationSection(
            builder,
            out var projectName,
            out var projectDescription,
            out var policyName);

        // Options
        builder.Services
            .AddOptions<ServerSettings>()
            .Bind(configuration);

        builder.Services
            // Register endpoints info
            .AddEndpointsApiExplorer()
            .RegisterSwagger(projectName, projectDescription)

            // Add system components
            .AddDbContext<DatabaseContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("SQLite")))
            .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies().AsEnumerable())
            .RegisterRepositories()
            .RegisterValidators()
            .RegisterCors(policyName);
        return builder;
    }

    private static IConfigurationSection ConfigurationSection(WebApplicationBuilder builder, out string projectName,
        out string? projectDescription, out string policyName)
    {
        var configuration = builder.Configuration;
        var serverConfiguration = configuration.GetSection(nameof(ServerSettings));

        projectName = serverConfiguration.GetValue<string>("ProjectName")
                      ?? throw new Exception("Project name not found");

        projectDescription = serverConfiguration.GetValue<string>("ProjectDescription");

        policyName = serverConfiguration.GetValue<string>("PolicyName")
                     ?? throw new Exception("Policy name not found");
        
        return serverConfiguration;
    }
}