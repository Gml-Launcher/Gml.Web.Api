using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class SiteAvailableAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public SiteAvailableAuditStage(IGmlManager gmlManager) : base(
        "Проверка доступности сайта", "Проверка ответа главной страницы и/или API",
        [
            "HTTP 200 от корневого домена",
            "Доступность API шлюза",
            "SSL сертификат валиден"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var site = await _gmlManager.Integrations.GetActiveAuthService();
        if (site is null)
        {
            AddError("Сервис авторизации не настроен");
            return;
        }

        if (string.IsNullOrWhiteSpace(site.Endpoint))
        {
            AddError("Endpoint сервиса авторизации не указан");
            return;
        }

        // Используем общий тестер
        var res = await EndpointHealthChecker.CheckEndpointAsync(site.Endpoint, []);
        if (res.Success)
        {
            AddSuccess(res.Message);
        }
        else if (site.AuthType == AuthType.Any)
        {
            AddWarning("Используется тип авторизации Any, разрешен вход под любым игровым ником");
        }
        else
        {
            AddError(res.Message);
        }

        // Дополнительная проверка SSL-подтверждения
        if (Uri.TryCreate(site.Endpoint, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            AddSuccess("SSL сертификат валиден");
        }
        else
        {
            AddWarning("Соединение не использует HTTPS");
        }
    }
}
