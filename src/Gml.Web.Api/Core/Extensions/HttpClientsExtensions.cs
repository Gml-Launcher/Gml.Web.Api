using Gml.Web.Api.Core.Options;

namespace Gml.Web.Api.Core.Extensions;

public static class HttpClientsExtensions
{
    public static IServiceCollection AddNamedHttpClients(this IServiceCollection services)
    {
        string? skinsServiceUrl = Environment.GetEnvironmentVariable("SkinServiceUrl") ?? string.Empty; //ToDo: To Env

        var dockerEnv = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");

        var isRunningInDocker = !string.IsNullOrEmpty(dockerEnv) && dockerEnv.ToLower() == "true";

        services.AddHttpClient(HttpClientNames.SkinService, client =>
        {
            client.BaseAddress = isRunningInDocker
                ? new Uri("http://gml-web-skins:8085/")
                : new Uri(skinsServiceUrl);
        });

        return services;
    }

}
