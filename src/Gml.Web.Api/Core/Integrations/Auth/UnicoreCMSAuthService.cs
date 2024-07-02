using System.Text;
using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class UnicoreCMSAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : IPlatformAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<AuthResult> Auth(string login, string password)
    {
        var authService = (await gmlManager.Integrations.GetActiveAuthService())!.Endpoint;

        var baseUri = new Uri(authService);

        var endpoint = $"{baseUri.Scheme}://{baseUri.Host}/auth/login";

        var dto = JsonConvert.SerializeObject(new
        {
            username_or_email = login,
            password,
            totp = string.Empty,
            save_me = string.Empty
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var result =
            await _httpClient.PostAsync(endpoint, content);

        var data = await result.Content.ReadAsStringAsync();

        var jData = JObject.Parse(data);

        return new AuthResult
        {
            Login = login,
            IsSuccess = result.IsSuccessStatusCode,
            Uuid = jData["user"]?["uuid"]?.ToString()
        };
    }
}
