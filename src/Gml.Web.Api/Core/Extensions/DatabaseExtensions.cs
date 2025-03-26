using System.Reactive.Subjects;
using Gml.Core.Launcher;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Settings;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
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
        var settings = services.GetRequiredService<ServerSettings>();

        app.UseCors(settings.PolicyName);

        if (context.Database.GetPendingMigrations().Any())
            context.Database.Migrate();

        EnsureCreateRecords(context, app.Services);

        return app;
    }

    private static void EnsureCreateRecords(DatabaseContext context, IServiceProvider services)
    {
        var settingsSubject = services.GetRequiredService<ISubject<Settings>>();
        var gmlManager = services.GetRequiredService<IGmlManager>();
        var applicationContext = services.GetRequiredService<ApplicationContext>();

        var dataBaseSettings = context.Settings.OrderBy(c => c.Id).LastOrDefault();

        if (dataBaseSettings is null)
        {
            dataBaseSettings = context.Settings.Add(new Settings
            {
                RegistrationIsEnabled = true,
                CurseForgeKey = string.Empty,
                TextureProtocol = TextureProtocol.Https
            }).Entity;

            context.SaveChanges();
        }

        settingsSubject.OnNext(dataBaseSettings);

        RestoreStorage(gmlManager, dataBaseSettings);

        gmlManager.LauncherInfo.UpdateSettings(
            dataBaseSettings.StorageType,
            dataBaseSettings.StorageHost,
            dataBaseSettings.StorageLogin,
            dataBaseSettings.StoragePassword,
            dataBaseSettings.TextureProtocol,
            dataBaseSettings.CurseForgeKey,
            dataBaseSettings.VkKey
        );
    }

    private static void RestoreStorage(IGmlManager gmlManager, Settings settings)
    {
        gmlManager.LauncherInfo.StorageSettings.StorageType = settings.StorageType;
        gmlManager.LauncherInfo.StorageSettings.StorageHost = settings.StorageHost;
        gmlManager.LauncherInfo.StorageSettings.StorageLogin = settings.StorageLogin;
        gmlManager.LauncherInfo.StorageSettings.StoragePassword = settings.StoragePassword;
        gmlManager.LauncherInfo.StorageSettings.TextureProtocol = settings.TextureProtocol;
        gmlManager.LauncherInfo.AccessTokens[AccessTokenTokens.CurseForgeKey] = settings.CurseForgeKey;
        gmlManager.LauncherInfo.AccessTokens[AccessTokenTokens.VkKey] = settings.VkKey;
    }
}
