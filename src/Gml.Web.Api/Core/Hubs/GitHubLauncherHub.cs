using System.Diagnostics;
using System.Runtime.InteropServices;
using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class GitHubLauncherHub(IGitHubService gitHubService, IGmlManager gmlManager) : BaseHub
{
    private const string _launcherGitHub = "https://github.com/Gml-Launcher/Gml.Launcher";

    public async Task Download(string branchName, string host, string folderName)
    {
        try
        {
            var projectPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "Launcher", branchName);

            if (Directory.Exists(projectPath))
            {
                await gmlManager.Notifications
                    .SendMessage("Лаунчер уже существует в папке, удалите его перед сборкой", NotificationType.Error);
                return;
            }

            projectPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "Launcher");

            ChangeProgress(nameof(GitHubLauncherHub), 5);
            var allowedVersions = await gitHubService
                .GetRepositoryTags("Gml-Launcher", "Gml.Launcher");

            if (allowedVersions.All(c => c != branchName))
            {
                await gmlManager.Notifications
                    .SendMessage($"Полученная версия лаунчера \"{branchName}\" не поддерживается", NotificationType.Error);
                return;
            }

            ChangeProgress(nameof(GitHubLauncherHub), 10);
            var newFolder = await gitHubService.DownloadProject(projectPath, branchName, _launcherGitHub);
            ChangeProgress(nameof(GitHubLauncherHub), 20);

            await gitHubService.EditLauncherFiles(newFolder, host, folderName);
            ChangeProgress(nameof(GitHubLauncherHub), 30);

            ChangeProgress(nameof(GitHubLauncherHub), 100);
            SendCallerMessage($"Проект \"{branchName}\" успешно создан");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            await gmlManager.Notifications.SendMessage("Ошибка при загрузке клиента лаунчера", exception);
        }
        finally
        {
            await Clients.Caller.SendAsync("LauncherDownloadEnded");
        }
    }

    public async Task Compile(string version, string[] osTypes)
    {
        try
        {
            if (!gmlManager.Launcher.CanCompile(version, out string message))
            {
                SendCallerMessage(message);
                return;
            }

            Log("Start compilling...");

            if (await gmlManager.LauncherInfo.Settings.SystemProcedures.InstallDotnet())
            {
                var eventObservable = gmlManager.Launcher.BuildLogs.Subscribe(Log);

                var result = await gmlManager.Launcher.Build(version, osTypes);

                eventObservable.Dispose();

                if (result)
                    await gmlManager.Notifications.SendMessage("Лаунчер успешно скомпилирован!", NotificationType.Info);
                else
                    await gmlManager.Notifications.SendMessage("Сборка лаунчера завершилась ошибкой!", NotificationType.Error);

            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            await gmlManager.Notifications.SendMessage("Ошибка при загрузке клиента лаунчера", exception);
        }
        finally
        {
            await Clients.Caller.SendAsync("LauncherBuildEnded");
        }
    }
}
