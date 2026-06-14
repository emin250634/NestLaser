using NestLaserDesktop.Models;
using Xunit;

namespace NestLaserDesktop.Tests;

public class LayerTests
{
    [Fact]
    [Trait("Category", "LayerTests")]
    public void LayerClone_PreservesWorkflowFlags()
    {
        var layer = new LayerModel
        {
            Id = "layer-1",
            Name = "Engrave",
            Type = LayerType.Engrave,
            Color = "#FF00AA",
            IsVisible = false,
            IsLocked = true,
            Power = 35,
            Speed = 120,
            PassCount = 2,
            Order = 3
        };

        var clone = layer.Clone();

        Assert.Equal(layer.Id, clone.Id);
        Assert.Equal(layer.Name, clone.Name);
        Assert.Equal(layer.Type, clone.Type);
        Assert.Equal(layer.Color, clone.Color);
        Assert.Equal(layer.IsVisible, clone.IsVisible);
        Assert.Equal(layer.IsLocked, clone.IsLocked);
        Assert.Equal(layer.Power, clone.Power);
        Assert.Equal(layer.Speed, clone.Speed);
        Assert.Equal(layer.PassCount, clone.PassCount);
        Assert.Equal(layer.Order, clone.Order);
    }
}
