using System.Text;
using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class UndefinedAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager) : IPlatformAuthService
{
    private readonly IGmlManager _gmlManager = gmlManager;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<AuthResult> Auth(string login, string password, string? totp = null)
    {
        var activeAuthService = await _gmlManager.Integrations.GetActiveAuthService();

        if (activeAuthService == null) throw new Exception("Сервис авторизации не настроен или настроен неправильно");

        var dto = JsonConvert.SerializeObject(new
        {
            Login = login,
            Password = password,
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var result = await _httpClient.PostAsync(activeAuthService.Endpoint, content);

        return new AuthResult
        {
            Login = login,
            IsSuccess = result.IsSuccessStatusCode
        };
    }
}
