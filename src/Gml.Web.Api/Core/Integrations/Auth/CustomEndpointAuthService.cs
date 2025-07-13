using System.Text;
using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class CustomEndpointAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : IPlatformAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public virtual async Task<AuthResult> Auth(string login, string password, string? totp = null)
    {
        var dto = JsonConvert.SerializeObject(new
        {
            Login = login,
            Password = password,
            Totp = totp
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var authService = await gmlManager.Integrations.GetActiveAuthService();

        var result =await _httpClient.PostAsync(authService!.Endpoint, content);

        var resultContent = await result.Content.ReadAsStringAsync();

        var authResult = new AuthResult
        {
            Login = login,
            IsSuccess = result.IsSuccessStatusCode
        };

        if (string.IsNullOrEmpty(resultContent))
            return authResult;

        if (!result.IsSuccessStatusCode && resultContent.Contains("2fa", StringComparison.CurrentCultureIgnoreCase))
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = "Введите код из приложения 2FA",
                TwoFactorEnabled = true
            };
        }

        var model = JsonConvert.DeserializeObject<AuthCustomResponse>(resultContent);

        authResult.Login = model?.Login ?? login;
        authResult.Uuid = model?.UserUuid;

        return authResult;

    }
}
