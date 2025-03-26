using Gml.Core.Launcher;
using Gml.Web.Api.Domains.Launcher;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Extensions;

public static class GmlConfigurationExtension
{
    public static IServiceCollection ConfigureGmlManager(
        this IServiceCollection services,
        string projectName,
        string securityKey,
        string? projectPath,
        string? textureEndpoint)
    {
        services.AddSingleton<IGmlManager>(_ =>
        {
            var settings = new GmlSettings(projectName, securityKey, projectPath)
            {
                TextureServiceEndpoint = textureEndpoint ?? "http://gml-web-skins:8085"
            };

            var manager = new GmlManager(settings);

            manager.RestoreSettings<LauncherVersion>();

            return manager;
        });

        return services;
    }
}
