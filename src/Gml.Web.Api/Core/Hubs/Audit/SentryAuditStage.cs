using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class SentryAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public SentryAuditStage(IGmlManager gmlManager) : base(
        "Проверка настроек Sentry",
        "Проверка корректности конфигурации Sentry DSN, включая формат URL, используемый протокол и валидность хоста",
        [
            "Проверка наличия Sentry DSN",
            "Проверка корректности формата Sentry DSN",
            "Проверка используемого протокола",
            "Проверка валидности хоста Sentry"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        try
        {
            var sentryUrl = await _gmlManager.Integrations.GetSentryService();

            if (string.IsNullOrEmpty(sentryUrl))
            {
                AddWarning("Sentry DSN не настроен");
                return;
            }

            if (!ValidateSentryDsn(sentryUrl))
            {
                AddError($"Некорректный формат Sentry DSN: {sentryUrl}");
                return;
            }

            if (!Uri.TryCreate(sentryUrl, UriKind.Absolute, out var uri))
            {
                AddError($"Не удалось распарсить Sentry DSN: {sentryUrl}");
                return;
            }

            if (uri.Scheme == Uri.UriSchemeHttps)
            {
                AddSuccess($"Sentry DSN настроен корректно и использует HTTPS: {sentryUrl}");
            }
            else if (uri.Scheme == Uri.UriSchemeHttp)
            {
                AddWarning($"Sentry DSN использует небезопасный протокол HTTP: {sentryUrl}");
            }
            else
            {
                AddError($"Sentry DSN использует неподдерживаемый протокол: {uri.Scheme}");
            }
        }
        catch (Exception e)
        {
            AddError($"Не удалось проверить Sentry настройки: {e.Message}");
        }
    }

    bool ValidateSentryDsn(string dsn)
    {
        return Uri.TryCreate(dsn, UriKind.Absolute, out _);
    }
}
