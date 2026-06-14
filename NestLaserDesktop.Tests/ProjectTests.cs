using System;
using System.Collections.Generic;
using System.IO;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class ProjectTests
{
    [Fact]
    [Trait("Category", "ProjectTests")]
    public void ProjectSaveLoad_RoundTripsProfileSnapshotsAndPdfSettings()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NestLaserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var projectPath = Path.Combine(tempDir, "portable.nelp");
        var material = new MaterialProfile
        {
            Id = "mat-portable",
            Name = "Portable Steel",
            ThicknessMm = 2,
            UnitPrice = 100,
            UnitType = UnitType.PerSheet
        };
        var machine = new MachineProfile { Id = "machine-portable", Name = "Portable Fiber" };

        var project = new NestLaserProject
        {
            ProjectName = "Portable Project",
            SelectedMaterialId = material.Id,
            SelectedMachineId = machine.Id,
            ProfileSnapshot = new ProfileSnapshot { Material = material, Machine = machine },
            CompanyProfile = new CompanyProfile { CompanyName = "ACME Laser", Email = "sales@example.com" },
            PdfReportSettings = new PdfReportSettings { LastReportType = "Quotation" },
            Parts = new List<PartModel>
            {
                new() { Id = "p1", Name = "P1", Geometry = GeometryUtils.CreateRectangle(10, 10) }
            }
        };

        ProjectService.SaveProject(projectPath, project);
        var loaded = ProjectService.LoadProject(projectPath);

        Assert.NotNull(loaded);
        Assert.Equal("Portable Project", loaded!.ProjectName);
        Assert.Equal("Portable Steel", loaded.ProfileSnapshot.Material?.Name);
        Assert.Equal("Portable Fiber", loaded.ProfileSnapshot.Machine?.Name);
        Assert.Equal("ACME Laser", loaded.CompanyProfile.CompanyName);
        Assert.Equal("Quotation", loaded.PdfReportSettings.LastReportType);
        Assert.Single(loaded.Parts);
    }

    [Fact]
    [Trait("Category", "ProjectTests")]
    public void SafeProjectSave_CreatesBackupFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NestLaserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var projectPath = Path.Combine(tempDir, "backup.nelp");

        ProjectService.SaveProject(projectPath, new NestLaserProject { ProjectName = "First" });
        ProjectService.SaveProject(projectPath, new NestLaserProject { ProjectName = "Second" });

        Assert.True(File.Exists(projectPath));
        Assert.True(File.Exists(projectPath + ".bak"));
        Assert.Equal("Second", ProjectService.LoadProject(projectPath)?.ProjectName);
    }
}
