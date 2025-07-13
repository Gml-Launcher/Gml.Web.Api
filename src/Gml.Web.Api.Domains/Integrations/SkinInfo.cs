using Newtonsoft.Json;

namespace Gml.Web.Api.Domains.Integrations;

public class SkinInfo
{
    [JsonProperty("slim")]
    public bool Slim { get; set; }
}
