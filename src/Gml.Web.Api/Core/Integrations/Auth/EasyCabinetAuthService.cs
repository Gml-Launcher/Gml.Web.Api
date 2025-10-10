using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Gml.Web.Api.Domains.Integrations;
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

        // Базовый корень хоста (как в AzuriomService)
        var root = $"{baseUri.Scheme}://{baseUri.Host}";
        var loginEndpoint = $"{root}/auth/login";
        var meEndpoint = $"{root}/users";

        // 1) Логинимся и получаем accessToken
        var dto = JsonConvert.SerializeObject(new { login, password });
        var content = new StringContent(dto, Encoding.UTF8, "application/json");

        var loginResponse = await _httpClient.PostAsync(loginEndpoint, content);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();

        if (!loginResponse.IsSuccessStatusCode)
        {
            // Для большинства случаев 401/400 — просто неверные креды
            var msg = loginResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest
                ? "Неверный логин или пароль."
                : $"Ошибка авторизации: {(int)loginResponse.StatusCode}";

            return new AuthResult
            {
                Login = login,
                IsSuccess = false,
                Message = msg
            };
        }

        var tokenModel = JsonConvert.DeserializeObject<TokenResponse>(loginBody);
        if (tokenModel is null || string.IsNullOrWhiteSpace(tokenModel.AccessToken))
        {
            return new AuthResult
            {
                Login = login,
                IsSuccess = false,
                Message = "Не удалось получить токен доступа."
            };
        }

        // Попробуем вытащить uuid/login из JWT (не валидируя подпись — только парсинг)
        string? jwtUuid = null;
        string? jwtLogin = null;
        try
        {
            (jwtUuid, jwtLogin) = TryReadJwt(tokenModel.AccessToken);
        }
        catch
        {
            // игнор — не критично
        }

        // 2) По токену получаем профиль — там есть isAlex (он же slim-модель)
        bool isSlim = false;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, meEndpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.AccessToken);

            var meResponse = await _httpClient.SendAsync(req);
            if (meResponse.IsSuccessStatusCode)
            {
                var meBody = await meResponse.Content.ReadAsStringAsync();
                var me = JsonConvert.DeserializeObject<UserInfoResponse>(meBody);

                // В Minecraft-модели: Alex = slim (3px руки), Steve = classic
                isSlim = me?.IsAlex ?? false;
            }
            // если не получилось — не валим авторизацию, просто оставим isSlim=false
        }
        catch
        {
            // тоже не критично для авторизации
        }

        return new AuthResult
        {
            Uuid = jwtUuid,
            Login = jwtLogin ?? login,
            IsSlim = isSlim,
            IsSuccess = true
        };
    }

    // --- DTOs / helpers ---

    private sealed class TokenResponse
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class UserInfoResponse
    {
        [JsonProperty("isAlex")]
        public bool IsAlex { get; set; }

        [JsonProperty("skinUrl")]
        public string? SkinUrl { get; set; }

        [JsonProperty("capeUrl")]
        public string? CapeUrl { get; set; }
    }

    private static (string? Uuid, string? Login) TryReadJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return (null, null);

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var payload = JsonConvert.DeserializeObject<JwtPayload>(payloadJson);
        return (payload?.Uuid, payload?.Login);
    }

    private sealed class JwtPayload
    {
        [JsonProperty("uuid")]
        public string? Uuid { get; set; }

        [JsonProperty("login")]
        public string? Login { get; set; }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
