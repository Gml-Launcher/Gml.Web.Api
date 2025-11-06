using System.IO.Compression;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Services;

public class RestoreService(IGmlManager gmlManager)
{
    public void Restore(string backupKey)
    {
        if (string.IsNullOrWhiteSpace(backupKey))
        {
            throw new ArgumentNullException(nameof(backupKey));
        }

        var directory = new DirectoryInfo(Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "backups"));

        if (!directory.Exists)
        {
            throw new Exception("Backup directory not found");
        }

        var archiveFile = directory.GetFiles($"*.gbak")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.Name) == backupKey);

        if (archiveFile is null)
        {
            throw new Exception("Backup file not found");
        }

        MoveFiles(archiveFile);
    }

    private void MoveFiles(FileInfo archiveFile)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(archiveFile.Name));
        Directory.CreateDirectory(tempPath);
        ZipFile.ExtractToDirectory(archiveFile.FullName, tempPath, true);

        var sourceDirectoryInfo = new DirectoryInfo(tempPath);
        foreach (var file in sourceDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(tempPath, file.FullName);
            var targetPath = Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            file.CopyTo(targetPath, true);
        }


        Directory.Delete(tempPath, true);
    }

    public async Task<IReadOnlyCollection<string>> GetBackupKeysAsync()
    {
        var directory = new DirectoryInfo(Path.Combine(gmlManager.LauncherInfo.InstallationDirectory, "backups"));

        if (directory.Exists)
        {
            return directory.GetFiles("*.gbak").Select(file => Path.GetFileNameWithoutExtension(file.Name)).ToList();
        }

        return [];
    }
}
