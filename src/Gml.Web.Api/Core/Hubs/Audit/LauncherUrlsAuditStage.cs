using System.Text.RegularExpressions;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class LauncherUrlsAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public LauncherUrlsAuditStage(IGmlManager gmlManager) : base(
        "Проверка URL-адресов лаунчера",
        "Проверка соответствия URL-адресов в исходном коде лаунчера с адресом хоста системы",
        [
            "Проверка наличия исходников лаунчера",
            "Проверка соответствия HOST в ResourceKeysDictionary.cs",
            "Проверка корректности указанного адреса хоста"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var launcherFolder = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "Launcher");

        if (!Directory.Exists(launcherFolder))
        {
            AddWarning($"В директории с лаунчером отсутствуют загруженные исходники: {launcherFolder}");
            return;
        }

        if (string.IsNullOrEmpty(Host))
        {
            AddError("Host is empty");
            return;
        }

        var normalizedHost = Host.TrimEnd('/');
        var configs = Directory.GetFiles(launcherFolder, "ResourceKeysDictionary.cs", SearchOption.AllDirectories);
        var pattern = @"public\s+const\s+string\s+Host\s*=\s*"""
                      + Regex.Escape(normalizedHost) +
                      @"/?""\s*;";
        var regex = new Regex(pattern, RegexOptions.Compiled);

        var isFound = false;
        foreach (var config in configs)
        {
            var content = await File.ReadAllTextAsync(config);

            if (!regex.IsMatch(content))
            {
                isFound = true;
                AddError($"В файле {config} адрес отличается от {Host}");
            }
        }

        if (!isFound)
        {
            AddSuccess("Все адреса настроены верно в лаунчере");
        }
    }
}
