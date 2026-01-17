using System.Text;
using Gml.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class EasyCabinetAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : IPlatformAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<AuthResult> Auth(string login, string password, string? totp = null)
    {
        var authService = (await gmlManager.Integrations.GetActiveAuthService())!.Endpoint;

        var baseUri = new Uri(authService);

        var endpoint = $"{baseUri.Scheme}://{baseUri.Host}/aurora/auth";
        // TODO add deserialize json response
        // https://github.com/ArslandTeam/EasyCabinet/blob/6e2c6760521d6dcd4e66b275b2d2adaab85e3012/packages/backend/src/api/aurora/service.rs#L16-L51
        var dto = JsonConvert.SerializeObject(new
        {
            login,
            password
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var result =
            await _httpClient.PostAsync(endpoint, content);

        return new AuthResult
        {
            Login = login,
            IsSuccess = result.IsSuccessStatusCode
        };
    }
}
