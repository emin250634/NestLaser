using System.Collections.Generic;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class CostTests
{
    [Theory]
    [Trait("Category", "CostTests")]
    [InlineData(UnitType.PerSheet, 100, 0, 0, 1000, 2000, 2, 200)]
    [InlineData(UnitType.PerSquareMeter, 50, 0, 0, 1000, 2000, 2, 200)]
    [InlineData(UnitType.PerKg, 10, 1, 7850, 1000, 1000, 1, 78.5)]
    public void MaterialCost_ModesAreStable(
        UnitType unitType,
        double unitPrice,
        double thickness,
        double density,
        double plateWidth,
        double plateHeight,
        int plateCount,
        double expected)
    {
        var material = new MaterialProfile
        {
            UnitType = unitType,
            UnitPrice = unitPrice,
            ThicknessMm = thickness,
            Density = density
        };

        var actual = CostEstimationService.CalculateMaterialCost(material, plateWidth, plateHeight, plateCount);

        Assert.Equal(expected, actual, 3);
    }

    [Fact]
    [Trait("Category", "CostTests")]
    public void MultipleCutOperationsOnSameLayer_DoNotUseGlobalCutLengthForEachOperation()
    {
        var layer = new LayerModel { Id = "cut", Name = "Cut", Type = LayerType.Cut, IsVisible = true };
        var part = new PartModel
        {
            Id = "part-1",
            LayerId = layer.Id,
            Geometry = GeometryUtils.CreateRectangle(100, 50)
        };
        var operations = new List<LaserOperation>
        {
            new() { LayerId = layer.Id, Type = OperationType.CutOuter, Speed = 60, SpeedUnit = SpeedUnit.MmPerMinute, PassCount = 1, Enabled = true },
            new() { LayerId = layer.Id, Type = OperationType.CutInner, Speed = 60, SpeedUnit = SpeedUnit.MmPerMinute, PassCount = 1, Enabled = true }
        };

        var estimate = CostEstimationService.Calculate(
            "cost-regression",
            new MaterialProfile { UnitPrice = 0 },
            new MachineProfile(),
            new List<PartModel> { part },
            new List<LayerModel> { layer },
            operations,
            null,
            500,
            300,
            new CostSettings
            {
                RoundFinalPrice = false,
                IncludeConsumables = false,
                IncludeElectricityCost = false,
                IncludeOperatorCost = false,
                MachineHourlyRate = 0
            });

        Assert.Equal(600, estimate.TotalCutLengthMm, 6);
        Assert.Equal(10, estimate.EstimatedCutTimeMinutes, 6);
        Assert.Equal(10, estimate.TotalEstimatedTimeMinutes, 6);
    }
}
