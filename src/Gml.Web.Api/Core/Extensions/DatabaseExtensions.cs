using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Extensions;

public static class DatabaseExtensions
{

    public static WebApplication InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<DatabaseContext>();
        var settings = services.GetRequiredService<IOptions<ServerSettings>>();

        app.UseCors(settings.Value.PolicyName);

        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }

        return app;

    }

}