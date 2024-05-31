using System.IO.Compression;
using System.Net;
using Gml.Web.Api.Domains.Plugins;
using Gml.Web.Api.Dto.Messages;
using Microsoft.AspNetCore.Authorization;

namespace Gml.Web.Api.Core.Handlers;

public abstract class PluginHandler : IPluginHandler
{
    public static Task<IResult> GetInstalledPlugins()
    {
        var pluginsDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

        var plugins = pluginsDirectory.GetFiles("*.dll", SearchOption.AllDirectories);

        var pluginsDto = plugins.Select(c => new InstalledPluginReadDto
        {
            Name = c.Name.Replace(Path.GetExtension(c.Name), string.Empty),
            Version = c.Directory!.Name
        });

        return Task.FromResult(Results.Ok(ResponseMessage.Create(pluginsDto, string.Empty, HttpStatusCode.OK)));

    }

    public static async Task<IResult> InstallPlugin(HttpContext context)
    {

        var pluginFormData = new
        {
            Url = context.Request.Form["pluginUrl"]
        };

        if (string.IsNullOrEmpty(pluginFormData.Url))
        {
            return Results.BadRequest(ResponseMessage.Create("Не указан адрес плагина", HttpStatusCode.BadRequest));
        }

        var pluginsDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

        if (!pluginsDirectory.Exists)
        {
            pluginsDirectory.Create();
        }

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(pluginFormData.Url);

            var contentDisposition = response.Content.Headers.ContentDisposition;
            string? fileName = contentDisposition?.FileName?.Trim('\"');

            if (string.IsNullOrEmpty(fileName))
            {
                return Results.BadRequest(ResponseMessage.Create("Именование плагина имело неверный формат", HttpStatusCode.BadRequest));
            }

            var pluginPath = Path.Combine(pluginsDirectory.FullName, fileName);

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (Stream fileStream = new FileStream(pluginPath, FileMode.Create,
                       FileAccess.Write, FileShare.None, 8192, true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            ExtractPlugin(pluginsDirectory.FullName, pluginPath);

        }


        return Results.Empty;
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
