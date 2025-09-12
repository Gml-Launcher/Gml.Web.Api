using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Options;

public class ServerSettings
{
    [JsonProperty(nameof(PolicyName))] public string PolicyName { get; set; } = "GmlPolicy";
    [JsonProperty(nameof(ProjectName))] public string ProjectName { get; set; } = "GmlServer";
    [JsonProperty(nameof(SecurityKey))] public string SecurityKey { get; set; } = "SecretGmlKey";
    public string JwtIssuer { get; set; } = "gml-api";
    public string JwtAudience { get; set; } = "gml-clients";
    public int AccessTokenMinutes { get; set; } = 1;
    public int RefreshTokenDays { get; set; } = 30;
    public string ProjectVersion { get; set; } = null!;
    public string[] SkinDomains { get; set; } = [];

    [JsonProperty(nameof(ProjectDescription))]
    public string? ProjectDescription { get; set; }

    public string? ProjectPath { get; set; }
    public string? TextureEndpoint { get; set; }
    public string MarketEndpoint { get; set; } = "https://gml-market.recloud.tech";
    public bool IsEnabledApiDocs { get; set; }
}
