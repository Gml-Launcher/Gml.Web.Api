using System.IO.Compression;
using System.Net;
using FluentValidation;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.LauncherDto;
using Gml.Web.Api.Dto.Launcher;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class GitHubIntegrationHandler : IGitHubIntegrationHandler
{
    private const string LauncherGitHubUrl = "https://github.com/Gml-Launcher/Gml.Backend.git";

    public static async Task<IResult> GetVersions(IGitHubService gitHubService)
    {
        var versions = await gitHubService.GetRepositoryTags("Gml-Launcher", "Gml.Launcher");

        var versionsDtos = versions.Select(c => new LauncherVersionReadDto
        {
            Version = c
        });

        return Results.Ok(ResponseMessage.Create(versionsDtos, "Список версий успешно получен", HttpStatusCode.OK));
    }

    public static async Task<IResult> DownloadLauncher(
        IGmlManager manager,
        IGitHubService gitHubService,
        IValidator<LauncherCreateDto> validator,
        LauncherCreateDto launcherCreateDto)
    {
        var result = await validator.ValidateAsync(launcherCreateDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var path = Path.Combine(manager.LauncherInfo.InstallationDirectory, "Launcher");

        var projectPath =
            await gitHubService.DownloadProject(path, launcherCreateDto.GitHubVersions, LauncherGitHubUrl);

        await gitHubService.EditLauncherFiles(projectPath, launcherCreateDto.Host, launcherCreateDto.Folder);

        return await ReturnLauncherSolution(manager, launcherCreateDto.GitHubVersions);
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
