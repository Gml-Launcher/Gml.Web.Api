using System.Net.Http.Headers;
using Gml.Core.User;
using Gml.Web.Api.Core.Options;

namespace Gml.Web.Api.Core.Services;

public class SkinServiceManager(IHttpClientFactory httpClientFactory) : ISkinServiceManager
{
    private HttpClient _skinServiceClient = httpClientFactory.CreateClient(HttpClientNames.SkinService);

    public async Task<bool> UpdateSkin(AuthUser authUser, Stream texture)
    {
        var content = new MultipartFormDataContent();

        content.Add(new StreamContent(texture)
        {
            Headers =
            {
                ContentLength = texture.Length,
                ContentType = new MediaTypeHeaderValue("image/png")
            }
        }, "file", "skin.png"); // Pass the name of the form field, the file name, and the content type

        var request = await _skinServiceClient.PostAsync($"/skin/{authUser.Name}", content);

        return request.IsSuccessStatusCode;
    }
    public async Task<bool> UpdateCloak(AuthUser authUser, Stream texture)
    {
        var content = new MultipartFormDataContent();

        content.Add(new StreamContent(texture)
        {
            Headers =
            {
                ContentLength = texture.Length,
                ContentType = new MediaTypeHeaderValue("image/png")
            }
        }, "file", "skin.png"); // Pass the name of the form field, the file name, and the content type

        var request = await _skinServiceClient.PostAsync($"/cloak/{authUser.Name}", content);

        return request.IsSuccessStatusCode;
    }

}

public interface ISkinServiceManager
{
    Task<bool> UpdateSkin(AuthUser authUser, Stream texture);
    Task<bool> UpdateCloak(AuthUser authUser, Stream texture);
}
