using System.Net;
using AutoMapper;
using Gml.Web.Api.Domains.Launcher;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Launcher;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Launcher;

namespace Gml.Web.Api.Core.Handlers;

public class LauncherUpdateHandler : ILauncherUpdateHandler
{
    public static IResult GetActualVersion(IGmlManager gmlManager)
    {
        return Results.Ok(ResponseMessage.Create(gmlManager.LauncherInfo.ActualLauncherVersion, string.Empty, HttpStatusCode.OK));
    }

    public static async Task<IResult> GetBuilds(IGmlManager gmlManager, IMapper mapper)
    {
        var builds = await gmlManager.LauncherInfo.GetBuilds();

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<LauncherBuildReadDto>>(builds), string.Empty, HttpStatusCode.OK));
    }
    public static async Task<IResult> GetPlatforms(IGmlManager gmlManager)
    {
        var platforms = await gmlManager.Launcher.GetPlatforms();

        return Results.Ok(ResponseMessage.Create(platforms, string.Empty, HttpStatusCode.OK));
    }

    public static async Task<IResult> UploadLauncherVersion(HttpContext context, IGmlManager gmlManager)
    {
        var buildName = context.Request.Form["LauncherBuild"].ToString();

        if (string.IsNullOrEmpty(buildName))
        {
            return Results.BadRequest(ResponseMessage.Create($"Не удалось определить версию сборки", HttpStatusCode.BadRequest));
        }

        var build = await gmlManager.LauncherInfo.GetBuild(buildName);

        if (build is null)
        {
            return Results.BadRequest(ResponseMessage.Create($"Указанная версия не найдена", HttpStatusCode.BadRequest));
        }

        var minecraftVersion = new LauncherVersion
        {
            Version = context.Request.Form["Version"].FirstOrDefault() ?? string.Empty,
            Title = context.Request.Form["Title"].FirstOrDefault() ?? string.Empty,
            Description = context.Request.Form["Description"].FirstOrDefault() ?? string.Empty
        };

        try
        {
            var versions = await gmlManager.Launcher.CreateVersion(minecraftVersion, build);

            return Results.Ok(ResponseMessage.Create("Версия лаунчера успешно обновлена!", HttpStatusCode.OK));
        }
        catch (Exception exception)
        {
            await gmlManager.Notifications.SendMessage("Ошибка при публикации новой версии лаунчера", exception);
            return Results.BadRequest(ResponseMessage.Create($"Не удалось обновить файл лаунчера: {exception.Message}", HttpStatusCode.InternalServerError));
        }
    }
}
