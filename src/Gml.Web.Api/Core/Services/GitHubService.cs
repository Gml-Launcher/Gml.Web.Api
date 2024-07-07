using System.IO.Compression;
using GmlCore.Interfaces;
using Newtonsoft.Json.Linq;

namespace Gml.Web.Api.Core.Services;

public class GitHubService : IGitHubService
{
    private readonly IGmlManager _gmlManager;
    private readonly HttpClient _httpClient;

    public GitHubService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    {
        _gmlManager = gmlManager;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<string>> GetRepositoryBranches(string user, string repository)
    {
        var url = $"https://api.github.com/repos/{user}/{repository}/branches";

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("request");

        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();

            var branches = JArray.Parse(responseString);

            return branches.Select(jt => jt["name"].ToString()).ToList();
        }

        return new List<string> { "main", "dev" };
    }

    public async Task<string> DownloadProject(string projectPath, string branchName, string repoUrl)
    {
        var httpClient = new HttpClient();

        var directory = new DirectoryInfo(projectPath);

        if (!directory.Exists) directory.Create();

        var zipPath = $"{projectPath}/{branchName}.zip";
        var extractPath = NormalizePath(projectPath, branchName);

        var url = $"https://github.com/Gml-Launcher/Gml.Launcher/archive/refs/heads/{branchName}.zip";

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            using (
                Stream contentStream = await (await httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                stream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                await contentStream.CopyToAsync(stream);
            }
        }

        // Проверяем, существует ли уже папка для распаковки
        if (!Directory.Exists(extractPath))
            // Если папка не существует - создаем ее
            Directory.CreateDirectory(extractPath);

        // Распаковываем архив
        ZipFile.ExtractToDirectory(zipPath, extractPath, true);

        File.Delete(zipPath);

        return new DirectoryInfo(extractPath).GetDirectories().First().FullName;
    }


    private string NormalizePath(string directory, string fileDirectory)
    {
        directory = directory
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        // .TrimStart(Path.DirectorySeparatorChar);

        fileDirectory = fileDirectory
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        return Path.Combine(directory, fileDirectory);
    }

    public async Task EditLauncherFiles(string projectPath, string host, string folder)
    {
        var keys = new Dictionary<string, string>
        {
            { "{{HOST}}", host },
            { "{{FOLDER_NAME}}", folder }
        };

        var settingsTemplateFile =
            Path.Combine(projectPath, "src", "Gml.Launcher", "Assets", "Resources",
                "ResourceKeysDictionary.Template.cs");

        var settingsFile =
            Path.Combine(projectPath, "src", "Gml.Launcher", "Assets", "Resources", "ResourceKeysDictionary.cs");

        if (File.Exists(settingsFile))
            File.Delete(settingsFile);

        var content = await File.ReadAllTextAsync(settingsTemplateFile);

        foreach (var key in keys) content = content.Replace(key.Key, key.Value);

        await File.WriteAllTextAsync(settingsFile, content);
    }
}
