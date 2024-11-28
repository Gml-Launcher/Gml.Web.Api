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

        var responseResult = await result.Content.ReadAsStringAsync();

        var data = JsonConvert.DeserializeObject<UnicoreAuthResult>(responseResult);

        if (data is null || !result.IsSuccessStatusCode || data.User is null || data?.User?.Ban is not null )
        {
            if (data?.User?.Ban is {} ban)
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
                Message = "Произошла ошибка при обработке данных с сервера авторизации."
            };
        }

        return new AuthResult
        {
            Login = data.User.Username ?? login,
            IsSuccess = result.IsSuccessStatusCode,
            Uuid = data.User.Uuid
        };
    }
}
