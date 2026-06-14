using System.Collections.ObjectModel;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.ViewModels;

namespace NestLaserDesktop.Tests;

public class SnapAndMoveTests
{
    [Fact]
    public void NestedSelectionBounds_UseTransformedGeometry()
    {
        var vm = new MainViewModel();
        var part = CreateRectPart("p1", 20, 10);
        vm.Parts = new ObservableCollection<PartModel> { part };

        var placementGeometry = GeometryUtils.Translate(part.Geometry, 100, 50);
        vm.NestResult = new NestResult
        {
            Plates = { new PlateModel { Width = 300, Height = 200, Margin = 0 } },
            Placed =
            {
                new NestPlacement
                {
                    PartId = part.Id,
                    PartName = part.Name,
                    Part = part,
                    PlateIndex = 0,
                    RotationDeg = 0,
                    X = placementGeometry.Bounds.MinX,
                    Y = placementGeometry.Bounds.MinY,
                    Width = placementGeometry.Bounds.Width,
                    Height = placementGeometry.Bounds.Height,
                    TransformedGeometry = placementGeometry
                }
            }
        };

        vm.SelectPart(part);

        var bounds = vm.GetSelectionBoundsPublic();
        Assert.Equal(100, bounds.MinX, 6);
        Assert.Equal(50, bounds.MinY, 6);
        Assert.Equal(120, bounds.MaxX, 6);
        Assert.Equal(60, bounds.MaxY, 6);
    }

    [Fact]
    public void NestedMove_UpdatesMatchingPlacementByPartId()
    {
        var vm = new MainViewModel();
        var part = CreateRectPart("p1", 20, 10);
        vm.Parts = new ObservableCollection<PartModel> { part };

        var placementGeometry = GeometryUtils.Translate(part.Geometry, 100, 50);
        vm.NestResult = new NestResult
        {
            Plates = { new PlateModel { Width = 300, Height = 200, Margin = 0 } },
            Placed =
            {
                new NestPlacement
                {
                    PartId = part.Id,
                    PartName = part.Name,
                    Part = part,
                    PlateIndex = 0,
                    RotationDeg = 0,
                    X = placementGeometry.Bounds.MinX,
                    Y = placementGeometry.Bounds.MinY,
                    Width = placementGeometry.Bounds.Width,
                    Height = placementGeometry.Bounds.Height,
                    TransformedGeometry = placementGeometry
                }
            }
        };

        vm.SelectPart(part);
        vm.MoveSelected(15, -5, false);

        var placement = vm.NestResult!.Placed.Single(p => p.PartId == part.Id);
        Assert.Equal(115, placement.TransformedGeometry.Bounds.MinX, 6);
        Assert.Equal(45, placement.TransformedGeometry.Bounds.MinY, 6);
        Assert.Equal(15, part.Geometry.Bounds.MinX, 6);
        Assert.Equal(-5, part.Geometry.Bounds.MinY, 6);
    }

    [Fact]
    public void NestedMove_CrossesPlateBoundary_RehomesPlacementToTargetPlate()
    {
        var vm = new MainViewModel();
        var part = CreateRectPart("p1", 20, 10);
        vm.Parts = new ObservableCollection<PartModel> { part };

        var placementGeometry = GeometryUtils.Translate(part.Geometry, 100, 50);
        vm.NestResult = new NestResult
        {
            Plates =
            {
                new PlateModel { Width = 300, Height = 200, Margin = 0 },
                new PlateModel { Width = 300, Height = 200, Margin = 0 }
            },
            Placed =
            {
                new NestPlacement
                {
                    PartId = part.Id,
                    PartName = part.Name,
                    Part = part,
                    PlateIndex = 0,
                    RotationDeg = 0,
                    X = placementGeometry.Bounds.MinX,
                    Y = placementGeometry.Bounds.MinY,
                    Width = placementGeometry.Bounds.Width,
                    Height = placementGeometry.Bounds.Height,
                    TransformedGeometry = placementGeometry
                }
            }
        };

        vm.SelectPart(part);
        vm.MoveSelected(250, 0, false);

        var placement = vm.NestResult!.Placed.Single(p => p.PartId == part.Id);
        Assert.Equal(1, placement.PlateIndex);
        Assert.Equal(30, placement.TransformedGeometry.Bounds.MinX, 6);
        Assert.Equal(350, placement.TransformedGeometry.Bounds.MinX + 320, 6);

        var bounds = vm.GetSelectionBoundsPublic();
        Assert.Equal(30, bounds.MinX, 6);
    }

    private static PartModel CreateRectPart(string id, double width, double height)
    {
        return new PartModel
        {
            Id = id,
            Name = id,
            Geometry = GeometryUtils.CreateRectangle(width, height)
        };
    }
}
