using System;
using System.IO;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class ProjectPortabilityTests
{
    [Fact]
    [Trait("Category", "ProjectTests")]
    public void Migration_AddsVersionAndPortableSnapshots()
    {
        var project = CreatePortableProject();
        project.ProjectVersion = string.Empty;
        project.CreatedWithVersion = string.Empty;
        project.LastSavedWithVersion = string.Empty;

        var report = new ProjectRecoveryReport();
        ProjectMigrationService.Migrate(project, report);
        ProjectIntegrityService.ValidateAndRepair(project, report);

        Assert.Equal(ProjectMigrationService.CurrentProjectVersion, project.ProjectVersion);
        Assert.Equal(ProjectMigrationService.CurrentProjectVersion, project.LastSavedWithVersion);
        Assert.NotNull(project.ProfileSnapshot.Material);
        Assert.NotNull(project.ProfileSnapshot.Machine);
        Assert.NotNull(project.ProfileSnapshot.CostSettings);
        Assert.Single(project.ProfileSnapshot.OperationSettings);
        Assert.True(report.MigrationNotes.Count > 0);
    }

    [Fact]
    [Trait("Category", "ProjectTests")]
    public void ProjectLoad_RecoversFromBakWhenMainJsonIsCorrupt()
    {
        var tempDir = CreateTempDir();
        var projectPath = Path.Combine(tempDir, "corrupt.nelp");

        ProjectService.SaveProject(projectPath, CreatePortableProject("First"));
        ProjectService.SaveProject(projectPath, CreatePortableProject("Recovered"));
        File.WriteAllText(projectPath, "{ broken json");

        var result = ProjectService.LoadProjectWithRecovery(projectPath);

        Assert.True(result.Success);
        Assert.True(result.RecoveryReport.UsedBackup);
        Assert.Equal("First", result.Project!.ProjectName);
        Assert.Contains("Project backup", result.RecoveryReport.RecoveredSections);
    }

    [Fact]
    [Trait("Category", "ProjectTests")]
    public void ProjectPackage_ExportsAndImportsPortableProject()
    {
        var tempDir = CreateTempDir();
        var packagePath = Path.Combine(tempDir, "portable.nelpkg");
        var project = CreatePortableProject();

        ProjectPackageService.ExportPackage(packagePath, project);
        var imported = ProjectPackageService.ImportPackage(packagePath);

        Assert.True(File.Exists(packagePath));
        Assert.True(new FileInfo(packagePath).Length > 0);
        Assert.True(imported.Success);
        Assert.Equal(project.ProjectName, imported.Project!.ProjectName);
        Assert.Equal("mat-portable", imported.Project.ProfileSnapshot.Material?.Id);
        Assert.Equal("machine-portable", imported.Project.ProfileSnapshot.Machine?.Id);
        Assert.Single(imported.Project.ProfileSnapshot.OperationSettings);
        Assert.Equal("TRY", imported.Project.ProfileSnapshot.CostSettings?.Currency);
    }

    [Fact]
    [Trait("Category", "ProjectTests")]
    public void ProjectIntegrity_RepairsMissingIdsAndProfileReferences()
    {
        var project = CreatePortableProject();
        project.SelectedMaterialId = string.Empty;
        project.SelectedMachineId = string.Empty;
        project.Parts[0].Id = string.Empty;
        project.Parts[0].LayerId = "missing-layer";
        project.Operations[0].Id = string.Empty;
        project.Operations[0].LayerId = "missing-layer";

        var report = ProjectIntegrityService.ValidateAndRepair(project);

        Assert.False(string.IsNullOrWhiteSpace(project.Parts[0].Id));
        Assert.Equal(project.Layers[0].Id, project.Parts[0].LayerId);
        Assert.False(string.IsNullOrWhiteSpace(project.Operations[0].Id));
        Assert.Equal(project.Layers[0].Id, project.Operations[0].LayerId);
        Assert.Equal("mat-portable", project.SelectedMaterialId);
        Assert.Equal("machine-portable", project.SelectedMachineId);
        Assert.True(report.IntegrityWarnings.Count >= 2);
    }

    [Fact]
    [Trait("Category", "ProjectTests")]
    public void BackupSystem_KeepsLatestTenProjectBackups()
    {
        var tempDir = CreateTempDir();
        var projectPath = Path.Combine(tempDir, "backup-limit.nelp");

        ProjectService.SaveProject(projectPath, CreatePortableProject("Initial"));
        for (int i = 0; i < 12; i++)
        {
            ProjectService.SaveProject(projectPath, CreatePortableProject($"Revision {i}"));
        }

        var backupDir = Path.Combine(tempDir, "Backups");
        Assert.True(Directory.Exists(backupDir));
        Assert.True(Directory.GetFiles(backupDir, "backup-limit_*.nelp").Length <= 10);
    }

    private static string CreateTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NestLaserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static NestLaserProject CreatePortableProject(string name = "Portable Project")
    {
        var layer = new LayerModel { Id = "layer-cut", Name = "Cut", Type = LayerType.Cut };
        var material = new MaterialProfile
        {
            Id = "mat-portable",
            Name = "Portable Steel",
            ThicknessMm = 2,
            UnitPrice = 100,
            UnitType = UnitType.PerSheet
        };
        var machine = new MachineProfile
        {
            Id = "machine-portable",
            Name = "Portable Fiber",
            WorkingAreaX = 3000,
            WorkingAreaY = 1500
        };
        var operationSetting = new MaterialOperationSetting
        {
            Id = "setting-portable",
            MaterialId = material.Id,
            MachineId = machine.Id,
            OperationType = OperationType.CutOuter,
            Speed = 25
        };

        return new NestLaserProject
        {
            ProjectName = name,
            SelectedMaterialId = material.Id,
            SelectedMachineId = machine.Id,
            Layers = new() { layer },
            Operations = new()
            {
                new LaserOperation
                {
                    Id = "op-cut",
                    Name = "Cut outer",
                    LayerId = layer.Id,
                    LayerName = layer.Name,
                    Type = OperationType.CutOuter
                }
            },
            Parts = new()
            {
                new PartModel
                {
                    Id = "part-1",
                    Name = "Part 1",
                    LayerId = layer.Id,
                    LayerName = layer.Name,
                    Geometry = GeometryUtils.CreateRectangle(20, 10)
                }
            },
            ProfileSnapshot = new ProfileSnapshot
            {
                Material = material,
                Machine = machine,
                OperationSettings = new() { operationSetting },
                CostSettings = new CostSettings { Currency = "TRY" }
            },
            CompanyProfile = new CompanyProfile { CompanyName = "Portable Company" },
            PdfReportSettings = new PdfReportSettings { LastReportType = "Quotation" }
        };
    }
}
