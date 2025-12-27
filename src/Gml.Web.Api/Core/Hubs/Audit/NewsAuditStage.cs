using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class NewsAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public NewsAuditStage(IGmlManager gmlManager) : base(
        "Проверка настройки новостей",
        "Проверка корректности настройки новостных провайдеров и возможности получения новостей",
        [
            "Проверка наличия настроенных новостных провайдеров",
            "Проверка доступности новостей",
            "Проверка корректности получения списка новостей"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var providers = _gmlManager.Integrations.NewsProvider.Providers;

        if (providers.Count == 0)
        {
            AddWarning("Не настроен ни один из новостных провайдеров");
            return;
        }

        try
        {
            foreach (var provider in providers)
            {
                await provider.GetNews();
            }

            AddSuccess("Все новостные провайдеры работают");
        }
        catch (Exception e)
        {
            AddWarning($"Не удалось получить список новостей: {e.Message}");
        }
    }
}
