using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class DiscordAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public DiscordAuditStage(IGmlManager gmlManager) : base(
        "Проверка настройки Discord",
        "Проверка корректности заполнения всех обязательных полей конфигурации Discord Rich Presence",
        [
            "Проверка ClientId Discord",
            "Проверка Details Discord",
            "Проверка LargeImageKey Discord",
            "Проверка LargeImageText Discord",
            "Проверка SmallImageKey Discord",
            "Проверка SmallImageText Discord"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var discord = await _gmlManager.Integrations.GetDiscordRpc();

        if (discord is null)
        {
            AddWarning("Discord не настроен");
            return;
        }

        if (string.IsNullOrEmpty(discord.ClientId))
        {
            AddWarning("В Дискорде не указано значение СlientId");
        }

        if (string.IsNullOrEmpty(discord.Details))
        {
            AddWarning("В Дискорде не указано значение Details");
        }

        if (string.IsNullOrEmpty(discord.LargeImageKey))
        {
            AddWarning("В Дискорде не указано значение LargeImageKey");
        }

        if (string.IsNullOrEmpty(discord.LargeImageText))
        {
            AddWarning("В Дискорде не указано значение LargeImageText");
        }

        if (string.IsNullOrEmpty(discord.SmallImageKey))
        {
            AddWarning("В Дискорде не указано значение SmallImageKey");
        }

        if (string.IsNullOrEmpty(discord.SmallImageText))
        {
            AddWarning("В Дискорде не указано значение SmallImageText");
        }
    }
}
