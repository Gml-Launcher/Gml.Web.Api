using System.Net;
using FluentValidation;
using Gml.Web.Api.Core.Messages;
using Gml.Web.Api.Core.Validation;
using Gml.Web.Api.Domains.Texture;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Gml.Web.Api.Core.Handlers;

public class TextureIntegrationHandler : ITextureIntegrationHandler
{
    public static async Task<IResult> GetSkinUrl(IGmlManager gmlManager)
    {
        try
        {
            var url = await gmlManager.Integrations.GetSkinServiceAsync();

            return Results.Ok(ResponseMessage.Create(new TextureServiceDto(url), "Успешно", HttpStatusCode.OK));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> SetSkinUrl(
        IGmlManager gmlManager,
        IValidator<TextureServiceDto> validator,
        TextureServiceDto textureDto)
    {
        var result = await validator.ValidateAsync(textureDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        await gmlManager.Integrations.SetSkinServiceAsync(textureDto.Url);

        return Results.Ok(ResponseMessage.Create("Сервис скинов успешно обновлен", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetCloakUrl(IGmlManager gmlManager)
    {
        try
        {
            var url = await gmlManager.Integrations.GetCloakServiceAsync();

            return Results.Ok(ResponseMessage.Create(new TextureServiceDto(url), "Успешно", HttpStatusCode.OK));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> SetCloakUrl(
        IGmlManager gmlManager,
        IValidator<TextureServiceDto> validator,
        TextureServiceDto textureDto)
    {
        var result = await validator.ValidateAsync(textureDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        await gmlManager.Integrations.SetCloakServiceAsync(textureDto.Url);

        return Results.Ok(ResponseMessage.Create("Сервис плащей успешно обновлен", HttpStatusCode.OK));
    }
}
