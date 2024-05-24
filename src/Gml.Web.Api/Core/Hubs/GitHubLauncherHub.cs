using System.Diagnostics;
using System.Runtime.InteropServices;
using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs;

public class GitHubLauncherHub(IGitHubService gitHubService, IGmlManager gmlManager) : BaseHub
{
    private const string _launcherGitHub = "https://github.com/GamerVII-NET/Gml.Launcher";

    public async Task Download(string branchName, string host, string folderName)
    {
        var projectPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "Launcher", branchName);

        if (Directory.Exists(projectPath))
        {
            SendCallerMessage("Лаунчер уже существует в папке, удалите его перед сборкой");
            return;
        }

        projectPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "Launcher");

        ChangeProgress(nameof(GitHubLauncherHub), 5);
        var allowedVersions = await gitHubService.GetRepositoryBranches("GamerVII-NET", "Gml.Launcher");

        if (allowedVersions.All(c => c != branchName))
        {
            SendCallerMessage($"Полученная версия лаунчера \"{branchName}\" не поддерживается");
            return;
        }

        ChangeProgress(nameof(GitHubLauncherHub), 10);
        var newFolder = await gitHubService.DownloadProject(projectPath, branchName, _launcherGitHub);
        ChangeProgress(nameof(GitHubLauncherHub), 20);

        await gitHubService.EditLauncherFiles(newFolder, host, folderName);
        ChangeProgress(nameof(GitHubLauncherHub), 30);

        ChangeProgress(nameof(GitHubLauncherHub), 100);
        SendCallerMessage("Проект успешно создан");
    }

    public async Task Compile(string branchName)
    {
        var allowedVersions = new List<string>
        {
            "win-x86",
            "win-x64",
            "linux-x64"
        };

        var projectPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "Launcher", branchName);
        var launcherDirectory = new DirectoryInfo(Path.Combine(projectPath, "src", "Gml.Launcher"));

        if (!Directory.Exists(projectPath))
        {
            SendCallerMessage("Нет исходников для фомирования бинарных файлов!");
            return;
        }

        var buildFolder = await CreateBuilds(allowedVersions, projectPath, launcherDirectory);


    }

    private async Task<object> CreateBuilds(List<string> allowedVersions, string projectPath, DirectoryInfo launcherDirectory)
    {
        foreach (var version in allowedVersions)
        {
            ProcessStartInfo? processStartInfo = default;
            var command = string.Empty;

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = $"/c dotnet publish ./src/Gml.Launcher/ -r {version} -p:PublishSingleFile=true --self-contained false -p:IncludeNativeLibrariesForSelfExtract=true";
                processStartInfo = new ProcessStartInfo("cmd", command)
                {
                    WorkingDirectory = projectPath
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                command = $"dotnet publish ./src/Gml.Launcher/ -r {version} -p:PublishSingleFile=true --self-contained false -p:IncludeNativeLibrariesForSelfExtract=true";
                processStartInfo = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"")
                {
                    WorkingDirectory = projectPath
                };
            }

            if (processStartInfo != null)
            {
                var process = new Process
                {
                    StartInfo = processStartInfo
                };

                process.Start();
                await process.WaitForExitAsync();
            }
        }

        var publishDirectory = launcherDirectory.GetDirectories("publish", SearchOption.AllDirectories);

        var buildsFolder = new DirectoryInfo(Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "builds",
            $"build-{DateTime.Now:dd-MM-yyyy HH-mm-ss}"));

        if (!buildsFolder.Exists)
        {
            buildsFolder.Create();
        }

        foreach (DirectoryInfo dir in publishDirectory)
        {
            var newFolder = new DirectoryInfo(Path.Combine(buildsFolder.FullName, dir.Parent.Name));
            if (!newFolder.Exists)
            {
                newFolder.Create();
            }

            CopyDirectory(dir, newFolder);
        }

        return buildsFolder.FullName;
    }


    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        if (!destination.Exists)
        {
            destination.Create();
        }

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destination.FullName, file.Name), true);
        }

        foreach (DirectoryInfo subDir in source.GetDirectories())
        {
            CopyDirectory(subDir, new DirectoryInfo(Path.Combine(destination.FullName, subDir.Name)));
        }
    }
}
