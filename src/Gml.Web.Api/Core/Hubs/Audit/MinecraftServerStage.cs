using Gml.Models.Servers;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class MinecraftServerStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public MinecraftServerStage(IGmlManager gmlManager) : base(
        "Проверка серверов Minecraft",
        "Проверка доступности и состояния всех настроенных серверов Minecraft в профилях",
        [
            "Проверка наличия серверов в профилях",
            "Проверка состояния подключения к серверам",
            "Проверка доступности серверов по адресу и порту"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var profiles = await _gmlManager.Profiles.GetProfiles();

        foreach (var server in profiles.SelectMany(c => c.Servers).OfType<MinecraftServer>())
        {
            await _gmlManager.Servers.UpdateServerState(server);

            if (!server.IsOnline)
            {
                AddWarning($"Сервер не в сети: {server.Name} {server.Address}:{server.Port}");
            }
        }
    }
}
