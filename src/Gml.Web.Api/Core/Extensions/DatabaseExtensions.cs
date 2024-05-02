using System.Reactive.Subjects;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Settings;
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

        if (context.Database.GetPendingMigrations().Any()) context.Database.Migrate();

        EnsureCreateRecords(context, app.Services);

        return app;
    }

    private static void EnsureCreateRecords(DatabaseContext context, IServiceProvider services)
    {
        var settingsSubject = services.GetRequiredService<ISubject<Settings>>();
        var applicationContext = services.GetRequiredService<ApplicationContext>();

        var dataBaseSettings = context.Settings.OrderBy(c => c.Id).LastOrDefault();

        if (dataBaseSettings is null)
        {
            dataBaseSettings = context.Settings.Add(new Settings
            {
                RegistrationIsEnabled = true
            }).Entity;

            context.SaveChanges();
        }

        settingsSubject.OnNext(dataBaseSettings);
    }
}
