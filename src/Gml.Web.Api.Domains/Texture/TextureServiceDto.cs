namespace Gml.Web.Api.Domains.Texture;

public class TextureServiceDto
{
    public TextureServiceDto(string url)
    {
        Url = url;
    }

    public string Url { get; set; }
}
