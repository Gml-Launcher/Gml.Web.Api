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

    public async Task<AuthResult> Auth(string login, string password, string? totp = null)
    {
        var authService = (await gmlManager.Integrations.GetActiveAuthService())!.Endpoint;

        var baseUri = new Uri(authService);

        var endpoint = $"{baseUri.Scheme}://{baseUri.Host}/auth/login";

        var dto = JsonConvert.SerializeObject(new
        {
            username_or_email = login,
            password,
            totp = totp ?? string.Empty,
            save_me = string.Empty
        });

        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var result =
            await _httpClient.PostAsync(endpoint, content);

        var responseResult = await result.Content.ReadAsStringAsync();

        if (responseResult.Contains("require2fa"))
        {
            return new AuthResult
            {
                IsSuccess = false,
                Message = "Введите код из приложения 2FA",
                TwoFactorEnabled = true
            };
        }

        var data = JsonConvert.DeserializeObject<UnicoreAuthResult>(responseResult);

        if (data is null || !result.IsSuccessStatusCode || data.User is null || data?.User?.Ban is not null)
        {
            if (data?.User?.Ban is { } ban)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Message = $"Пользователь заблокирован. Причина: {ban.Reason}"
                };
            }

            return new AuthResult
            {
                IsSuccess = false,
                Message = responseResult.Contains("\"statusCode\":401")
                    ? "Неверный логин или пароль"
                    : "Произошла ошибка при обработке данных с сервера авторизации."
            };
        }

        return new AuthResult
        {
            Login = data.User.Username ?? login,
            IsSuccess = result.IsSuccessStatusCode,
            Uuid = data.User.Uuid,
            IsSlim = data.User.Skin?.Slim ?? false,
            TwoFactorEnabled = data.User.TwoFactorEnabled is true,
            TwoFactorSecret = data.User.TwoFactorSecret?.ToString(),
            TwoFactorSecretTemp = data.User.TwoFactorSecretTemp
        };
    }
}
