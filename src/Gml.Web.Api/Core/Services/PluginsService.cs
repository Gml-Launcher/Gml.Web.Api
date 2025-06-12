using System.Net.Http.Headers;
using Gml.Web.Api.Core.Options;
using Microsoft.Extensions.Primitives;

namespace Gml.Web.Api.Core.Services;

public class PluginsService
{
    private readonly PluginAssemblyManager _pluginsManager;
    private HttpClient _httpClient;

    private DirectoryInfo _pluginsDirectory;

    public PluginsService(IHttpClientFactory httpClientFactory, PluginAssemblyManager pluginsManager)
    {
        _pluginsManager = pluginsManager;
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.MarketService);

        var rootDirectory = Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory;

        _pluginsDirectory = new(Path.Combine(Path.GetDirectoryName(rootDirectory)!, "plugins"));
    }

    public async Task<bool> CanInstall(string recloudToken, Guid pluginId)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recloudToken);

        var data = await _httpClient.GetAsync($"/api/v1/marketplace/products/{pluginId}/check");

        return data.IsSuccessStatusCode;
    }

    public async Task Install(string recloudToken, Guid pluginId)
    {
        if (!_pluginsDirectory.Exists)
            _pluginsDirectory.Create();

        var pluginDirectory = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, pluginId.ToString()));
        var assemblyDirectory = new DirectoryInfo(Path.Combine(pluginDirectory.FullName, "backend"));

        if (!pluginDirectory.Exists)
            pluginDirectory.Create();

        var dlls = assemblyDirectory.GetFiles("*.dll");

        foreach (var dll in dlls)
        {
            _pluginsManager.LoadPlugin(dll.FullName);
        }
    }
}
