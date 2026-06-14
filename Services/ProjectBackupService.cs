using System.IO;

namespace NestLaserDesktop.Services;

public static class ProjectBackupService
{
    private const int MaxBackups = 10;

    public static string? CreateBackupBeforeSave(string projectPath)
    {
        if (!File.Exists(projectPath))
            return null;

        string? directory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(directory))
            directory = Directory.GetCurrentDirectory();

        string backupDir = Path.Combine(directory, "Backups");
        Directory.CreateDirectory(backupDir);

        string name = Path.GetFileNameWithoutExtension(projectPath);
        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmm");
        string backupPath = Path.Combine(backupDir, $"{name}_{timestamp}.nelp");

        if (File.Exists(backupPath))
        {
            backupPath = Path.Combine(backupDir, $"{name}_{timestamp}_{DateTime.Now:ssfff}.nelp");
        }

        File.Copy(projectPath, backupPath, overwrite: false);
        CleanupOldBackups(backupDir, name);
        return backupPath;
    }

    private static void CleanupOldBackups(string backupDir, string projectName)
    {
        var backups = Directory
            .GetFiles(backupDir, $"{projectName}_*.nelp")
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.CreationTimeUtc)
            .ToList();

        foreach (var backup in backups.Skip(MaxBackups))
        {
            try { backup.Delete(); }
            catch (Exception ex) { AppLogger.LogError(ex, $"Project backup cleanup failed: {backup.FullName}"); }
        }
    }
}
