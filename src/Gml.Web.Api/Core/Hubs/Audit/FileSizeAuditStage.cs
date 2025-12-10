using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class FileSizeAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public FileSizeAuditStage(IGmlManager gmlManager) : base(
        "Проверка размеров файлов и директорий",
        "Проверка и расчет размеров основных директорий и файлов системы",
        [
            "Проверка размера файла базы данных",
            "Проверка размера временной директории",
            "Проверка размера директории общих данных",
            "Проверка размера директории игровых данных"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var tempDirectory = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "temp");
        var assetsDirectory = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "shared data");
        var gameDirectory = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "game data");
        var launcherDirectory = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "Launcher");
        var launcherBuildsDirectory = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "LauncherBuilds");
        var attachmentsDirectory = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "Attachments");
        var databaseFile = Path.Combine(_gmlManager.LauncherInfo.InstallationDirectory, "data.db");

        await CalculateAndReportFileSize(databaseFile);
        await CalculateAndReportDirectorySize(tempDirectory);
        await CalculateAndReportDirectorySize(assetsDirectory);
        await CalculateAndReportDirectorySize(gameDirectory);
        await CalculateAndReportDirectorySize(launcherBuildsDirectory);
        await CalculateAndReportDirectorySize(attachmentsDirectory);
        await CalculateAndReportDirectorySize(launcherDirectory);
    }

    private async Task CalculateAndReportFileSize(string databaseFile)
    {
        if (!File.Exists(databaseFile))
        {
            AddWarning($"Файл не существует: {databaseFile}");
            return;
        }

        try
        {
            var fileInfo = new FileInfo(databaseFile);
            long fileSizeBytes = fileInfo.Length;

            double sizeInMb = fileSizeBytes / (1024.0 * 1024.0);
            AddDefault($"Размер файла {fileInfo.Name}: {sizeInMb:F2} МБ");
        }
        catch (Exception ex)
        {
            AddError($"Ошибка при подсчете размера файла: {ex.Message}");
        }
    }

    private async Task CalculateAndReportDirectorySize(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            AddWarning($"Директория не существует: {directoryPath}");
            return;
        }

        try
        {
            long totalSizeBytes = 0;
            var directoryInfo = new DirectoryInfo(directoryPath);

            foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                totalSizeBytes += file.Length;
            }

            double sizeInMb = totalSizeBytes / (1024.0 * 1024.0);
            AddDefault($"Размер директории {directoryPath}: {sizeInMb:F2} МБ");
        }
        catch (Exception ex)
        {
            AddError($"Ошибка при подсчете размера директории: {ex.Message}");
        }
    }
}
