using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class ProtocolAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;


    public ProtocolAuditStage(IGmlManager gmlManager, IServiceProvider provider) : base(
        "Проверка согласованности протоколов",
        "Проверка соответствия протоколов (HTTP/HTTPS) между настройками системы и используемыми сервисами",
        [
            "Проверка единообразия протоколов всех сервисов",
            "Проверка соответствия протокола в настройках системы",
            "Проверка соответствия протокола хоста"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        List<string> urls = [];

        if (!string.IsNullOrEmpty(Host))
            urls.Add(Host);

        try
        {
            var skin = await _gmlManager.Integrations.GetSkinServiceAsync();
            urls.Add(skin);
        }
        catch
        {
            // ignored
        }

        try
        {
            var cloak = await _gmlManager.Integrations.GetCloakServiceAsync();
            urls.Add(cloak);
        }
        catch
        {
            // ignored
        }

        var sentryUrl = await _gmlManager.Integrations.GetSentryService();
        var authUrl = await _gmlManager.Integrations.GetActiveAuthService();

        if (!string.IsNullOrEmpty(sentryUrl))
            urls.Add(sentryUrl);

        if (authUrl is not null && !string.IsNullOrEmpty(sentryUrl) && authUrl.AuthType != AuthType.Any)
            urls.Add(authUrl.Endpoint);

        if (urls.Count > 1)
        {
            var protocols = urls
                .Select(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Scheme : null)
                .Where(scheme => scheme != null)
                .Distinct()
                .ToList();

            if (protocols.Count > 1)
            {
                AddWarning(
                    "Обнаружено использование разных протоколов (HTTP и HTTPS). Рекомендуется использовать единый протокол для всех сервисов.\n" +
                    string.Join("\n", urls));
            }
        }

        var mostPopularProtocol = urls
            .Select(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Scheme : null)
            .Where(scheme => scheme != null)
            .GroupBy(scheme => scheme)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

        var protocol = _gmlManager.LauncherInfo.StorageSettings.TextureProtocol;

        string? hostProtocol = null;
        if (!string.IsNullOrEmpty(Host) && Uri.TryCreate(Host, UriKind.Absolute, out var hostUri))
        {
            hostProtocol = hostUri.Scheme;
        }

        if (protocol.ToString().ToLower() != mostPopularProtocol?.ToLower())
        {
            AddError(
                $"Протокол в настройках системы ({protocol.ToString().ToLower()}) не совпадает с наиболее используемым протоколом ({mostPopularProtocol})");
        }

        if (!string.IsNullOrEmpty(hostProtocol) && protocol.ToString().ToLower() != hostProtocol.ToLower())
        {
            AddError(
                $"Протокол в настройках системы ({protocol.ToString().ToLower()}) не совпадает с протоколом хоста ({hostProtocol})");
        }

        if (!string.IsNullOrEmpty(Host) && Host.Contains("localhost", StringComparison.InvariantCultureIgnoreCase))
        {
            AddWarning(
                "Вы используете localhost для серверной части. Это значит что никто кроме вас не сможет зайти на проект. За исключением случаев, когда оба устройства находятся в одной сети");
        }
    }
}
