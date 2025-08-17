using System.Collections.Frozen;
using System.Diagnostics.Tracing;
using System.IO.Compression;
using System.Net;
using System.Text;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.Plugins;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Gml.Web.Api.Core.Handlers;

public abstract class PluginHandler : IPluginHandler
{
    public static async Task<IResult> RemovePlugin(Guid id, PluginsService pluginsService, IGmlManager manager)
    {
        try
        {
            if (await pluginsService.RemovePlugin(id))
            {
                return Results.Ok(ResponseMessage.Create("Плагин успешно удален", HttpStatusCode.OK));

            }

            var message = "Не удалось удалить расширение, необходимо выполнить принудительный перезапуск Gml.";
            await manager.Notifications.SendMessage(message, NotificationType.Fatal);
            return Results.BadRequest(ResponseMessage.Create(message, HttpStatusCode.OK));

        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.OK));
        }
    }

    public static Task<IResult> GetInstalledPlugins(PluginsService pluginsService)
    {
        var plugins = pluginsService.Products;

        return Task.FromResult(Results.Ok(ResponseMessage.Create(plugins.Values, string.Empty, HttpStatusCode.OK)));
    }

    public static Task<IResult> GetPluginScript(PluginsService pluginsService, IGmlManager manager, Guid id)
    {
        try
        {
            var plugin = pluginsService.Products.Values.FirstOrDefault(c => c.Id == id);

            if (plugin == null)
            {
                return Task.FromResult(Results.NotFound());
            }

            var stream = pluginsService.GetFrontendPluginContent(plugin);

            if (stream is null)
            {
                return Task.FromResult(Results.NotFound());
            }

            var result = Results.File(stream, "text/javascript", "main.js");

            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            manager.BugTracker.CaptureException(exception );
            return Task.FromResult(Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.OK)));
        }
    }
    public static async Task<IResult> GetPluginByPlaceScript(PluginsService pluginsService, IGmlManager manager, PluginsService.PluginPlace place)
    {
        try
        {
            var plugins = (await pluginsService.GetPlugins(place)).ToFrozenSet();

            if (plugins.Count == 0)
            {
                return Results.NotFound();
            }

            var content = string.Join("\n", plugins);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            var result = Results.File(stream, "text/javascript", "main.js");

            return result;
        }
        catch (Exception exception)
        {
            manager.BugTracker.CaptureException(exception );
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.OK));
        }
    }

    public static async Task<IResult> InstallPlugin(HttpContext context, RecloudPluginCreateDto plugin,
        PluginsService pluginsService)
    {
        var token = context.Request.Headers["recloud-id-token"].ToString();

        if (string.IsNullOrEmpty(token))
        {
            return Results.BadRequest(ResponseMessage.Create("Не указан системный токен RecloudID",
                HttpStatusCode.BadRequest));
        }

        var canInstall = await pluginsService.CanInstall(token, plugin.Id);

        if (!canInstall)
            return Results.BadRequest(ResponseMessage.Create(
                "У вас недостаточно прав для установки данного расширения или плагин уже установлен",
                HttpStatusCode.OK));

        await pluginsService.Install(token, plugin.Id);

        return Results.Ok(ResponseMessage.Create("Плагин успешно установлен", HttpStatusCode.OK));
    }

    private static void ExtractPlugin(string pluginsDirectory, string zipPath)
    {
        var pluginNameAndVersion = Path.GetFileNameWithoutExtension(zipPath).Substring("plugin-".Length);

        var versionStartIndex = pluginNameAndVersion.IndexOf("-v", StringComparison.Ordinal) + 2;
        var pluginName = pluginNameAndVersion.Substring(0, versionStartIndex - 2);
        var pluginVersion = pluginNameAndVersion.Substring(versionStartIndex);

        var extractPath = Path.Combine(pluginsDirectory, pluginName, $"v{pluginVersion}");

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        ZipFile.ExtractToDirectory(zipPath, extractPath, true);
        File.Delete(zipPath);
    }
}
