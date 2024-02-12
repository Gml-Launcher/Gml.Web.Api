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
        
        #region SignalR Hubs

        app.MapHub<ProfileHub>("/ws/profiles/restore").RequireAuthorization();;

        #endregion

        #region Auth

        app.MapPost("/api/v1/users/signup", AuthHandler.CreateUser)
            .WithDescription("Регистрация нового пользователя")
            .WithName("Create User")
            .WithTags("Users")
            .Produces<ResponseMessage<UserAuthReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/v1/users/signin", AuthHandler.AuthUser)
            .WithDescription("Авторизация")
            .WithName("Authenticate User")
            .WithTags("Users")
            .Produces<ResponseMessage<UserAuthReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);

        #endregion

        #region Integrations

        app.MapPost("/api/v1/integrations/auth/signin", IntegrationHandler.Auth)
            .WithDescription("Авторизация через соответствующий сервиис")
            .WithName("Auth")
            .WithTags("Integration")
            .Produces<ResponseMessage<PlayerReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapPut("/api/v1/integrations/auth", IntegrationHandler.SetAuthService)
            .WithDescription("Обновление сервиса авторизации")
            .WithName("Update auth service")
            .WithTags("Integration")
            .Produces<ResponseMessage>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapGet("/api/v1/integrations/auth", IntegrationHandler.GetIntegrationServices)
            .WithDescription("Получение списка сервисов авторизации")
            .WithName("Auth services list")
            .WithTags("Integration")
            .Produces<ResponseMessage<List<AuthServiceReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapGet("/api/v1/integrations/auth/active", IntegrationHandler.GetAuthService)
            .WithDescription("Получение активного сервиса авторизации")
            .WithName("Get active auth service")
            .WithTags("Integration")
            .Produces<ResponseMessage<AuthServiceReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapDelete("/api/v1/integrations/auth/active", IntegrationHandler.RemoveAuthService)
            .WithDescription("Удаление активного сервиса авторизации")
            .WithName("Remove active auth service")
            .WithTags("Integration")
            .Produces<ResponseMessage<AuthServiceReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        
        #endregion

        #region Profiles

        app.MapGet("/api/v1/profiles", ProfileHandler.GetProfiles)
            .WithDescription("Получение списка профиля")
            .WithName("Profiles list")
            .WithTags("Profiles")
            .Produces<ResponseMessage<List<ProfileReadDto>>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapPost("/api/v1/profiles", ProfileHandler.CreateProfile)
            .WithDescription("Создание игрового профиля")
            .WithName("Create profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapPut("/api/v1/profiles", ProfileHandler.UpdateProfile)
            .WithDescription("Обновление игрового профиля")
            .WithName("Update profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapPost("/api/v1/profiles/restore", ProfileHandler.RestoreProfile)
            .WithDescription("Установка игрового профиля")
            .WithName("Restore profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapDelete("/api/v1/profiles/{profileName}", ProfileHandler.RemoveProfile)
            .WithDescription("Удаление игрового профиля")
            .WithName("Remove profile")
            .WithTags("Profiles")
            .Produces<ResponseMessage>((int)HttpStatusCode.NotFound)
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        
        app.MapPost("/api/v1/profiles/info", ProfileHandler.GetProfileInfo)
            .WithDescription("Получение информации об игровом профиле")
            .WithName("Get profile info")
            .WithTags("Profiles")
            .Produces<ResponseMessage<ProfileReadInfoDto>>()
            .Produces<ResponseMessage>((int)HttpStatusCode.BadRequest);
        

        #endregion
        
        return app;
    }

    
}