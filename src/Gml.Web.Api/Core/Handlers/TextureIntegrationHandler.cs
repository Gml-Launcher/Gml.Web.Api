using System.Net;
using FluentValidation;
using Gml.Core.User;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Texture;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;

namespace Gml.Web.Api.Core.Handlers;

public class TextureIntegrationHandler : ITextureIntegrationHandler
{
    public static async Task<IResult> GetSkinUrl(IGmlManager gmlManager)
    {
        try
        {
            var url = await gmlManager.Integrations.GetSkinServiceAsync();

            return Results.Ok(ResponseMessage.Create(new UrlServiceDto(url), "Успешно", HttpStatusCode.OK));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> SetSkinUrl(
        IGmlManager gmlManager,
        IValidator<UrlServiceDto> validator,
        UrlServiceDto urlDto)
    {
        var result = await validator.ValidateAsync(urlDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        await gmlManager.Integrations.SetSkinServiceAsync(urlDto.Url);

        return Results.Ok(ResponseMessage.Create("Сервис скинов успешно обновлен", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetCloakUrl(IGmlManager gmlManager)
    {
        try
        {
            var url = await gmlManager.Integrations.GetCloakServiceAsync();

            return Results.Ok(ResponseMessage.Create(new UrlServiceDto(url), "Успешно", HttpStatusCode.OK));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> SetCloakUrl(
        IGmlManager gmlManager,
        IValidator<UrlServiceDto> validator,
        UrlServiceDto urlDto)
    {
        var result = await validator.ValidateAsync(urlDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        await gmlManager.Integrations.SetCloakServiceAsync(urlDto.Url);

        return Results.Ok(ResponseMessage.Create("Сервис плащей успешно обновлен", HttpStatusCode.OK));
    }

    public static async Task<IResult> UpdateUserSkin(
        HttpContext context,
        ISkinServiceManager skinServiceManager,
        IGmlManager gmlManager)
    {
        var login = context.Request.Form["Login"].FirstOrDefault();
        var token = context.Request.Headers.Authorization.First()?.Split(' ').LastOrDefault();

        if (string.IsNullOrEmpty(login))
        {
            return Results.BadRequest(ResponseMessage.Create("Не заполнено обязательное поля \"Texture\"",
                HttpStatusCode.BadRequest));
        }

        if (await gmlManager.Users.GetUserByName(login) is not AuthUser user
            || string.IsNullOrEmpty(token)
            || string.IsNullOrEmpty(user.AccessToken)
            || !user.AccessToken.Equals(token))
        {
            return Results.NotFound(ResponseMessage.Create("Ошибка идентификации",
                HttpStatusCode.NotFound));
        }

        var texture = context.Request.Form.Files["Texture"]?.OpenReadStream();

        if (texture is null)
        {
            return Results.BadRequest(ResponseMessage.Create("Не заполнено обязательное поля \"Texture\"",
                HttpStatusCode.BadRequest));
        }

        await skinServiceManager.UpdateSkin(user, texture);

        return await skinServiceManager.UpdateCloak(user, texture)
            ? Results.Ok(ResponseMessage.Create("Скин успешно установлен!", HttpStatusCode.OK))
            : Results.BadRequest(ResponseMessage.Create("Не удалось обновить скин!", HttpStatusCode.BadRequest));
    }

    public static async Task<IResult> UpdateUserCloak(
        HttpContext context,
        ISkinServiceManager skinServiceManager,
        IGmlManager gmlManager)
    {
        var login = context.Request.Form["Login"].FirstOrDefault();
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').FirstOrDefault();

        if (string.IsNullOrEmpty(login))
        {
            return Results.BadRequest(ResponseMessage.Create("Не заполнено обязательное поля \"Texture\"",
                HttpStatusCode.BadRequest));
        }

        if (await gmlManager.Users.GetUserByName(login) is not AuthUser user
            || string.IsNullOrEmpty(token)
            || string.IsNullOrEmpty(user.AccessToken)
            || !user.AccessToken.Equals(token))
        {
            return Results.NotFound(ResponseMessage.Create("Ошибка идентификации",
                HttpStatusCode.NotFound));
        }

        var texture = context.Request.Form.Files["Texture"]?.OpenReadStream();

        if (texture is null)
        {
            return Results.BadRequest(ResponseMessage.Create("Не заполнено обязательное поля \"Texture\"",
                HttpStatusCode.BadRequest));
        }

        return await skinServiceManager.UpdateCloak(user, texture)
            ? Results.Ok(ResponseMessage.Create("Плащ успешно установлен!", HttpStatusCode.OK))
            : Results.BadRequest(ResponseMessage.Create("Не удалось обновить плащ!", HttpStatusCode.BadRequest));
    }

    public static async Task<IResult> GetUserSkin(IGmlManager gmlManager, string textureGuid)
    {
        var user = await gmlManager.Users.GetUserBySkinGuid(textureGuid);

        if (user is null)
        {
            return Results.NotFound();
        }

        return Results.File(await gmlManager.Users.GetSkin(user));
    }

    public static async Task<IResult> GetUserCloak(IGmlManager gmlManager, string textureGuid)
    {
        var user = await gmlManager.Users.GetUserByCloakGuid(textureGuid);

        if (user is null)
        {
            return Results.NotFound();
        }

        return Results.File(await gmlManager.Users.GetCloak(user));
    }

    public static async Task<IResult> GetUserHead(IGmlManager gmlManager, string userUuid)
    {
        var user = await gmlManager.Users.GetUserByUuid(userUuid);

        if (user is null)
            return Results.NotFound(ResponseMessage.Create($"Пользователь с UUID: \"{userUuid}\" не найден",
                HttpStatusCode.NotFound));

        return Results.Stream(await gmlManager.Users.GetHead(user).ConfigureAwait(false));
    }
}
