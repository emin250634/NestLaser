using System;
using System.Collections.Generic;
using System.IO;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class PdfTests
{
    [Fact]
    [Trait("Category", "PdfTests")]
    public void QuotationAndProductionPdf_AreCreatedWithPdfHeader()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NestLaserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var quotationPath = Path.Combine(tempDir, "quotation.pdf");
        var productionPath = Path.Combine(tempDir, "production.pdf");
        var layer = new LayerModel { Id = "cut", Name = "Cut", Type = LayerType.Cut, IsVisible = true };
        var part = new PartModel
        {
            Id = "p1",
            Name = "P1",
            LayerId = layer.Id,
            Geometry = GeometryUtils.CreateRectangle(100, 50)
        };
        var plate = new PlateModel { Width = 500, Height = 300 };
        var nestResult = new NestResult
        {
            Plates = new List<PlateModel> { plate },
            UsedArea = part.Area,
            Placed = new List<NestPlacement>
            {
                new()
                {
                    PartId = part.Id,
                    PartName = part.Name,
                    Part = part,
                    Width = part.Width,
                    Height = part.Height,
                    TransformedGeometry = part.Geometry.Clone()
                }
            }
        };
        var material = new MaterialProfile { Name = "Steel", ThicknessMm = 2, UnitPrice = 100 };
        var machine = new MachineProfile { Name = "Fiber" };
        var operations = new List<LaserOperation>
        {
            new() { LayerId = layer.Id, Type = OperationType.CutOuter, Speed = 60, SpeedUnit = SpeedUnit.MmPerMinute, Enabled = true }
        };
        var estimate = CostEstimationService.Calculate(
            "pdf-smoke",
            material,
            machine,
            new List<PartModel> { part },
            new List<LayerModel> { layer },
            operations,
            nestResult,
            plate.Width,
            plate.Height,
            new CostSettings { RoundFinalPrice = false });

        PdfReportService.CreateQuotationPdf(quotationPath, "PDF Smoke", new CompanyProfile(), material, machine, plate, nestResult, estimate, operations);
        PdfReportService.CreateProductionReportPdf(productionPath, "PDF Smoke", new CompanyProfile(), material, machine, plate, nestResult, estimate, operations);

        AssertPdf(quotationPath);
        AssertPdf(productionPath);
    }

    private static void AssertPdf(string path)
    {
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
        using var stream = File.OpenRead(path);
        var header = new byte[4];
        Assert.Equal(4, stream.Read(header, 0, header.Length));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(header));
    }
}
