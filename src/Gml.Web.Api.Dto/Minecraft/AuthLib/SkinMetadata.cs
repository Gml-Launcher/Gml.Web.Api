using Newtonsoft.Json;

namespace Gml.Web.Api.Dto.Minecraft.AuthLib;

public class SkinMetadata
{
    [JsonProperty("model")]
    public string Model { get; set; }
}
