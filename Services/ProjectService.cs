using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class ProjectService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void SaveProject(string path, NestLaserProject project)
    {
        ProjectBackupService.CreateBackupBeforeSave(path);
        ProjectMigrationService.Migrate(project);
        ProjectIntegrityService.ValidateAndRepair(project);
        project.UpdatedAt = DateTime.Now;
        project.LastSavedWithVersion = ProjectMigrationService.CurrentProjectVersion;
        SafeJsonFileService.Save(path, project, _options);
    }

    public static NestLaserProject? LoadProject(string path)
        => LoadProjectWithRecovery(path).Project;

    public static ProjectLoadResult LoadProjectWithRecovery(string path)
    {
        var result = new ProjectLoadResult();
        if (!File.Exists(path))
        {
            result.RecoveryReport.LostSections.Add("Project file");
            return result;
        }

        try
        {
            var project = LoadFromPath(path);
            if (project == null)
            {
                result.RecoveryReport.LostSections.Add("Project");
                return result;
            }

            result.Project = ProjectMigrationService.Migrate(project, result.RecoveryReport);
            ProjectIntegrityService.ValidateAndRepair(result.Project, result.RecoveryReport);
            return result;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Project load failed: {path}");
            result.RecoveryReport.CorruptedSections.Add("Project JSON");

            string backupPath = path + ".bak";
            if (!File.Exists(backupPath))
            {
                result.RecoveryReport.LostSections.Add("Project");
                return result;
            }

            try
            {
                var backupProject = LoadFromPath(backupPath);
                if (backupProject == null)
                {
                    result.RecoveryReport.LostSections.Add("Backup project");
                    return result;
                }

                result.RecoveryReport.UsedBackup = true;
                result.RecoveryReport.RecoveredSections.Add("Project backup");
                result.Project = ProjectMigrationService.Migrate(backupProject, result.RecoveryReport);
                ProjectIntegrityService.ValidateAndRepair(result.Project, result.RecoveryReport);
                return result;
            }
            catch (Exception backupEx)
            {
                AppLogger.LogError(backupEx, $"Project backup load failed: {backupPath}");
                result.RecoveryReport.CorruptedSections.Add("Project backup JSON");
                result.RecoveryReport.LostSections.Add("Project");
                return result;
            }
        }
    }

    private static NestLaserProject? LoadFromPath(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<NestLaserProject>(json, _options);
    }

    public static void SaveRecentProjects(List<string> projects)
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NestLaser");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            string path = Path.Combine(folder, "recent-projects.json");
            SafeJsonFileService.Save(path, projects, _options);
        }
        catch (Exception ex) { AppLogger.LogError(ex, "Recent projects save failed"); }
    }

    public static List<string> LoadRecentProjects()
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NestLaser", "recent-projects.json");
            if (!File.Exists(path)) return new List<string>();

            return SafeJsonFileService.Load<List<string>>(path, _options) ?? new List<string>();
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, "Recent projects load failed");
            return new List<string>();
        }
    }
}
