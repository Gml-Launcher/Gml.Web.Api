using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Dto.Messages;
using Gml.Dto.Texture;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class SentryErrorSaveHandler : IErrorSaveHandler
{
    public static async Task<IResult> GetDsnUrl(HttpContext context, IGmlManager gmlManager)
    {
        var hostValue = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? context.Request.Host.Value;
        var serviceUrl = await gmlManager.Integrations.GetSentryService();

        if (serviceUrl is not null)
            return Results.Ok(ResponseMessage.Create(new UrlServiceDto(serviceUrl), "Успешно", HttpStatusCode.OK));

        serviceUrl = $"https://{hostValue}/api/sentry/dsn";
        await gmlManager.Integrations.SetSentryService(serviceUrl);

        return Results.Ok(ResponseMessage.Create(new UrlServiceDto(serviceUrl), "Успешно", HttpStatusCode.OK));
    }

    public static async Task<IResult> UpdateDsnUrl(HttpContext context, IGmlManager gmlManager, IMapper mapper,
        IValidator<UrlServiceDto> validator,
        UrlServiceDto urlDto)
    {
        var result = await validator.ValidateAsync(urlDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        await gmlManager.Integrations.SetSentryService(urlDto.Url);

        return Results.Ok(ResponseMessage.Create("Сервис Sentry успешно обновлен", HttpStatusCode.OK));
    }
}
