using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class ProjectWorkflowService
{
    public static async Task<NestLaserProject?> OpenAsync(
        string path,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => (await OpenWithRecoveryAsync(path, progress, cancellationToken)).Project;

    public static Task<ProjectLoadResult> OpenWithRecoveryAsync(
        string path,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("Proje dosyasi okunuyor...", 25));
            var result = ProjectService.LoadProjectWithRecovery(path);
            progress?.Report(new WorkflowProgress("Proje verisi hazirlaniyor...", 80));
            return result;
        }, cancellationToken);
    }

    public static Task SaveAsync(
        string path,
        NestLaserProject project,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("Proje kaydediliyor...", 35));
            ProjectService.SaveProject(path, project);
            progress?.Report(new WorkflowProgress("Proje yedek kaydi guncellendi.", 90));
        }, cancellationToken);
    }

    public static List<string> LoadRecentProjects() => ProjectService.LoadRecentProjects();

    public static void SaveRecentProjects(List<string> projects) => ProjectService.SaveRecentProjects(projects);

    public static List<string> AddRecentProject(IEnumerable<string> current, string path, int maxCount = 5)
    {
        var list = new List<string>(current);
        if (string.IsNullOrWhiteSpace(path)) return list;

        list.Remove(path);
        list.Insert(0, path);

        while (list.Count > maxCount)
            list.RemoveAt(list.Count - 1);

        SaveRecentProjects(list);
        return list;
    }

    public static List<string> RemoveRecentProject(IEnumerable<string> current, string path)
    {
        var list = new List<string>(current);
        list.Remove(path);
        SaveRecentProjects(list);
        return list;
    }

    public static string DefaultProjectFileName(string projectName)
        => string.IsNullOrWhiteSpace(projectName) ? "Yeni Proje.nelp" : $"{projectName}.nelp";

    public static string ProjectNameFromPath(string path) => Path.GetFileNameWithoutExtension(path);
}
