using System.Reflection;

namespace Gml.Web.Api.Core.Services;

public sealed record BuildInfo(string Version, string Commit)
{
    public static BuildInfo Current { get; } = ReadFromAssembly();

    private static BuildInfo ReadFromAssembly()
    {
        var metadata = typeof(BuildInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);

        return new BuildInfo(
            metadata.GetValueOrDefault("BuildVersion") ?? "development",
            metadata.GetValueOrDefault("BuildCommit") ?? "unknown");
    }
}
