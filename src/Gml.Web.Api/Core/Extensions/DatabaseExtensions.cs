using System.Reactive.Subjects;
using Gml.Core.Launcher;
using Gml.Domains.Auth;
using Gml.Domains.Settings;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.EntityFrameworkCore;

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

        var databaseDirectory = new DirectoryInfo("database");

        if (!databaseDirectory.Exists)
        {
            databaseDirectory.Create();
        }

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
            dataBaseSettings.VkKey,
            dataBaseSettings.SentryAutoClearPeriod,
            dataBaseSettings.SentryNeedAutoClear
        );

        // Seed base RBAC: Admin role and base permissions with descriptions
        try
        {
            // Ensure Admin role exists
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (adminRole == null)
            {
                adminRole = context.Roles.Add(new Role
                {
                    Name = "Admin",
                    Description = "System administrator"
                }).Entity;
                context.SaveChanges();
            }

            // Define all base permissions with descriptions
            var basePerms = new (string Name, string Description)[]
            {
                ("launcher.manage", "Управление лаунчером, включая получение версий, скачивание и управление сборками"),
                ("launcher.view", "Просмотр и получение версий лаунчера"),
                ("launcher.create", "Загрузка и создание сборок лаунчера"),
                ("launcher.update", "Сборка лаунчера и управление процессом сборки"),
                ("launcher.delete", "Удаление сборок лаунчера или откаты"),
                ("integrations.sentry.manage",
                    "Управление интеграцией с Sentry, включая обновление DSN, получение ошибок, фильтрацию и очистку"),
                ("integrations.discord.update", "Обновление данных интеграции с DiscordRPC"),
                ("integrations.textures.update", "Обновление ссылок на сервисы текстур (скины и плащи)"),
                ("integrations.textures.view", "Просмотр ссылок на сервисы текстур (скины и плащи)"),
                ("integrations.auth.view", "Просмотр активного сервиса авторизации и списка доступных сервисов"),
                ("integrations.auth.create", "Добавление/установка сервиса авторизации"),
                ("integrations.auth.update", "Обновление информации о сервисе авторизации"),
                ("integrations.auth.delete", "Удаление/отключение активного сервиса авторизации"),
                ("integrations.news.manage",
                    "Управление слушателями новостей, включая добавление, удаление и получение списка слушателей"),
                ("integrations.news.view", "Получение списка новостей"),
                ("profiles.view", "Просмотр списка профилей и версий Minecraft"),
                ("profiles.create", "Создание игровых профилей"),
                ("profiles.update",
                    "Обновление игровых профилей, включая восстановление, компиляцию и управление whitelist"),
                ("profiles.delete", "Удаление игровых профилей"),
                ("players.manage",
                    "Управление списком игроков, включая просмотр, удаление, блокировку и разблокировку"),
                ("players.view", "Просмотр списка игроков"),
                ("players.delete", "Удаление игроков из списка"),
                ("players.ban", "Блокировка игроков"),
                ("players.pardon", "Разблокировка игроков"),
                ("servers.manage", "Управление игровыми серверами через SignalR хаб"),
                ("notifications.manage", "Управление уведомлениями через SignalR хаб")
            };

            var basePermNames = basePerms.Select(p => p.Name).ToArray();
            var existingPerms = context.Permissions.Where(p => basePermNames.Contains(p.Name)).ToList();
            var existingNames = existingPerms.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, description) in basePerms)
            {
                if (!existingNames.Contains(name))
                {
                    var newPerm = context.Permissions.Add(new Permission
                    {
                        Name = name,
                        Description = description,
                        IsSystem = true
                    }).Entity;
                }
                else
                {
                    // Ensure description is up-to-date
                    var perm = existingPerms.First(p =>
                        string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (string.IsNullOrWhiteSpace(perm.Description))
                    {
                        perm.Description = description;
                    }

                    // Mark as system permission (shadow property)
                    var entry = context.Entry(perm);
                    if (!Equals(entry.Property("IsSystem").CurrentValue, true))
                    {
                        entry.Property("IsSystem").CurrentValue = true;
                    }
                }
            }

            if (context.ChangeTracker.HasChanges())
                context.SaveChanges();

            // Ensure Admin has all these permissions
            var permsForLink = context.Permissions.Where(p => basePermNames.Contains(p.Name)).Select(p => p.Id)
                .ToList();
            foreach (var permId in permsForLink)
            {
                var linkExists =
                    context.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permId);
                if (!linkExists)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permId
                    });
                }
            }

            if (context.ChangeTracker.HasChanges())
                context.SaveChanges();
        }
        catch
        {
            // ignore seeding errors to not block application startup
        }
    }

    private static void RestoreStorage(IGmlManager gmlManager, Settings settings)
    {
        gmlManager.LauncherInfo.StorageSettings.StorageType = settings.StorageType;
        gmlManager.LauncherInfo.StorageSettings.StorageHost = settings.StorageHost;
        gmlManager.LauncherInfo.StorageSettings.StorageLogin = settings.StorageLogin;
        gmlManager.LauncherInfo.StorageSettings.StoragePassword = settings.StoragePassword;
        gmlManager.LauncherInfo.StorageSettings.TextureProtocol = settings.TextureProtocol;
        gmlManager.LauncherInfo.StorageSettings.SentryAutoClearPeriod = settings.SentryAutoClearPeriod;
        gmlManager.LauncherInfo.StorageSettings.SentryNeedAutoClear = settings.SentryNeedAutoClear;
        gmlManager.LauncherInfo.AccessTokens[AccessTokenTokens.CurseForgeKey] = settings.CurseForgeKey;
        gmlManager.LauncherInfo.AccessTokens[AccessTokenTokens.VkKey] = settings.VkKey;
    }
}
