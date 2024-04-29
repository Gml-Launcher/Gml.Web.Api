using System.IO.Compression;
using System.Net;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.LauncherDto;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class GitHubIntegrationHandler : IGitHubIntegrationHandler
{
    private const string LauncherGitHubUrl = "https://github.com/GamerVII-NET/Gml.Backend.git";

    public static async Task<IResult> GetVersions(IGitHubService gitHubService)
    {
        var versions = await gitHubService.GetRepositoryBranches("GamerVII-NET", "Gml.Launcher");

        var versionsDtos = versions.Select(c => new LauncherVersionReadDto
        {
            Version = c
        });

        return Results.Ok(ResponseMessage.Create(versionsDtos, "Список версий успешно получен", HttpStatusCode.OK));
    }

    public static async Task<IResult> DownloadLauncher(IGmlManager manager, IGitHubService gitHubService,
        CreateLauncherProject createLauncherDto)
    {
        var path = Path.Combine(manager.LauncherInfo.InstallationDirectory, "Launcher");

        var projectPath =
            await gitHubService.DownloadProject(path, createLauncherDto.GitHubVersions, LauncherGitHubUrl);

        return Results.Ok();
    }

    public static async Task<IResult> ReturnLauncherSolution(IGmlManager gmlManager, string version)
    {
        var projectPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "Launcher", version);

        if (!Directory.Exists(projectPath))
            return Results.BadRequest(ResponseMessage.Create("Проект не найден, сначала скачайте и соберите его",
                HttpStatusCode.BadRequest));

        var zipPath = Path.Combine(Path.GetTempPath(), $"Solution_Launcher_{DateTime.Now.Ticks}.zip");

        await Task.Run(() => ZipFile.CreateFromDirectory(projectPath, zipPath));

        var contentType = "application/zip";

        var downloadFileName = "gml-solution.zip";

        var fileBytes = await File.ReadAllBytesAsync(zipPath);

        return Results.File(fileBytes, contentType, downloadFileName);
    }
}
