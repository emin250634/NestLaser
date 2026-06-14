using NestLaserDesktop.Models;
using Xunit;

namespace NestLaserDesktop.Tests;

public class OperationTests
{
    [Fact]
    [Trait("Category", "OperationTests")]
    public void OperationClone_PreservesReportAndCostFields()
    {
        var operation = new LaserOperation
        {
            Id = "op-1",
            Name = "Outer Cut",
            LayerId = "layer-1",
            LayerName = "Cut",
            Color = "#00AAFF",
            Type = OperationType.CutOuter,
            Power = 85,
            Speed = 150,
            SpeedUnit = SpeedUnit.MmPerMinute,
            PassCount = 3,
            Priority = 2,
            Enabled = false
        };

        var clone = operation.Clone();

        Assert.Equal(operation.Id, clone.Id);
        Assert.Equal(operation.Name, clone.Name);
        Assert.Equal(operation.LayerId, clone.LayerId);
        Assert.Equal(operation.LayerName, clone.LayerName);
        Assert.Equal(operation.Color, clone.Color);
        Assert.Equal(operation.Type, clone.Type);
        Assert.Equal(operation.Power, clone.Power);
        Assert.Equal(operation.Speed, clone.Speed);
        Assert.Equal(operation.SpeedUnit, clone.SpeedUnit);
        Assert.Equal(operation.PassCount, clone.PassCount);
        Assert.Equal(operation.Priority, clone.Priority);
        Assert.Equal(operation.Enabled, clone.Enabled);
    }
}
