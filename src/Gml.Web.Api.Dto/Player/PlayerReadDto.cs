using System;

namespace Gml.Web.Api.Dto.Player;

public class PlayerReadDto
{
    public string Name { get; set; } = null!;
    public string AccessToken { get; set; }
    public string Uuid { get; set; }
    public DateTime ExpiredDate { get; set; }
    public string TextureSkinUrl { get; set; }
    public string TextureCloakUrl { get; set; }
    public string TextureSkinGuid { get; set; }
    public string TextureCloakGuid { get; set; }
}
