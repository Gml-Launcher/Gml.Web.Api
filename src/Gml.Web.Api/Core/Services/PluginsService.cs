using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.Http.Headers;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Dto.Marketplace;
using Gml.Web.Api.Dto.Messages;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Services;

public class PluginsService
{
    private readonly PluginAssemblyManager _pluginsManager;
    private HttpClient _httpClient;

    private DirectoryInfo _pluginsDirectory;
    private ConcurrentDictionary<string, ProductReadDto> _products = new();
    public IReadOnlyDictionary<string, ProductReadDto> Products => _products.AsReadOnly();

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

        return data.IsSuccessStatusCode && !_products.ContainsKey(pluginId.ToString());
    }

    public async Task Install(string recloudToken, Guid pluginId)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recloudToken);

        if (!_pluginsDirectory.Exists)
            _pluginsDirectory.Create();

        var pluginDirectory = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, pluginId.ToString()));
        var assemblyDirectory = new DirectoryInfo(Path.Combine(pluginDirectory.FullName, "backend"));

        if (!pluginDirectory.Exists)
            pluginDirectory.Create();

        if (!assemblyDirectory.Exists)
            assemblyDirectory.Create();

        var pluginInfo = await _httpClient.GetAsync($"/api/v1/marketplace/products/{pluginId}");
        var pluginFile = await _httpClient.GetStreamAsync($"/api/v1/marketplace/products/{pluginId}/download");

        var content = await pluginInfo.Content.ReadAsStringAsync();
        var product = JsonConvert.DeserializeObject<ResponseMessage<ProductReadDto>>(content);

        if (product?.Data != null)
        {
            _products.TryAdd(product.Data.Id.ToString(), product.Data);
            await File.WriteAllTextAsync(Path.Combine(pluginDirectory.FullName, "product.json"), JsonConvert.SerializeObject(product.Data, Formatting.Indented));
        }

        var pluginZipPath = Path.Combine(pluginDirectory.FullName, "plugin.zip");

        await using (var fileStream = File.Create(pluginZipPath))
        {
            await pluginFile.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(pluginZipPath, pluginDirectory.FullName, true);

        File.Delete(pluginZipPath);

        var dlls = assemblyDirectory.GetFiles("*.dll");

        foreach (var dll in dlls)
        {
            _pluginsManager.LoadPlugin(dll.FullName);
        }
    }

    public async Task<bool> RemovePlugin(Guid id)
    {
        _products.TryRemove(id.ToString(), out _);
        var pluginDirectory = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, id.ToString()));

        // Создаем временную директорию для бэкапа
        var backupDir = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, $"{id}_backup"));
        var jsonFiles = pluginDirectory.Exists
            ? pluginDirectory.GetFiles("product.json", SearchOption.AllDirectories)
            : [];

        // Копируем product.json файлы во временную директорию
        if (jsonFiles.Length > 0 && !backupDir.Exists)
        {
            backupDir.Create();
            foreach(var file in jsonFiles)
            {
                file.CopyTo(Path.Combine(backupDir.FullName, file.Name));
            }
        }

        // Находим файлы только в директории плагина, а не во всех плагинах
        var dlls = pluginDirectory.Exists
            ? pluginDirectory.GetFiles("*.dll", SearchOption.AllDirectories)
            : [];

        // Выгружаем каждую сборку плагина
        foreach (var dll in dlls)
        {
            _pluginsManager.UnloadPlugin(dll.FullName);
        }

        // Даем немного времени на выгрузку сборок
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        bool deleteFailed = false;
        // Удаляем директорию плагина только после выгрузки сборок
        if (pluginDirectory.Exists)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    pluginDirectory.Delete(true);
                    deleteFailed = false;
                    break;
                }
                catch (IOException ex) when (ex.Message.Contains("используется") || ex.Message.Contains("being used"))
                {
                    // Если файлы все еще заблокированы, ждем немного и пробуем снова
                    await Task.Delay(500);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    if (i == 9)
                    {
                        deleteFailed = true;
                        throw;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Если нет прав доступа, делаем еще одну попытку после сборки мусора
                    await Task.Delay(100);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    if (i == 9)
                    {
                        deleteFailed = true;
                    }
                }
            }
        }

        // Восстанавливаем из бэкапа если удаление не удалось
        if (deleteFailed && backupDir.Exists)
        {
            if (!pluginDirectory.Exists)
                pluginDirectory.Create();

            foreach(var file in backupDir.GetFiles())
            {
                file.CopyTo(Path.Combine(pluginDirectory.FullName, file.Name), true);
            }
        }

        // Удаляем временную директорию
        if (backupDir.Exists)
            backupDir.Delete(true);

        return !deleteFailed;
    }

    public void RestorePlugins()
    {
        try
        {

            if (!_pluginsDirectory.Exists)
                _pluginsDirectory.Create();

            var dlls = _pluginsDirectory.GetFiles("*.dll", SearchOption.AllDirectories);
            var jsonFiles = _pluginsDirectory.GetFiles("product.json", SearchOption.AllDirectories);

            var pluginConfigs = jsonFiles
                .Select(file => JsonConvert.DeserializeObject<ProductReadDto>(File.ReadAllText(file.FullName))!);

            foreach (var pluginConfig in pluginConfigs)
            {
                _products.TryAdd(pluginConfig.Id.ToString(), pluginConfig);
            }

            foreach (var dll in dlls)
            {
                _pluginsManager.LoadPlugin(dll.FullName);
            }

            Console.WriteLine($"{dlls.Length} plugins installed");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine(exception);
        }
    }

    public Stream? GetFrontendPluginContent(ProductReadDto plugin)
    {

        var pluginDirectory = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, plugin.Id.ToString()));
        var frontendFile = new FileInfo(Path.Combine(pluginDirectory.FullName, "frontend", "main.js"));

        return frontendFile.Exists
            ? File.OpenRead(Path.Combine(pluginDirectory.FullName, "frontend", "main.js"))
            : null;
    }

    public enum PluginPlace
    {
        AfterLoginForm = 0
    }

    public async Task<IEnumerable<string>> GetPlugins(PluginPlace place)
    {
        var fileName = place + ".js";

        return _products.Values.Where(plugin =>
        {
            var pluginDirectory = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, plugin.Id.ToString()));
            var frontendFile = new FileInfo(Path.Combine(pluginDirectory.FullName, "frontend", fileName));
            return frontendFile.Exists;
        }).Select(plugin =>
        {
            var pluginDirectory = new DirectoryInfo(Path.Combine(_pluginsDirectory.FullName, plugin.Id.ToString()));
            var frontendFile = new FileInfo(Path.Combine(pluginDirectory.FullName, "frontend", fileName));
            return File.ReadAllText(frontendFile.FullName);
        });
    }
}
