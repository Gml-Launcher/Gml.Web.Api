using System.Net;
using Gml.Web.Api.Core.Handlers;
using Gml.Web.Api.Core.Hubs;
using Gml.Web.Api.Domains.LauncherDto;
using Gml.Web.Api.Domains.Plugins;
using Gml.Web.Api.Domains.Servers;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.Servers;
using Gml.Web.Api.Dto.Settings;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces.Notifications;

namespace Gml.Web.Api.Core.Extensions;

public static class EndpointsExtensions
{
    public static WebApplication RegisterEndpoints(this WebApplication app)
    {
        #region Root

        app.MapGet("/", () => Results.Redirect("/swagger", true));

        #endregion

        #region Launcher

        app.MapGet("/api/v1/integrations/github/launcher/versions", GitHubIntegrationHandler.GetVersions)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Список версий лаунчера";
                return generatedOperation;
            })
            .WithName("Get launcher versions")
            .WithTags("Integration/GitHub/Launcher")
            .Produces<ResponseMessage<IEnumerable<LauncherVersionReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPost("/api/v1/integrations/github/launcher/download", GitHubIntegrationHandler.DownloadLauncher)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Загрузить лаунчер";
                return generatedOperation;
            })
            .WithName("Download launcher version")
            .WithTags("Integration/GitHub/Launcher")
            .Produces<ResponseMessage<string>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapGet("/api/v1/integrations/github/launcher/download/{version}",
                GitHubIntegrationHandler.ReturnLauncherSolution)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Скачать решение лаунчера";
                return generatedOperation;
            })
            .WithName("Download launcher solution")
            .WithTags("Integration/GitHub/Launcher")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        #endregion

        #region SignalR Hubs

        app.MapHub<ProfileHub>("/ws/profiles/restore").RequireAuthorization();
        app.MapHub<GitHubLauncherHub>("/ws/launcher/build").RequireAuthorization();
        app.MapHub<GameServerHub>("/ws/gameServer");
        app.MapHub<LauncherHub>("/ws/launcher");
        app.MapHub<NotificationHub>("/ws/notifications");

        #endregion

        #region Auth

        app.MapPost("/api/v1/users/signup", AuthHandler.CreateUser)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Регистрация нового пользователя";
                return generatedOperation;
            })
            .WithName("Create User")
            .WithTags("Users")
            .Produces<ResponseMessage<UserAuthReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/v1/users/signin", AuthHandler.AuthUser)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Авторизация пользователя";
                return generatedOperation;
            })
            .WithDescription("Авторизация")
            .WithName("Authenticate User")
            .WithTags("Users")
            .Produces<ResponseMessage<UserAuthReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        #endregion

        #region Integrations

        #region Sentry

        app.MapGet("/api/v1/integrations/sentry/dsn", SentryErrorSaveHandler.GetDsnUrl)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение DSN для лаунчера";
                return generatedOperation;
            })
            .WithDescription("Получение ссылки на DSN сервис Sentry")
            .WithName("Get dsn sentry service url")
            .WithTags("Integration/Sentry")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPut("/api/v1/integrations/sentry/dsn", SentryErrorSaveHandler.UpdateDsnUrl)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление DSN для лаунчера";
                return generatedOperation;
            })
            .WithDescription("Обновление ссылки на DSN сервис Sentry")
            .WithName("Update dsn sentry service url")
            .WithTags("Integration/Sentry")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPost("/api/{projectId}/envelope", SentryHandler.GetMessage)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение ошибок Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение ошибок Sentry")
            .WithName("Get sentry message")
            .WithTags("Integration/Sentry");

        app.MapGet("/api/v1/sentry", SentryHandler.GetBugs)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение всех багов Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение всех багов Sentry")
            .WithName("Get all bugs sentry")
            .WithTags("Integration/Sentry");

        app.MapGet("/api/v1/sentry/{exception}", SentryHandler.GetBugId)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение exception в Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение exception в Sentry")
            .WithName("Get exception on sentry")
            .WithTags("Integration/Sentry");

        app.MapGet("/api/v1/sentry/bug/{id}", SentryHandler.GetBugId)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение бага по Guid Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение бага по Guid Sentry")
            .WithName("Get bug or id sentry")
            .WithTags("Integration/Sentry");

        #endregion

        #region Discord

        app.MapGet("/api/v1/integrations/discord", DiscordHandler.GetInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение DiscordRPC";
                return generatedOperation;
            })
            .WithDescription("Получение данных DiscordRPC")
            .WithName("Get discord RPC data")
            .WithTags("Integration/Discord")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPut("/api/v1/integrations/discord", DiscordHandler.UpdateInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление DiscordRPC";
                return generatedOperation;
            })
            .WithDescription("Обновление данных DiscordRPC")
            .WithName("Update discord RPC data")
            .WithTags("Integration/Discord")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        #endregion

        #region Textures

        app.MapGet("/api/v1/integrations/texture/skins", TextureIntegrationHandler.GetSkinUrl)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение ссылки на сервис со скинами";
                return generatedOperation;
            })
            .WithDescription("Получение ссылки на сервис со скинами")
            .WithName("Get skin texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPut("/api/v1/integrations/texture/skins", TextureIntegrationHandler.SetSkinUrl)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление ссылки на сервис со скинами";
                return generatedOperation;
            })
            .WithDescription("Обновление ссылки на сервис со скинами")
            .WithName("Update skin texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapGet("/api/v1/integrations/texture/cloaks", TextureIntegrationHandler.GetCloakUrl)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение ссылки на сервис с плащами";
                return generatedOperation;
            })
            .WithDescription("Получение ссылки на сервис с плащами")
            .WithName("Get cloak texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPut("/api/v1/integrations/texture/cloaks", TextureIntegrationHandler.SetCloakUrl)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление ссылки на сервис с плащами";
                return generatedOperation;
            })
            .WithDescription("Обновление ссылки на сервис с плащами")
            .WithName("Update cloak texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPost("/api/v1/integrations/texture/skins/load", TextureIntegrationHandler.UpdateUserSkin)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление скина пользователя";
                return generatedOperation;
            })
            .WithDescription("Обновление скина пользователя")
            .WithName("Upload skin texture")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/v1/integrations/texture/cloak/load", TextureIntegrationHandler.UpdateUserCloak)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление плаща пользователя";
                return generatedOperation;
            })
            .WithDescription("Обновление плаща пользователя")
            .WithName("Upload cloak texture")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        #endregion

        #region Minecraft authlib

        app.MapGet("/api/v1/integrations/authlib/minecraft", MinecraftHandler.GetMetaData)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение метаданных для Authlib injector";
                return generatedOperation;
            })
            .WithDescription("Получение метаданных для Authlib injector")
            .WithName("Integration with authlib, get metadata")
            .WithTags("Integration/Minecraft/AuthLib")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);


        app.MapPost("/api/v1/integrations/authlib/minecraft/sessionserver/session/minecraft/join",
                MinecraftHandler.Join)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Реализация метода Minecraft Join";
                return generatedOperation;
            })
            .WithDescription("Реализация метода Minecraft Join")
            .WithName("Integration with authlib, join")
            .WithTags("Integration/Minecraft/AuthLib")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/authlib/minecraft/sessionserver/session/minecraft/hasJoined",
                MinecraftHandler.HasJoined)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Реализация метода Minecraft HasJoin";
                return generatedOperation;
            })
            .WithDescription("Реализация метода Minecraft HasJoin")
            .WithName("Implementation of Minecraft's HasJoin method")
            .WithTags("Integration/Minecraft/AuthLib")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/authlib/minecraft/sessionserver/session/minecraft/profile/{uuid}",
                MinecraftHandler.GetProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Реализация получения профиля пользователя Minecraft";
                return generatedOperation;
            })
            .WithDescription("Реализация профиля пользователя Minecraft")
            .WithName("Implementation of Minecraft user profile retrieval")
            .WithTags("Integration/Minecraft/AuthLib")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/v1/integrations/authlib/minecraft/profiles/minecraft", MinecraftHandler.GetPlayersUuids)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Реализация Uuid профилей";
                return generatedOperation;
            })
            .WithDescription("Получение метаданных для Authlib injector")
            .WithName("Implementation of Uuid profiles")
            .WithTags("Integration/Minecraft/AuthLib")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/authlib/minecraft/player/attributes", MinecraftHandler.GetPlayerAttribute)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение атрибутов пользователя";
                return generatedOperation;
            })
            .WithDescription("Получение метаданных для Authlib injector")
            .WithName(" Getting user attributes")
            .WithTags("Integration/Minecraft/AuthLib")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        #endregion

        #region Auth

        app.MapPost("/api/v1/integrations/auth/signin", AuthIntegrationHandler.Auth)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Аутентификация через промежуточный сервис авторизации";
                return generatedOperation;
            })
            .WithDescription("Аутентификация через промежуточный сервис авторизации")
            .WithName("Auth")
            .WithTags("Integration/Auth")
            .Produces<ResponseMessage<PlayerReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPut("/api/v1/integrations/auth", AuthIntegrationHandler.SetAuthService)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление информации о промежуточном сервисе авторизации";
                return generatedOperation;
            })
            .WithDescription("Обновление сервиса авторизации")
            .WithName("Update auth service")
            .WithTags("Integration/Auth")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapGet("/api/v1/integrations/auth", AuthIntegrationHandler.GetIntegrationServices)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка сервисов авторизации";
                return generatedOperation;
            })
            .WithDescription("Получение списка сервисов авторизации")
            .WithName("Auth services list")
            .WithTags("Integration/Auth")
            .Produces<ResponseMessage<List<AuthServiceReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapGet("/api/v1/integrations/auth/active", AuthIntegrationHandler.GetAuthService)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение активного сервиса авторизации";
                return generatedOperation;
            })
            .WithDescription("Получение активного сервиса авторизации")
            .WithName("Get active auth service")
            .WithTags("Integration/Auth")
            .Produces<ResponseMessage<AuthServiceReadDto>>()
            .RequireAuthorization();

        app.MapDelete("/api/v1/integrations/auth/active", AuthIntegrationHandler.RemoveAuthService)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление активного сервиса авторизации";
                return generatedOperation;
            })
            .WithDescription("Удаление активного сервиса авторизации")
            .WithName("Remove active auth service")
            .WithTags("Integration/Auth")
            .Produces<ResponseMessage<AuthServiceReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        #endregion

        #endregion

        #region Profiles

        app.MapGet("/api/v1/profiles", ProfileHandler.GetProfiles)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка профилилей";
                return generatedOperation;
            })
            .WithDescription("Получение списка профиля")
            .WithName("Profiles list")
            .WithTags("Profiles")
            .Produces<ResponseMessage<List<ProfileReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/profiles/versions/{gameLoader}/{minecraftVersion}", ProfileHandler.GetMinecraftVersions)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка версий под каждый загрузчик Minecraft";
                return generatedOperation;
            })
            .WithDescription("Получение списка версий Minecraft")
            .WithName("Minecraft versions")
            .WithTags("Profiles")
            .Produces<ResponseMessage<List<string>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPost("/api/v1/profiles", ProfileHandler.CreateProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Создание игрового профиля";
                return generatedOperation;
            })
            .WithDescription("Создание игрового профиля")
            .WithName("Create profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPut("/api/v1/profiles", ProfileHandler.UpdateProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление игрового профиля";
                return generatedOperation;
            })
            .WithDescription("Обновление игрового профиля")
            .WithName("Update profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPost("/api/v1/profiles/restore", ProfileHandler.RestoreProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Установка (загрузка серверной части) игрового профиля";
                return generatedOperation;
            })
            .WithDescription("Установка игрового профиля")
            .WithName("Restore profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapDelete("/api/v1/profiles/{profileNames}", ProfileHandler.RemoveProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление игрового профиля";
                return generatedOperation;
            })
            .WithDescription("Удаление игрового профиля")
            .WithName("Remove profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        app.MapPost("/api/v1/profiles/info", ProfileHandler.GetProfileInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение информации об игровом профиле";
                return generatedOperation;
            })
            .WithDescription("Получение информации об игровом профиле")
            .WithName("Get profile info")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadInfoDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/v1/profiles/compile", ProfileHandler.CompileProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Компиляция игрового профиля";
                return generatedOperation;
            })
            .WithDescription("Компиляция игрового профиля")
            .WithName("Compile profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadInfoDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization();

        #endregion

        #region Files

        app.MapGet("/api/v1/file/{fileHash}", FileHandler.GetFile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение файла на загрузку";
                return generatedOperation;
            })
            .WithDescription("Получение файла на загрузку")
            .WithName("Download file")
            .WithTags("Files")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound);

        app.MapPost("/api/v1/file/whiteList", FileHandler.AddFileWhiteList)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Добавление файла в White-Лист";
                return generatedOperation;
            })
            .WithDescription("Добавление файла в White-Лист")
            .WithName("Add file to white list")
            .WithTags("Files")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization();

        app.MapDelete("/api/v1/file/whiteList", FileHandler.RemoveFileWhiteList)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление файла из White-Листа";
                return generatedOperation;
            })
            .WithDescription("Удаление файла из White-Лист")
            .WithName("Remove file from white list")
            .WithTags("Files")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization();

        app.MapPost("/api/v1/folder/whiteList", FileHandler.AddFolderWhiteList)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Добавление папок в White-Лист";
                return generatedOperation;
            })
            .WithDescription("Добавление папок в White-Лист")
            .WithName("Add folder to white list")
            .WithTags("Files")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization();

        app.MapDelete("/api/v1/folder/whiteList", FileHandler.RemoveFolderWhiteList)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление папок из White-Листа";
                return generatedOperation;
            })
            .WithDescription("Удаление папок из White-Лист")
            .WithName("Remove folder from white list")
            .WithTags("Files")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization();

        #endregion

        #region Settings

        app.MapGet("/api/v1/settings/platform", SettingsHandler.GetSettings)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение конфигурации платформы";
                return generatedOperation;
            })
            .WithDescription("Получение конфигурации платформы")
            .WithName("Get settings")
            .WithTags("Settings")
            .Produces<ResponseMessage<SettingsReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization();


        app.MapPut("/api/v1/settings/platform", SettingsHandler.UpdateSettings)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление конфигурации платформы";
                return generatedOperation;
            })
            .WithDescription("Обновление конфигурации платформы")
            .WithName("Update settings")
            .WithTags("Settings")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization();

        #endregion

        #region Plugins


        app.MapPost("/api/v1/plugins/install", PluginHandler.InstallPlugin)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Установка плагина в систему";
                return generatedOperation;
            })
            .WithDescription("Установка плагина в систему")
            .WithName("Install plugin")
            .WithTags("Plugins")
            .RequireAuthorization();


        app.MapGet("/api/v1/plugins", PluginHandler.GetInstalledPlugins)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка установленных плагинов";
                return generatedOperation;
            })
            .WithDescription("Получение списка установленных плагинов")
            .WithName("Get installed plugin")
            .WithTags("Plugins")
            .Produces<ResponseMessage<PluginVersionReadDto[]>>()
            .RequireAuthorization();


        app.MapDelete("/api/v1/plugins/{name}/{version}", PluginHandler.RemovePlugin)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление плагина из системы";
                return generatedOperation;
            })
            .WithDescription("Удаление плагина из системы")
            .WithName("Remove plugin")
            .WithTags("Plugins")
            .RequireAuthorization();

        #endregion

        #region Launcher

        app.MapPost("/api/v1/launcher/upload", LauncherUpdateHandler.UploadLauncherVersion)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Зарузка новой версии лаунчера";
                return generatedOperation;
            })
            .WithDescription("Загрузка новой версии лаунчера")
            .WithName("Upload launcher version")
            .WithTags("Launcher")
            .RequireAuthorization();

        app.MapGet("/api/v1/launcher", LauncherUpdateHandler.GetActualVersion)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение актуальной версии лаунчера";
                return generatedOperation;
            })
            .WithDescription("Получение актуальной версии лаунчера")
            .WithName("Get actual launcher version")
            .WithTags("Launcher");

        app.MapGet("/api/v1/launcher/builds", LauncherUpdateHandler.GetBuilds)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка сборок";
                return generatedOperation;
            })
            .WithDescription("Получение списка сборок")
            .WithName("Get launcher builds")
            .WithTags("Launcher")
            .RequireAuthorization();

        app.MapGet("/api/v1/launcher/platforms", LauncherUpdateHandler.GetPlatforms)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка платформ для сборки";
                return generatedOperation;
            })
            .WithDescription("Получение списка платформ для сборки")
            .WithName("Get launcher platforms")
            .WithTags("Launcher")
            .RequireAuthorization();

        #endregion

        #region Servers

        app.MapGet("/api/v1/servers/{profileName}", ServersHandler.GetServers)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка серверов у профиля";
                return generatedOperation;
            })
            .WithDescription("Получение списка серверов у профиля")
            .WithName("Get profile game servers")
            .WithTags("MinecraftServers")
            .Produces<ResponseMessage<List<ServerReadDto>>>()
            .RequireAuthorization();

        app.MapPost("/api/v1/servers/{profileName}", ServersHandler.CreateServer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Создание сервера у профиля";
                return generatedOperation;
            })
            .WithDescription("Создание сервера у профиля")
            .WithName("Create server to game profile")
            .WithTags("MinecraftServers")
            .RequireAuthorization();

        app.MapDelete("/api/v1/servers/{profileName}/{serverNamesString}", ServersHandler.RemoveServer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление сервера в игровом профиле";
                return generatedOperation;
            })
            .WithDescription("Удаление сервера в игровом профиле")
            .WithName("Remove server from game profile")
            .WithTags("MinecraftServers")
            .RequireAuthorization();

        #endregion

        #region Servers

        app.MapGet("/api/v1/notifications", NotificationHandler.GetNotifications)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка уведомлений";
                return generatedOperation;
            })
            .WithDescription("Получение списка уведомлений")
            .WithName("Get profile notifications")
            .WithTags("Notifications")
            .Produces<ResponseMessage<List<INotification>>>()
            .RequireAuthorization();

        app.MapDelete("/api/v1/notifications", NotificationHandler.ClearNotification)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление всех уведомлений";
                return generatedOperation;
            })
            .WithDescription("Удаление всех уведомлений")
            .WithName("Delete all notifications")
            .WithTags("Notifications")
            .Produces<ResponseMessage>()
            .RequireAuthorization();

        #endregion

        return app;
    }
}
