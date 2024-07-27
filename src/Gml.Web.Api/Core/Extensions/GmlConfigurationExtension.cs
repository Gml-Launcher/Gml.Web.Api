using Gml.Core.Launcher;
using Gml.Web.Api.Domains.Launcher;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Extensions;

public static class GmlConfigurationExtension
{
    public static IServiceCollection ConfigureGmlManager(this IServiceCollection services, string projectName, string securityKey, string? projectPath)
    {
        services.AddSingleton<IGmlManager>(_ =>
        {
            var manager =
                new GmlManager(new GmlSettings(projectName, securityKey, projectPath));

            manager.RestoreSettings<LauncherVersion>();

            return manager;
        });

        return services;
    }
}
