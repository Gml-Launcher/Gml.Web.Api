namespace Gml.Web.Api.Dto.Mods;

public class ModReadDto
{
    public string Name { get; set; }
}
public class ExtendedModReadDto : ModReadDto
{
    public string Description { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public int DownloadCount { get; set; }
    public int FollowsCount { get; set; }
}
