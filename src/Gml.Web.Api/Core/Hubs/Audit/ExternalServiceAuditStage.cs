using Gml.Web.Api.Core.Integrations.Auth;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class ExternalServiceAuditStage : AuditStageBase
{
    private readonly IAuthService _authService;
    private readonly IGmlManager _gmlManager;

    public ExternalServiceAuditStage(IGmlManager gmlManager, IServiceProvider provider) : base(
        "Проверка авторизации", "Проверка что пользователь может авторизоваться по логину и паролю",
        [
            "Доступность API шлюза",
        ]
    )
    {
        _authService = provider.GetRequiredService<IAuthService>();
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        try
        {
            var site = await _gmlManager.Integrations.GetActiveAuthService();

            if (site is null)
            {
                AddError("Сервис авторизации не настроен");
                return;
            }

            var result = await _authService.CheckAuth("GmlAdmin", "Test", site.AuthType, "GML-Audit");

            AddSuccess(
                $"Авторизация подключена. Для пользователя статус: {result.IsSuccess}, Ваш сайт вернул сообщение: {result.Message}");
        }
        catch (Exception e)
        {
            AddError($"Не удалось проверить авторизацию: {e.Message}");
        }
    }
}
