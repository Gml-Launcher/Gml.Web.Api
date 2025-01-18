using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Core.User;
using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Domains.Integrations;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Gml.Web.Api.Core.Handlers;

public class AuthIntegrationHandler : IAuthIntegrationHandler
{
    public static async Task<IResult> Auth(
        HttpContext context,
        IGmlManager gmlManager,
        IMapper mapper,
        IValidator<BaseUserPassword> validator,
        IAuthService authService,
        BaseUserPassword authDto)
    {
        try
        {
            var result = await validator.ValidateAsync(authDto);

            if (!result.IsValid)
                return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                    HttpStatusCode.BadRequest));

            var authType = await gmlManager.Integrations.GetAuthType();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            if (string.IsNullOrWhiteSpace(userAgent))
                return Results.BadRequest(ResponseMessage.Create(
                    "Не удалось определить устройство, с которого произошла авторизация",
                    HttpStatusCode.BadRequest));

            if (authType is not AuthType.Any && string.IsNullOrEmpty(authDto.Password))
            {
                return Results.BadRequest(ResponseMessage.Create(
                    "Не указан пароль при авторизации!",
                    HttpStatusCode.BadRequest));
            }

            var authResult = await authService.CheckAuth(authDto.Login, authDto.Password, authType);

            if (authResult.IsSuccess)
            {
                var player = await gmlManager.Users.GetAuthData(
                    authResult.Login ?? authDto.Login,
                    authDto.Password,
                    userAgent,
                    context.Request.Protocol,
                    context.Connection.RemoteIpAddress,
                    authResult.Uuid,
                    context.Request.Headers["X-HWID"]);

                if (player.IsBanned)
                {
                    return Results.BadRequest(ResponseMessage.Create(
                        "Пользователь заблокирован!",
                        HttpStatusCode.BadRequest));
                }

                await gmlManager.Profiles.CreateUserSessionAsync(null, player);

                player.TextureSkinUrl ??= (await gmlManager.Integrations.GetSkinServiceAsync())
                    .Replace("{userName}", player.Name)
                    .Replace("{userUuid}", player.Uuid);

                return Results.Ok(ResponseMessage.Create(
                    mapper.Map<PlayerReadDto>(player),
                    string.Empty,
                    HttpStatusCode.OK));
            }

            return Results.BadRequest(ResponseMessage.Create(authResult.Message ?? "Неверный логин или пароль", HttpStatusCode.Unauthorized));
        }
        catch (HttpRequestException exception)
        {
            Console.WriteLine(exception);
            return Results.BadRequest(ResponseMessage.Create(
                "Произошла ошибка при обмене данных с сервисом авторизации.", HttpStatusCode.InternalServerError));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.InternalServerError));
        }
    }
    public static async Task<IResult> AuthWithToken(
        HttpContext context,
        IGmlManager gmlManager,
        IMapper mapper,
        IAuthService authService,
        BaseUserPassword authDto)
    {
        try
        {
            var authType = await gmlManager.Integrations.GetAuthType();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            if (string.IsNullOrWhiteSpace(userAgent))
                return Results.BadRequest(ResponseMessage.Create(
                    "Не удалось определить устройство, с которого произошла авторизация",
                    HttpStatusCode.BadRequest));

            if (authType is not AuthType.Any && string.IsNullOrEmpty(authDto.AccessToken))
            {
                return Results.BadRequest(ResponseMessage.Create(
                    "Не был передан AccessToken",
                    HttpStatusCode.BadRequest));
            }

            var user = await gmlManager.Users.GetUserByAccessToken(authDto.AccessToken);

            if (user is not null && user.ExpiredDate> DateTime.Now)
            {
                var player = user;

                if (player.IsBanned)
                {
                    return Results.BadRequest(ResponseMessage.Create(
                        "Пользователь заблокирован!",
                        HttpStatusCode.BadRequest));
                }

                player.TextureSkinUrl ??= (await gmlManager.Integrations.GetSkinServiceAsync())
                    .Replace("{userName}", player.Name)
                    .Replace("{userUuid}", player.Uuid);

                _ = gmlManager.Profiles.CreateUserSessionAsync(null, player);

                return Results.Ok(ResponseMessage.Create(
                    mapper.Map<PlayerReadDto>(player),
                    string.Empty,
                    HttpStatusCode.OK));
            }
        }
        catch (HttpRequestException exception)
        {
            Console.WriteLine(exception);
            return Results.BadRequest(ResponseMessage.Create(
                "Произошла ошибка при обмене данных с сервисом авторизации.", HttpStatusCode.InternalServerError));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.InternalServerError));
        }

        return Results.BadRequest(ResponseMessage.Create("Неверный логин или пароль", HttpStatusCode.Unauthorized));
    }

    [Authorize]
    public static async Task<IResult> GetIntegrationServices(IGmlManager gmlManager, IMapper mapper)
    {
        var authServices = await gmlManager.Integrations.GetAuthServices();

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<AuthServiceReadDto>>(authServices), string.Empty,
            HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> GetAuthService(IGmlManager gmlManager, IMapper mapper)
    {
        var activeAuthService = await gmlManager.Integrations.GetActiveAuthService();

        return activeAuthService == null
            ? Results.NotFound(ResponseMessage.Create("Не настроен сервис для авторизации", HttpStatusCode.NotFound))
            : Results.Ok(ResponseMessage.Create(mapper.Map<AuthServiceReadDto>(activeAuthService), string.Empty,
                HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> SetAuthService(
        IGmlManager gmlManager,
        IValidator<IntegrationUpdateDto> validator,
        IntegrationUpdateDto updateDto)
    {
        var result = await validator.ValidateAsync(updateDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var service = await gmlManager.Integrations.GetAuthService(updateDto.AuthType);

        if (service == null)
            service = (await gmlManager.Integrations.GetAuthServices()).FirstOrDefault(c =>
                c.AuthType == updateDto.AuthType);

        if (service == null) return Results.NotFound();

        service.Endpoint = updateDto.Endpoint;

        await gmlManager.Integrations.SetActiveAuthService(service);

        return Results.Ok(ResponseMessage.Create("Сервис авторизации успешно обновлен", HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> RemoveAuthService(IGmlManager gmlManager)
    {
        await gmlManager.Integrations.SetActiveAuthService(null);

        return Results.Ok(ResponseMessage.Create("Сервис авторизации успешно удален", HttpStatusCode.OK));
    }
}
