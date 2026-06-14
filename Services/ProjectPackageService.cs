using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class ProjectPackageService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void ExportPackage(
        string packagePath,
        NestLaserProject project,
        string? pdfPath = null,
        string? exportReportPath = null)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "NestLaserPackage_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            project = ProjectMigrationService.Migrate(project);
            ProjectIntegrityService.ValidateAndRepair(project);

            var manifest = new ProjectPackageManifest
            {
                CreatedWithVersion = ProjectMigrationService.CurrentProjectVersion
            };

            WriteJson(Path.Combine(tempDir, "project.nelp"), project);
            manifest.IncludedFiles.Add("project.nelp");

            WriteJson(Path.Combine(tempDir, "material-snapshot.json"), project.ProfileSnapshot.Material);
            manifest.IncludedFiles.Add("material-snapshot.json");

            WriteJson(Path.Combine(tempDir, "machine-snapshot.json"), project.ProfileSnapshot.Machine);
            manifest.IncludedFiles.Add("machine-snapshot.json");

            WriteJson(Path.Combine(tempDir, "operation-settings-snapshot.json"), project.ProfileSnapshot.OperationSettings);
            manifest.IncludedFiles.Add("operation-settings-snapshot.json");

            WriteJson(Path.Combine(tempDir, "cost-settings-snapshot.json"), project.ProfileSnapshot.CostSettings);
            manifest.IncludedFiles.Add("cost-settings-snapshot.json");

            CopyOptional(pdfPath, tempDir, "report.pdf", manifest);
            CopyOptional(exportReportPath, tempDir, "export-report.txt", manifest);

            manifest.IncludedFiles.Add("manifest.json");
            WriteJson(Path.Combine(tempDir, "manifest.json"), manifest);

            string? directory = Path.GetDirectoryName(packagePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            if (File.Exists(packagePath))
                File.Delete(packagePath);

            ZipFile.CreateFromDirectory(tempDir, packagePath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex, $"Package temp cleanup failed: {tempDir}");
            }
        }
    }

    public static ProjectLoadResult ImportPackage(string packagePath)
    {
        var result = new ProjectLoadResult();
        if (!File.Exists(packagePath))
        {
            result.RecoveryReport.LostSections.Add("Package");
            return result;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "NestLaserPackageImport_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            ZipFile.ExtractToDirectory(packagePath, tempDir);
            string projectPath = Path.Combine(tempDir, "project.nelp");
            if (!File.Exists(projectPath))
            {
                result.RecoveryReport.LostSections.Add("project.nelp");
                return result;
            }

            result = ProjectService.LoadProjectWithRecovery(projectPath);
            if (result.Project != null)
            {
                RestorePackageSnapshots(tempDir, result.Project, result.RecoveryReport);
                ProjectIntegrityService.ValidateAndRepair(result.Project, result.RecoveryReport);
            }

            return result;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, $"Package import failed: {packagePath}");
            result.RecoveryReport.CorruptedSections.Add("Package");
            return result;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                AppLogger.LogError(ex, $"Package import temp cleanup failed: {tempDir}");
            }
        }
    }

    private static void RestorePackageSnapshots(string tempDir, NestLaserProject project, ProjectRecoveryReport report)
    {
        project.ProfileSnapshot ??= new ProfileSnapshot();

        project.ProfileSnapshot.Material ??= ReadJson<MaterialProfile>(Path.Combine(tempDir, "material-snapshot.json"));
        project.ProfileSnapshot.Machine ??= ReadJson<MachineProfile>(Path.Combine(tempDir, "machine-snapshot.json"));
        project.ProfileSnapshot.OperationSettings ??= ReadJson<List<MaterialOperationSetting>>(Path.Combine(tempDir, "operation-settings-snapshot.json")) ?? new();
        project.ProfileSnapshot.CostSettings ??= ReadJson<CostSettings>(Path.Combine(tempDir, "cost-settings-snapshot.json"));

        report.RecoveredSections.Add("Package snapshots");
    }

    private static void CopyOptional(string? sourcePath, string tempDir, string packageName, ProjectPackageManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return;

        File.Copy(sourcePath, Path.Combine(tempDir, packageName), overwrite: true);
        manifest.IncludedFiles.Add(packageName);
    }

    private static void WriteJson<T>(string path, T value)
        => File.WriteAllText(path, JsonSerializer.Serialize(value, Options));

    private static T? ReadJson<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
    }
}
