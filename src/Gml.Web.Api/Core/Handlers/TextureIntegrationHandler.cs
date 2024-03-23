using System.Net;
using FluentValidation;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Texture;
using GmlCore.Interfaces;

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
}
