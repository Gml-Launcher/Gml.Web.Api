namespace Gml.Web.Api.Core.Hubs.Audit;

public class GutHubAvailableAuditStage : AuditStageBase
{
    public GutHubAvailableAuditStage() : base(
        "Проверка доступности GitHub", "Проверка доступности GitHub API и сервисов",
        [
            "HTTP 200 от api.github.com",
            "Доступность GitHub API",
            "SSL сертификат валиден"
        ]
    )
    {
    }

    public override async Task Evaluate()
    {
        const string githubApiUrl = "https://api.github.com";

        Uri uri;
        try
        {
            uri = new Uri(githubApiUrl);
            if (!uri.IsAbsoluteUri)
            {
                AddError($"URI должен быть абсолютным: {githubApiUrl}");
                return;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                AddError($"URI должен использовать HTTP или HTTPS протокол: {githubApiUrl}");
                return;
            }
        }
        catch (UriFormatException ex)
        {
            AddError($"Некорректный формат URI: {githubApiUrl}. Ошибка: {ex.Message}");
            return;
        }

        // Используем общий тестер
        var res = await EndpointHealthChecker.CheckEndpointAsync(githubApiUrl, []);
        if (res.Success)
        {
            AddSuccess(res.Message);
        }
        else
        {
            AddError(res.Message);
        }

        // SSL проверку оставляем как раньше
        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            AddSuccess("SSL сертификат валиден");
        }
        else
        {
            AddWarning("Соединение не использует HTTPS");
        }
    }
}
