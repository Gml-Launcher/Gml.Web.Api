using Newtonsoft.Json;

namespace Gml.Web.Api.Dto.Minecraft.AuthLib;

public class Textures
{
    [JsonProperty("SKIN", NullValueHandling = NullValueHandling.Ignore)] public SkinCape Skin { get; set; }

    [JsonProperty("CAPE", NullValueHandling = NullValueHandling.Ignore)] public SkinCape Cape { get; set; }
}
