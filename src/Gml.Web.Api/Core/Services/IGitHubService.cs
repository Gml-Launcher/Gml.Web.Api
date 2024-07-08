namespace Gml.Web.Api.Core.Services;

public interface IGitHubService
{
    Task<IEnumerable<string>> GetRepositoryBranches(string user, string repository);
    Task<IEnumerable<string>> GetRepositoryTags(string user, string repository);
    Task<string> DownloadProject(string projectPath, string branchName, string repoUrl);
    Task EditLauncherFiles(string projectPath, string host, string folder);
}
