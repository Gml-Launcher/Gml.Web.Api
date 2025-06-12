using System.Diagnostics.Tracing;
using System.IO.Compression;
using System.Net;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.Plugins;
using Gml.Web.Api.Dto.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Gml.Web.Api.Core.Handlers;

public abstract class PluginHandler : IPluginHandler
{
    public static Task<IResult> RemovePlugin(string name, string version)
    {
        var pluginPath = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

        var file = pluginPath.GetFiles($"{name}.dll", SearchOption.AllDirectories)
            .FirstOrDefault(c => c.Directory!.Name == version);

        if (file?.Exists == true)
        {
            try
            {
                file.Delete();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return Task.FromResult(Results.BadRequest(ResponseMessage.Create($"Произошла ошибка при удалении. Плагин не был удален.", HttpStatusCode.BadRequest)));
            }
        }

        return Task.FromResult(Results.Ok(ResponseMessage.Create("Плагин успешно удален", HttpStatusCode.OK)));
    }

    public static Task<IResult> GetInstalledPlugins()
    {
        var pluginsDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

        var plugins = pluginsDirectory.GetFiles("*.dll", SearchOption.AllDirectories);

        var pluginsDto = plugins.Select(c => new PluginVersionReadDto
        {
            Name = c.Name.Replace(Path.GetExtension(c.Name), string.Empty),
            Version = c.Directory!.Name
        });

        return Task.FromResult(Results.Ok(ResponseMessage.Create(pluginsDto, string.Empty, HttpStatusCode.OK)));

    }

    public static async Task<IResult> InstallPlugin(HttpContext context, RecloudPluginCreateDto plugin, PluginsService pluginsService)
    {
        var token = context.Request.Headers["recloud_id_token"].ToString();

        if (string.IsNullOrEmpty(token))
        {
            return Results.BadRequest(ResponseMessage.Create("Не указан системный токен RecloudID", HttpStatusCode.BadRequest));
        }

        var canInstall = await pluginsService.CanInstall(token, plugin.Id);

        if (!canInstall)
            return Results.Ok(ResponseMessage.Create("У вас недостаточно прав для установки данного расширения",
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
