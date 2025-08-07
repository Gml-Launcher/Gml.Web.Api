using System.Net;
using Gml.Models.News;
using Gml.Web.Api.Core.Handlers;
using Gml.Web.Api.Core.Hubs;
using Gml.Web.Api.Domains.LauncherDto;
using Gml.Web.Api.Domains.Plugins;
using Gml.Web.Api.Domains.Servers;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.News;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.Servers;
using Gml.Web.Api.Dto.Settings;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces.Notifications;
using GmlCore.Interfaces.User;

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        #endregion

        #region SignalR Hubs

        app.MapHub<ProfileHub>("/ws/profiles/restore")
            .RequireAuthorization(c => c.RequireRole("Admin"));
        app.MapHub<GitHubLauncherHub>("/ws/launcher/build")
            .RequireAuthorization(c => c.RequireRole("Admin"));
        app.MapHub<GameServerHub>("/ws/gameServer")
            .RequireAuthorization(c => c.RequireRole("Admin"));
        app.MapHub<LauncherHub>("/ws/launcher").RequireAuthorization();
        app.MapHub<NotificationHub>("/ws/notifications")
            .RequireAuthorization(c => c.RequireRole("Admin"));

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

        app.MapGet("/api/v1/users/info/{userName}", AuthHandler.UserInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение общей информации о пользователе";
                return generatedOperation;
            })
            .WithDescription("Получение общей информации о пользователе")
            .WithName("User info")
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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/{projectId}/envelope", SentryHandler.CreateBugInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Добавление ошибок Sentry";
                return generatedOperation;
            })
            .WithDescription("Добавление ошибок Sentry")
            .WithName("Get sentry message")
            .WithTags("Integration/Sentry");

        app.MapPost("/api/v1/sentry", SentryHandler.GetBugs)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение всех ошибок Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение всех ошибок Sentry")
            .WithName("Get all bugs sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/sentry/clear", SentryHandler.SolveAllBugs)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Очистка всех ошибок Sentry";
                return generatedOperation;
            })
            .WithDescription("Очистка всех ошибок Sentry")
            .WithName("Clear all bugs sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/sentry/filter", SentryHandler.GetFilterSentry)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение отфильтрованного списка ошибок";
                return generatedOperation;
            })
            .WithDescription("Получение отфильтрованного списка ошибок")
            .WithName("Get filtered bugs sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/sentry/filter/list", SentryHandler.GetFilterListSentry)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение отфильтрованного списка по ошибок";
                return generatedOperation;
            })
            .WithDescription("Получение отфильтрованного списка по ошибок")
            .WithName("Get filtered on bugs sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/sentry/stats/last", SentryHandler.GetLastSentryErrors)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка ошибок за последние 3 месяца";
                return generatedOperation;
            })
            .WithDescription("Получение списка ошибок за последние 3 месяца")
            .WithName("Get last bugs sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/sentry/stats/summary", SentryHandler.GetSummarySentryErrors)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получить сводку ошибок";
                return generatedOperation;
            })
            .WithDescription("Получить сводку ошибок")
            .WithName("Get summary bugs sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/sentry/{exception}", SentryHandler.GetByException)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение exception в Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение exception в Sentry")
            .WithName("Get exception on sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/sentry/bug/{id}", SentryHandler.GetBugId)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение бага по Guid Sentry";
                return generatedOperation;
            })
            .WithDescription("Получение бага по Guid Sentry")
            .WithName("Get bug or id sentry")
            .WithTags("Integration/Sentry")
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/integrations/texture/skins/{textureGuid}", TextureIntegrationHandler.GetUserSkin)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение скина пользователя";
                return generatedOperation;
            })
            .WithDescription("Получение скина пользователя")
            .WithName("Get user skin texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/texture/capes/{textureGuid}", TextureIntegrationHandler.GetUserCloak)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение плаща пользователя";
                return generatedOperation;
            })
            .WithDescription("Получение плаща пользователя")
            .WithName("Get user cloak texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/texture/head/{userUuid}", TextureIntegrationHandler.GetUserHead)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение текстуры лица пользователя";
                return generatedOperation;
            })
            .WithDescription("Получение текстуры лица пользователя")
            .WithName("Get user head texture url")
            .WithTags("Integration/Textures")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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

        app.MapPost("/api/v1/integrations/texture/cloaks/load", TextureIntegrationHandler.UpdateUserCloak)
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

        app.MapPost("/api/v1/integrations/auth/checkToken", AuthIntegrationHandler.AuthWithToken)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Проверка актуальности токена авторизации";
                return generatedOperation;
            })
            .WithDescription("Проверка актуальности токена авторизации")
            .WithName("Auth with access token")
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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        #endregion

        #region News

        app.MapPost("/api/v1/integrations/news", NewsHandler.AddNewsListener)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Добавление слушателя новостей";
                return generatedOperation;
            })
            .WithDescription("Добавление слушателя новостей")
            .WithName("Add news listeners")
            .WithTags("Integration/News")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapDelete("/api/v1/integrations/news/{type}", NewsHandler.RemoveNewsListener)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление слушателя новостей";
                return generatedOperation;
            })
            .WithDescription("Удаление слушателя новостей")
            .WithName("Remove news listeners")
            .WithTags("Integration/News")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/integrations/news/providers", NewsHandler.GetListeners)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка слушателей новостей";
                return generatedOperation;
            })
            .WithDescription("Получение списка слушателей новостей")
            .WithName("Get news listeners")
            .WithTags("Integration/News")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/integrations/news", NewsHandler.GetNewsListener)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка слушателя новостей";
                return generatedOperation;
            })
            .WithDescription("Получение списка слушателя новостей")
            .WithName("Get list of news listeners")
            .WithTags("Integration/News")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/integrations/news/list", NewsHandler.GetNews)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка новостей";
                return generatedOperation;
            })
            .WithDescription("Получение новостей")
            .WithName("Get list news")
            .WithTags("Integration/News")
            .Produces<ResponseMessage<NewsGetListenerDto[]>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        #endregion

        #endregion

        #region Profiles

        app.MapGet("/api/v1/profiles", ProfileHandler.GetProfiles)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка профилей";
                return generatedOperation;
            })
            .WithDescription("Получение списка профиля")
            .WithName("Profiles list")
            .WithTags("Profiles")
            .Produces<ResponseMessage<List<ProfileReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Player", "Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/profiles/{profileName}/players/whitelist/{userUuid}", ProfileHandler.AddPlayerToWhiteList)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Добавление игрока в белый список профиля";
                return generatedOperation;
            })
            .WithDescription("Добавление игрока в белый список профиля")
            .WithName("Add users white list profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/profiles/{profileName}/mods", ProfileHandler.GetMods)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка модов в профиле";
                return generatedOperation;
            })
            .WithDescription("Получение списка модов в профиле")
            .WithName("Get profile mods")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPut("/api/v1/mods/details", ProfileHandler.UpdateModInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление информации о моде";
                return generatedOperation;
            })
            .WithDescription("Обновление информации о моде")
            .WithName("Update mod details")
            .WithTags("Mods")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/mods/details", ProfileHandler.GetModsDetails)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение информации о модификациях";
                return generatedOperation;
            })
            .WithDescription("Получение информации о модификациях")
            .WithName("Get mod details")
            .WithTags("Mods")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin", "Player"));

        app.MapPost("/api/v1/profiles/{profileName}/mods/load", ProfileHandler.LoadMod)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Загрузка мода в профиль";
                return generatedOperation;
            })
            .WithDescription("Загрузка мода в профиль")
            .WithName("Load profile mods")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/profiles/{profileName}/mods/load/url", ProfileHandler.LoadByLink)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Загрузка мода в профиль по ссылке";
                return generatedOperation;
            })
            .WithDescription("Загрузка мода в профиль по ссылке")
            .WithName("Load profile mods by link")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapDelete("/api/v1/profiles/{profileName}/mods/remove/{fileName}", ProfileHandler.RemoveMod)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление мода из профиля";
                return generatedOperation;
            })
            .WithDescription("Удаление мода из профиля")
            .WithName("Remove profile mods")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/profiles/{profileName}/mods/optionals", ProfileHandler.GetOptionalsMods)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка опциональных модов в профиле";
                return generatedOperation;
            })
            .WithDescription("Получение списка опциональных модов в профиле")
            .WithName("Get optional profile mods")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin", "Player"));

        app.MapGet("/api/v1/profiles/{profileName}/mods/search", ProfileHandler.FindMods)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка доступных для загрузки модов в профиле";
                return generatedOperation;
            })
            .WithDescription("Получение списка доступных для загрузки модов в профиле")
            .WithName("Get available profile mods")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/profiles/{profileName}/mods/info", ProfileHandler.GetModInfo)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получить детальную информацию по моду";
                return generatedOperation;
            })
            .WithDescription("Получить детальную информацию по моду")
            .WithName("Get mod info")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapDelete("/api/v1/profiles/{profileName}/players/whitelist/{userUuid}", ProfileHandler.RemovePlayerFromWhiteList)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление игрока из белого списка профиля";
                return generatedOperation;
            })
            .WithDescription("Удаление игрока из белого списка профиля")
            .WithName("Remove user from profile white list")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

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

        app.MapPost("/api/v1/profiles/details", ProfileHandler.GetProfileDetails)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение информации об игровом профиле для панели управления";
                return generatedOperation;
            })
            .WithDescription("Получение информации об игровом профиле для панели управления")
            .WithName("Get profile details")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadInfoDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        #endregion

        #region Players

        app.MapGet("/api/v1/players", PlayersHandler.GetPlayers)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка игроков";
                return generatedOperation;
            })
            .WithDescription("Получение списка игроков")
            .WithName("Players list")
            .WithTags("Players")
            .Produces<ResponseMessage<List<IUser>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/players/ban", PlayersHandler.BanPlayer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Блокировка списка игроков";
                return generatedOperation;
            })
            .WithDescription("Блокировка списка игроков")
            .WithName("Ban players")
            .WithTags("Players")
            .Produces<ResponseMessage<List<IUser>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/players/remove", PlayersHandler.RemovePlayer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление пользователй из списка игроков";
                return generatedOperation;
            })
            .WithDescription("Удаление пользователй из списка игроков")
            .WithName("Remove players")
            .WithTags("Players")
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/players/pardon", PlayersHandler.PardonPlayer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Разблокировка списка игроков";
                return generatedOperation;
            })
            .WithDescription("Разблокировка списка игроков")
            .WithName("Pardon players")
            .WithTags("Players")
            .Produces<ResponseMessage<List<IUser>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest)
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));


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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));


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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/plugins/{id}/script", PluginHandler.GetPluginScript)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение JS-сегмента плагина";
                return generatedOperation;
            })
            .WithDescription("Получение JS-сегмента плагина")
            .WithName("Get plugin script")
            .WithTags("Plugins")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/plugins/script/{place}", PluginHandler.GetPluginByPlaceScript)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение JS-сегмента плагинов снизу, после авторизации";
                return generatedOperation;
            })
            .WithDescription("Получение JS-сегмента плагинов снизу, после авторизации")
            .WithName("Get plugin scripts after auth form")
            .WithTags("Plugins");


        app.MapDelete("/api/v1/plugins/{id}", PluginHandler.RemovePlugin)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление плагина из системы";
                return generatedOperation;
            })
            .WithDescription("Удаление плагина из системы")
            .WithName("Remove plugin")
            .WithTags("Plugins")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        #endregion

        #region Launcher

        app.MapPost("/api/v1/launcher/upload", LauncherUpdateHandler.UploadLauncherVersion)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Загрузка новой версии лаунчера";
                return generatedOperation;
            })
            .WithDescription("Загрузка новой версии лаунчера")
            .WithName("Upload launcher version")
            .WithTags("Launcher")
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapGet("/api/v1/launcher/platforms", LauncherUpdateHandler.GetPlatforms)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка платформ для сборки";
                return generatedOperation;
            })
            .WithDescription("Получение списка платформ для сборки")
            .WithName("Get launcher platforms")
            .WithTags("Launcher")
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapPost("/api/v1/servers/{profileName}", ServersHandler.CreateServer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Создание сервера у профиля";
                return generatedOperation;
            })
            .WithDescription("Создание сервера у профиля")
            .WithName("Create server to game profile")
            .WithTags("MinecraftServers")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        app.MapDelete("/api/v1/servers/{profileName}/{serverNamesString}", ServersHandler.RemoveServer)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление сервера в игровом профиле";
                return generatedOperation;
            })
            .WithDescription("Удаление сервера в игровом профиле")
            .WithName("Remove server from game profile")
            .WithTags("MinecraftServers")
            .RequireAuthorization(c => c.RequireRole("Admin"));

        #endregion

        #region Notifications

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

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
            .RequireAuthorization(c => c.RequireRole("Admin"));

        #endregion

        return app;
    }
}
