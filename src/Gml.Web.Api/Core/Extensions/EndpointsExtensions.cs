using System.Net;
using Gml.Web.Api.Core.Handlers;
using Gml.Web.Api.Core.Hubs;
using Gml.Web.Api.Core.Messages;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Extensions;

public static class EndpointsExtensions
{
    public static WebApplication RegisterEndpoints(this WebApplication app)
    {
        #region Root

        app.MapGet("/", () => Results.Redirect("/swagger", true));

        #endregion

        #region SignalR Hubs

        app.MapHub<ProfileHub>("/ws/profiles/restore").RequireAuthorization();

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

        app.MapPost("/api/v1/integrations/auth/signin", IntegrationHandler.Auth)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Аутентификация через промежуточный сервис авторизации";
                return generatedOperation;
            })
            .WithDescription("Аутентификация через промежуточный сервис авторизации")
            .WithName("Auth")
            .WithTags("Integration")
            .Produces<ResponseMessage<PlayerReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPut("/api/v1/integrations/auth", IntegrationHandler.SetAuthService)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Обновление информации о промежуточном сервисе авторизации";
                return generatedOperation;
            })
            .WithDescription("Обновление сервиса авторизации")
            .WithName("Update auth service")
            .WithTags("Integration")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/auth", IntegrationHandler.GetIntegrationServices)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение списка сервисов авторизации";
                return generatedOperation;
            })
            .WithDescription("Получение списка сервисов авторизации")
            .WithName("Auth services list")
            .WithTags("Integration")
            .Produces<ResponseMessage<List<AuthServiceReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/v1/integrations/auth/active", IntegrationHandler.GetAuthService)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Получение активного сервиса авторизации";
                return generatedOperation;
            })
            .WithDescription("Получение активного сервиса авторизации")
            .WithName("Get active auth service")
            .WithTags("Integration")
            .Produces<ResponseMessage<AuthServiceReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapDelete("/api/v1/integrations/auth/active", IntegrationHandler.RemoveAuthService)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление активного сервиса авторизации";
                return generatedOperation;
            })
            .WithDescription("Удаление активного сервиса авторизации")
            .WithName("Remove active auth service")
            .WithTags("Integration")
            .Produces<ResponseMessage<AuthServiceReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

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
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

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
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

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
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapDelete("/api/v1/profiles/{profileName}", ProfileHandler.RemoveProfile)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Удаление игрового профиля";
                return generatedOperation;
            })
            .WithDescription("Удаление игрового профиля")
            .WithName("Remove profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

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

        #endregion

        return app;
    }
}