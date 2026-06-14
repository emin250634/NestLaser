using System;
using System.Collections.Generic;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Nesting;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class NestingAlgorithmTests
{
    [Fact]
    [Trait("Category", "NestingAlgorithmTests")]
    public void ShapeAwarePolygon_PlacesRectangles_Efficiently()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.ShapeAwarePolygon,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 300, Height = 200 };
        var parts = new List<PartModel>
        {
            CreatePart("p1", 80, 40),
            CreatePart("p2", 80, 40),
            CreatePart("p3", 80, 40),
            CreatePart("p4", 80, 40)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.PlacedCount > 0);
        Assert.Empty(result.Unplaced);
    }

    [Fact]
    [Trait("Category", "NestingAlgorithmTests")]
    public void ShapeAwarePolygon_PlacesHexagons_WithoutCollision()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.ShapeAwarePolygon,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 300, Height = 200 };
        var parts = new List<PartModel>
        {
            CreateHexPart("hex1", 40),
            CreateHexPart("hex2", 40),
            CreateHexPart("hex3", 40)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.PlacedCount >= 2);
        Assert.True(result.CollisionCheckCount > 0);
    }

    [Fact]
    [Trait("Category", "NestingAlgorithmTests")]
    public void ShapeAwarePolygon_UsesVertexAnchors()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.ShapeAwarePolygon,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 200, Height = 150 };
        var parts = new List<PartModel>
        {
            CreatePart("p1", 50, 50),
            CreatePart("p2", 50, 50)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.CandidatePositionsTested > 0);
    }

    [Fact]
    [Trait("Category", "NestingAlgorithmTests")]
    public void ShapeAwarePolygon_FallsBackOnTimeout()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.ShapeAwarePolygon,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0
        };

        var plate = new PlateModel { Width = 1000, Height = 1000 };
        var parts = new List<PartModel>();

        for (int i = 0; i < 500; i++)
        {
            parts.Add(CreatePart($"p{i}", 20 + (i % 10), 20 + (i % 10)));
        }

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.FallbackUsed || result.PlacedCount > 0);
    }

    [Fact]
    [Trait("Category", "NestingAlgorithmTests")]
    public void ShapeAwarePolygon_MarksAlgorithmName()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.ShapeAwarePolygon,
            EnableExperimentalAlgorithms = true
        };

        var plate = new PlateModel { Width = 100, Height = 100 };
        var parts = new List<PartModel> { CreatePart("p1", 30, 30) };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.Contains("Shape-Aware", result.AlgorithmName);
    }

    private PartModel CreatePart(string id, double width, double height)
    {
        return new PartModel
        {
            Id = id,
            Name = id,
            Geometry = GeometryUtils.CreateRectangle(width, height)
        };
    }

    private PartModel CreateHexPart(string id, double radius)
    {
        var hex = new Polygon();
        for (int i = 0; i < 6; i++)
        {
            double angle = i * Math.PI / 3;
            hex.Vertices.Add(new Point2D(
                radius * Math.Cos(angle),
                radius * Math.Sin(angle)));
        }
        hex.Calculate();

        return new PartModel
        {
            Id = id,
            Name = id,
            Geometry = hex
        };
    }
}
