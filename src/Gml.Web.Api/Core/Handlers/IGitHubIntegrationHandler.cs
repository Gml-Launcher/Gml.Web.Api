using FluentValidation;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Launcher;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface IGitHubIntegrationHandler
{
    static abstract Task<IResult> GetVersions(IGitHubService gitHubService);

    static abstract Task<IResult> DownloadLauncher(
        IGmlManager manager,
        IGitHubService gitHubService,
        IValidator<LauncherCreateDto> launcherValidator,
        LauncherCreateDto launcherCreateDto);

    static abstract Task<IResult> ReturnLauncherSolution(IGmlManager gmlManager, string branchName);
}
