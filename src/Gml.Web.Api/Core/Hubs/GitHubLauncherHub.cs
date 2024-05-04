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
}
