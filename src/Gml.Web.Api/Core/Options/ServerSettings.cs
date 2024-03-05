using System.Reflection;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Options;

public class ServerSettings
{
    [JsonProperty(nameof(PolicyName))] public string PolicyName { get; set; } = "GmlPolicy";
    [JsonProperty(nameof(ProjectName))] public string ProjectName { get; set; } = "GmlServer";
    [JsonProperty(nameof(SecretKey))] public string SecretKey { get; set; } = "SecretGmlKey";
    public string ProjectVersion { get; set; } = null!;
    public string[] SkinDomains { get; set; } = [];

    [JsonProperty(nameof(ProjectDescription))]
    public string? ProjectDescription { get; set; }
}