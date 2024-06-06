using System.Net;
using Gml.Web.Api.Domains.Launcher;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class LauncherUpdateHandler : ILauncherUpdateHandler
{
    public static IResult GetActualVersion(IGmlManager gmlManager)
    {
        return Results.Ok(ResponseMessage.Create(gmlManager.LauncherInfo.ActualLauncherVersion, string.Empty, HttpStatusCode.OK));
    }

    public static async Task<IResult> UploadLauncherVersion(string osType, HttpContext context, IGmlManager gmlManager)
    {
        if (!Enum.TryParse<OsType>(osType, out var osTypeEnum))
        {
            return Results.BadRequest(ResponseMessage.Create($"Не удалось определить тип операционной системы", HttpStatusCode.InternalServerError));
        }

        var minecraftVersion = new LauncherVersion
        {
            Version = context.Request.Form["Version"].FirstOrDefault() ?? string.Empty,
            Title = context.Request.Form["Title"].FirstOrDefault() ?? string.Empty,
            Description = context.Request.Form["Description"].FirstOrDefault() ?? string.Empty,
            OsType = osTypeEnum,
            File = context.Request.Form.Files["File"] is null
                ? null
                : context.Request.Form.Files["File"]!.OpenReadStream()
        };

        try
        {
            var versions = await gmlManager.Launcher.CreateVersion(minecraftVersion, osTypeEnum);

            return Results.Ok(ResponseMessage.Create("Версия лаунчера успешно обновлена!", HttpStatusCode.OK));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Results.BadRequest(ResponseMessage.Create($"Не удалось обновить файл лаунчера: {e.Message}", HttpStatusCode.InternalServerError));
        }
    }
}
