using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class ProfileAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public ProfileAuditStage(IGmlManager gmlManager) : base(
        "Проверка профилей Minecraft",
        "Проверка состояния всех настроенных профилей Minecraft в системе",
        [
            "Проверка наличия профилей в системе",
            "Проверка состояния профилей",
            "Проверка готовности профилей к использованию"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var profiles = await _gmlManager.Profiles.GetProfiles();

        foreach (var profile in profiles)
        {
            if (profile.State != ProfileState.Ready)
            {
                AddWarning(
                    $"Состояние профиля {profile.DisplayName} не находится в Ready. Фактическое значение {profile.State}");
            }
        }

        if (profiles.All(c => c.State == ProfileState.Ready))
        {
            AddSuccess("Все профили собраны и готовы к загрузке");
        }
    }
}
