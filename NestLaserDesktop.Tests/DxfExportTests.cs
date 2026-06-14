using System;
using System.Collections.Generic;
using System.IO;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class DxfExportTests
{
    [Fact]
    [Trait("Category", "DxfExportTests")]
    public void Export_CreatesDxfAndReport()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NestLaserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dxfPath = Path.Combine(tempDir, "export.dxf");
        var layer = new LayerModel { Id = "cut", Name = "Cut", Type = LayerType.Cut, IsVisible = true };
        var part = new PartModel
        {
            Id = "part-1",
            Name = "Part 1",
            LayerId = layer.Id,
            LayerName = layer.Name,
            Geometry = GeometryUtils.CreateRectangle(100, 50)
        };

        var report = DxfService.Export(
            dxfPath,
            new PlateModel { Width = 500, Height = 300 },
            null,
            new List<PartModel> { part },
            Array.Empty<PartModel>(),
            new List<LayerModel> { layer },
            new DxfExportOptions { UseNestResult = false, ExportPlateBorders = true });

        Assert.True(File.Exists(dxfPath));
        Assert.Contains("LWPOLYLINE", File.ReadAllText(dxfPath));
        Assert.True(File.Exists(report.ReportPath));
        Assert.Contains("NESTLASER", File.ReadAllText(report.ReportPath));
        Assert.Equal(1, report.PartCount);
    }
}
