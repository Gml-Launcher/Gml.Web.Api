using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Dto.Integration;
using Gml.Dto.Messages;
using Gml.Dto.Player;
using Gml.Dto.User;
using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Auth;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Handlers;

public class AuthIntegrationHandler : IAuthIntegrationHandler
{
    private static async Task<IResult?> HandleCommonAuthValidation(HttpContext context,
        IGmlManager gmlManager,
        AuthType authType,
        string hwid)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        if (string.IsNullOrWhiteSpace(userAgent) || string.IsNullOrWhiteSpace(hwid))
            return Results.BadRequest(ResponseMessage.Create(
                "Не удалось определить устройство, с которого произошла авторизация",
                HttpStatusCode.BadRequest));

        return null;
    }

    private static async Task<IResult> HandleAuthenticatedUser(HttpContext context, IGmlManager gmlManager,
        IMapper mapper,
        IUser player,
        string userAgent)
    {
        if (player.IsBanned)
        {
            return Results.BadRequest(ResponseMessage.Create(
                "Пользователь заблокирован!",
                HttpStatusCode.BadRequest));
        }

        var hostValue = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();

        await gmlManager.Profiles.CreateUserSessionAsync(null, player, hostValue);

        // player.TextureSkinUrl = (await gmlManager.Integrations.GetSkinServiceAsync())
        //     .Replace("{userName}", player.Name)
        //     .Replace("{userUuid}", player.Uuid);
        //
        // player.TextureCloakUrl = (await gmlManager.Integrations.GetCloakServiceAsync())
        //     .Replace("{userName}", player.Name)
        //     .Replace("{userUuid}", player.Uuid);

        return Results.Ok(ResponseMessage.Create(
            mapper.Map<PlayerReadDto>(player),
            string.Empty,
            HttpStatusCode.OK));
    }

    private static IResult HandleAuthException(Exception exception, bool isHttpRequestException)
    {
        Console.WriteLine(exception);

        var errorMessage = string.Join('.',
            "Произошла ошибка при обмене данных с сервисом авторизации",
            exception.Message);

        return Results.BadRequest(ResponseMessage.Create(
            errorMessage,
            HttpStatusCode.InternalServerError));
    }

    public static async Task<IResult> Auth(
        HttpContext context,
        IGmlManager gmlManager,
        IAccessTokenService accessTokenService,
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

            var hwid = context.Request.Headers["X-HWID"].ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var authType = await gmlManager.Integrations.GetAuthType();

            var validationResult = await HandleCommonAuthValidation(context, gmlManager, authType, hwid);

            if (validationResult is not null)
                return validationResult;

            if (authType is not AuthType.Any && string.IsNullOrEmpty(authDto.Password))
            {
                return Results.BadRequest(ResponseMessage.Create(
                    "Не указан пароль при авторизации!",
                    HttpStatusCode.BadRequest));
            }

            var authResult = await authService.CheckAuth(authDto.Login, authDto.Password, authType, hwid, authDto.TwoFactorCode);

            if (authResult.TwoFactorEnabled && string.IsNullOrEmpty(authDto.TwoFactorCode))
            {
                return Results.BadRequest(ResponseMessage.Create(
                    "Введите код из приложения 2FA",
                    HttpStatusCode.Unauthorized));
            }

            if (!authResult.IsSuccess)
                return Results.BadRequest(ResponseMessage.Create(
                    authResult.Message ?? "Неверный логин или пароль",
                    HttpStatusCode.Unauthorized));

            var player = await gmlManager.Users.GetAuthData(
                authResult.Login ?? authDto.Login,
                authDto.Password,
                userAgent,
                context.Request.Protocol,
                context.ParseRemoteAddress(),
                authResult.Uuid,
                hwid,
                authResult.IsSlim);

            player.AccessToken = accessTokenService.GenerateAccessToken(
                player.Uuid,
                player.Name,
                player.Name,
                ["Player"], ["profiles.view"], 60 * 24 * 10); // 60 минут * 24 часа * 10 дней

            return await HandleAuthenticatedUser(context, gmlManager, mapper, player, userAgent);

        }
        catch (HttpRequestException exception)
        {
            gmlManager.BugTracker.CaptureException(exception);
            return HandleAuthException(exception, true);
        }
        catch (JsonReaderException exception)
        {
            gmlManager.BugTracker.CaptureException(exception);
            var humanException =
                new Exception("Не удалось прочитать ответ сервера, возможно сайт вернул html, вместо json, или неверно настроен сервер авторизации.");
            return HandleAuthException(humanException, true);
        }
        catch (Exception exception)
        {
            gmlManager.BugTracker.CaptureException(exception);
            return HandleAuthException(exception, false);
        }
    }

    public static async Task<IResult> AuthWithToken(
        HttpContext context,
        IGmlManager gmlManager,
        IMapper mapper,
        IAccessTokenService accessTokenService,
        IAuthService authService,
        BaseUserPassword authDto)
    {
        try
        {

            var authType = await gmlManager.Integrations.GetAuthType();

            if (authType is not AuthType.Any && string.IsNullOrEmpty(authDto.AccessToken))
            {
                return Results.BadRequest(ResponseMessage.Create(
                    "Не был передан AccessToken",
                    HttpStatusCode.BadRequest));
            }

            var user = await gmlManager.Users.GetUserByAccessToken(authDto.AccessToken);
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            if (user is not null && accessTokenService.ValidateToken(user.AccessToken))
            {
                return await HandleAuthenticatedUser(context, gmlManager, mapper, user, userAgent);
            }

            return Results.BadRequest(ResponseMessage.Create(
                "Неверный логин или пароль",
                HttpStatusCode.Unauthorized));
        }
        catch (HttpRequestException exception)
        {
            return HandleAuthException(exception, true);
        }
        catch (Exception exception)
        {
            return HandleAuthException(exception, false);
        }
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
