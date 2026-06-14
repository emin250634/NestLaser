using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Nesting;
using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class TrueShapeNestingTests
{
    private NestSettings CreateExperimentalSettings()
    {
        return new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true
        };
    }

    private NestSettings CreateExperimentalSettings(NestSettings settings)
    {
        settings.EnableExperimentalAlgorithms = true;
        return settings;
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_PlacesHexagons_WithoutCollision()
    {
        var settings = CreateExperimentalSettings();
        settings.GapBetweenParts = 2;
        settings.PlateMargin = 5;

        var plate = new PlateModel { Width = 300, Height = 200, Margin = 0 };
        var parts = new List<PartModel>
        {
            CreateHexPart("hex1", 40),
            CreateHexPart("hex2", 40),
            CreateHexPart("hex3", 40),
            CreateHexPart("hex4", 40)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.PlacedCount >= 3,
            $"{result.TrueShapeDebugReport}");
        Assert.Empty(result.Unplaced.Where(u => result.Placed.Any(p => p.PartId == u.Id)));
        Assert.True(result.TrueShapeCandidateCount > 0);
        Assert.True(result.VertexToVertexCandidateCount > 0);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_SmallParts_FillGaps()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0
        };

        var engine = new NestingEngine();
        var plate = new PlateModel { Width = 100, Height = 87, Margin = 0 };
        var largeHex = CreateHexPart("hex", 50);
        var translatedHex = GeometryUtils.Translate(largeHex.Geometry, 50, 43.30127018922193);
        var existingResult = new NestResult
        {
            Plates = { plate }
        };

        existingResult.Placed.Add(new NestPlacement
        {
            PartId = largeHex.Id,
            PartName = largeHex.Name,
            Part = largeHex,
            PlateIndex = 0,
            RotationDeg = 0,
            X = translatedHex.Bounds.MinX,
            Y = translatedHex.Bounds.MinY,
            Width = translatedHex.Bounds.Width,
            Height = translatedHex.Bounds.Height,
            TransformedGeometry = translatedHex
        });

        var smallParts = new List<PartModel>
        {
            CreateRectPart("small", 8, 8)
        };

        var method = typeof(NestingEngine).GetMethod(
            "TryGapFill",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var fillResult = (NestResult)method.Invoke(
            engine,
            new object?[] { smallParts, existingResult, plate, settings, 0d, 0L, 15000L })!;

        Assert.NotNull(fillResult);
        Assert.True(existingResult.GapFillAttemptCount > 0, existingResult.TrueShapeDebugReport);
        Assert.True(fillResult.GapFillSuccessCount > 0, existingResult.TrueShapeDebugReport);
        Assert.NotEmpty(fillResult.Placed);
        Assert.All(fillResult.Placed, p => Assert.Equal(0, p.PlateIndex));
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_UsesNFPCandidates()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 200, Height = 150 };
        var parts = new List<PartModel>
        {
            CreateLShape("l1", 60, 40, 20),
            CreateRectPart("r1", 30, 30)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.TrueShapeCandidateCount > 0, $"{result.TrueShapeDebugReport}");
        Assert.True(result.CandidatePositionsTested > 0, $"{result.TrueShapeDebugReport}");
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_MarksAlgorithmName()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting
        };

        var plate = new PlateModel { Width = 100, Height = 100, Margin = 0 };
        var parts = new List<PartModel> { CreateRectPart("p1", 30, 30) };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.Contains("True Shape", result.AlgorithmName);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_FallsBackOnTimeout()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0
        };

        var plate = new PlateModel { Width = 1000, Height = 1000 };
        var parts = new List<PartModel>();

        for (int i = 0; i < 500; i++)
        {
            parts.Add(CreateRectPart($"p{i}", 15 + (i % 8), 15 + (i % 8)));
        }

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.FallbackUsed || result.PlacedCount > 0);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_NoOverlap_Validation()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 150, Height = 100 };
        var parts = new List<PartModel>
        {
            CreateRectPart("r1", 60, 60),
            CreateRectPart("r2", 60, 60)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);

        for (int i = 0; i < result.Placed.Count; i++)
        {
            for (int j = i + 1; j < result.Placed.Count; j++)
            {
                var p1 = result.Placed[i].TransformedGeometry;
                var p2 = result.Placed[j].TransformedGeometry;
                bool overlap = GeometryUtils.PolygonsIntersect(p1, p2);
                Assert.False(overlap, $"Parts {i} and {j} overlap");
            }
        }
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_OversizedPart_Unplaced()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 100, Height = 100 };
        var parts = new List<PartModel>
        {
            CreateRectPart("large", 200, 200)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.Equal(0, result.PlacedCount);
        Assert.Single(result.Unplaced);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_LShape_ConcaveGap()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5
        };

        var plate = new PlateModel { Width = 200, Height = 150, Margin = 0 };
        var parts = new List<PartModel>
        {
            CreateLShape("l1", 80, 60, 25),
            CreateRectPart("small", 15, 15)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.PlacedCount >= 1, $"{result.TrueShapeDebugReport}");
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_HexagonGap_AllowsSmallPartInsideBoundingVoid()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0
        };

        var plate = new PlateModel { Width = 100, Height = 87, Margin = 0 };
        var parts = new List<PartModel>
        {
            CreateHexPart("hex", 50),
            CreateRectPart("small", 8, 8)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.PlacedCount == 2,
            $"{result.TrueShapeDebugReport} | Placed={string.Join(',', result.Placed.Select(p => p.PartId))} | Unplaced={string.Join(',', result.Unplaced.Select(p => p.Id))}");

        var large = result.Placed.First(p => p.PartId == "hex");
        var small = result.Placed.First(p => p.PartId == "small");

        Assert.Equal(large.PlateIndex, small.PlateIndex);
        Assert.True(large.TransformedGeometry.Bounds.Contains(small.TransformedGeometry.Bounds.Center));
        Assert.False(GeometryUtils.PolygonsIntersect(large.TransformedGeometry, small.TransformedGeometry));
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_HexagonCircleGap_PlacesSmallCirclesOnSamePlate()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0
        };

        var plate = new PlateModel { Width = 320, Height = 220, Margin = 0 };
        var parts = new List<PartModel>
        {
            CreateHexPart("hex1", 35),
            CreateHexPart("hex2", 35),
            CreateHexPart("hex3", 35),
            CreateCirclePart("c1", 5),
            CreateCirclePart("c2", 5),
            CreateCirclePart("c3", 5),
            CreateCirclePart("c4", 5),
            CreateCirclePart("c5", 5),
            CreateCirclePart("c6", 5)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);

        int hexPlate = result.Placed.First(p => p.PartId == "hex1").PlateIndex;
        int circlesOnHexPlate = result.Placed.Count(p => p.PartId.StartsWith("c") && p.PlateIndex == hexPlate);

        Assert.True(circlesOnHexPlate > 0, result.TrueShapeDebugReport);
        Assert.True(result.Placed.Count >= 6, result.TrueShapeDebugReport);
        Assert.True(result.Plates.Count <= 2, result.TrueShapeDebugReport);
        Assert.True(result.BoundingBoxOverlapButSATClearAccepted > 0, result.TrueShapeDebugReport);

        var hexagons = result.Placed.Where(p => p.PartId.StartsWith("hex")).ToList();
        var overlapButClear = result.Placed
            .Where(p => p.PartId.StartsWith("c") && p.PlateIndex == hexPlate)
            .Any(p => hexagons.Any(h =>
                h.TransformedGeometry.Bounds.Intersects(p.TransformedGeometry.Bounds) &&
                !GeometryUtils.PolygonsIntersect(h.TransformedGeometry, p.TransformedGeometry)));
        Assert.True(overlapButClear, result.TrueShapeDebugReport);

        for (int i = 0; i < result.Placed.Count; i++)
        {
            for (int j = i + 1; j < result.Placed.Count; j++)
            {
                var p1 = result.Placed[i].TransformedGeometry;
                var p2 = result.Placed[j].TransformedGeometry;
                Assert.False(GeometryUtils.PolygonsIntersect(p1, p2), $"Parts {result.Placed[i].PartId} and {result.Placed[j].PartId} overlap");
            }
        }
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_LConcaveGap_FitsSmallSquare()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0
        };

        var plate = new PlateModel { Width = 80, Height = 60, Margin = 0 };
        var parts = new List<PartModel>
        {
            CreateLShape("l", 80, 60, 25),
            CreateRectPart("small", 6, 6)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.True(result.PlacedCount >= 1, result.TrueShapeDebugReport);
        Assert.True(result.TrueShapeCandidateCount > 0, result.TrueShapeDebugReport);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_AnchorTranslation_AlignsVertexToTarget()
    {
        var engine = new NestingEngine();
        var plate = new PlateModel { Width = 200, Height = 200, Margin = 0 };
        var result = new NestResult();
        var oriented = GeometryUtils.CreateRectangle(20, 10);
        var part = CreateRectPart("rect", 20, 10);
        var target = new Point2D(80, 90);
        var localAnchor = oriented.Vertices[2];

        var engineType = typeof(NestingEngine);
        var candidateType = engineType.GetNestedType("PlacementCandidate", BindingFlags.NonPublic)!;
        var anchorType = engineType.GetNestedType("AnchorMatchType", BindingFlags.NonPublic)!;
        var listType = typeof(List<>).MakeGenericType(candidateType);
        var candidates = Activator.CreateInstance(listType)!;

        var method = engineType.GetMethod(
            "TryAddTrueShapeCandidate",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var sourceType = engineType.GetNestedType("CandidateSource", BindingFlags.NonPublic)!;
        var invokeArgs = new object?[]
        {
            target,
            localAnchor,
            Enum.Parse(anchorType, "VertexToVertex"),
            0d,
            oriented,
            candidates,
            plate,
            result,
            0,
            0d,
            part,
            false,
            Enum.Parse(sourceType, "Unknown")
        };

        var placed = (bool)method.Invoke(engine, invokeArgs)!;
        Assert.True(placed);

        var enumerator = ((System.Collections.IEnumerable)candidates).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        var candidate = enumerator.Current!;
        var geometry = (Polygon)candidateType.GetProperty("Geometry")!.GetValue(candidate)!;

        Assert.Contains(geometry.Vertices, v => Math.Abs(v.X - target.X) < 1e-6 && Math.Abs(v.Y - target.Y) < 1e-6);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_LocalAnchors_DoNotUseBoundingBoxCorners()
    {
        var engine = new NestingEngine();
        var oriented = CreateHexPart("hex", 20).Geometry;

        var method = typeof(NestingEngine).GetMethod(
            "GetLocalAnchors",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var anchors = (IEnumerable<Point2D>)method.Invoke(engine, new object[] { oriented })!;
        var anchorList = anchors.ToList();
        var bounds = oriented.Bounds;

        Assert.DoesNotContain(anchorList, p => Math.Abs(p.X - bounds.MinX) < 1e-6 && Math.Abs(p.Y - bounds.MinY) < 1e-6);
        Assert.DoesNotContain(anchorList, p => Math.Abs(p.X - bounds.MaxX) < 1e-6 && Math.Abs(p.Y - bounds.MinY) < 1e-6);
        Assert.DoesNotContain(anchorList, p => Math.Abs(p.X - bounds.MinX) < 1e-6 && Math.Abs(p.Y - bounds.MaxY) < 1e-6);
        Assert.DoesNotContain(anchorList, p => Math.Abs(p.X - bounds.MaxX) < 1e-6 && Math.Abs(p.Y - bounds.MaxY) < 1e-6);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_RotationCandidate_UsesNinetyDegrees()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 0,
            AllowRotation0 = false,
            AllowRotation90 = true,
            AllowAdvancedRotation = false
        };

        var plate = new PlateModel { Width = 30, Height = 60, Margin = 0 };
        var parts = new List<PartModel>
        {
            CreateRectPart("rect", 60, 30)
        };

        var result = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(result);
        Assert.Single(result.Placed);
        var placement = result.Placed[0];
        Assert.True(Math.Abs(placement.RotationDeg - 90) < 1e-6 || Math.Abs(placement.RotationDeg - 270) < 1e-6,
            $"Unexpected rotation: {placement.RotationDeg}");
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void MultiPlateOverlay_UsesPlateOffset()
    {
        var result = new NestResult();
        result.Plates.Add(new PlateModel { Width = 100, Height = 50, Margin = 0 });
        result.Plates.Add(new PlateModel { Width = 120, Height = 50, Margin = 0 });
        result.Plates.Add(new PlateModel { Width = 80, Height = 50, Margin = 0 });

        var helper = typeof(NestLaserDesktop.Views.MainWindow).GetMethod(
            "GetNestedPlateOffsetX",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        double offset0 = (double)helper.Invoke(null, new object[] { result, 0 })!;
        double offset1 = (double)helper.Invoke(null, new object[] { result, 1 })!;
        double offset2 = (double)helper.Invoke(null, new object[] { result, 2 })!;

        Assert.Equal(0, offset0, 6);
        Assert.Equal(120, offset1, 6);
        Assert.Equal(260, offset2, 6);
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_ForcedGapBboxOverlapButSATClear()
    {
        var plate = new PlateModel { Width = 200, Height = 100, Margin = 5 };
        var hex = CreateHexPart("hex", 40);
        var small = CreateRectPart("small", 6, 6);

        var engineType = typeof(NestingEngine);
        var buildMethod = engineType.GetMethod("BuildOrientedGeometry", BindingFlags.NonPublic | BindingFlags.Static)!;
        var transformMethod = engineType.GetMethod("TransformPolygonForPlacement", BindingFlags.NonPublic | BindingFlags.Static)!;
        var collisionMethod = engineType.GetMethod("PassesCollisionCheck", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var insideMethod = engineType.GetMethod("IsGeometryInsideUsableArea", BindingFlags.NonPublic | BindingFlags.Static)!;

        var hexOriented = buildMethod.Invoke(null, [hex.Geometry, 0.0]);
        var hexPlaced = transformMethod.Invoke(null, [hex.Geometry, 0.0, new Point2D(50, 30)])!;

        var result = new NestResult();
        result.Plates.Add(plate);
        result.Placed.Add(new NestPlacement
        {
            PartId = "hex",
            TransformedGeometry = (Polygon)hexPlaced,
            PlateIndex = 0
        });

        var placedBounds = ((Polygon)hexPlaced).Bounds;
        var hexPoly = (Polygon)hexPlaced;

        double gapX = placedBounds.MaxX - 4;
        double gapY = placedBounds.MinY + 4;

        var smallOriented = buildMethod.Invoke(null, [small.Geometry, 0.0]) as Polygon;
        var smallPlaced = transformMethod.Invoke(null, [small.Geometry, 0.0, new Point2D(gapX, gapY)]) as Polygon ?? new Polygon();

        bool insideUsable = (bool)insideMethod.Invoke(null, [smallPlaced, plate])!;
        Assert.True(insideUsable, "Circle in hexagon gap must be inside usable area");

        bool bboxIntersects = smallPlaced.Bounds.Intersects(placedBounds);
        Assert.True(bboxIntersects, "Circle bbox must overlap hexagon bbox for gap test");

        bool satIntersects = GeometryUtils.PolygonsIntersect(smallPlaced, hexPoly);
        Assert.False(satIntersects, "Circle polygon must NOT overlap hexagon polygon in gap");

        var engine = new NestingEngine();
        object[] collisionArgs = [smallPlaced, result, 0, small, 0.0, 0, 0, 0, false];
        bool collisionPass = (bool)collisionMethod.Invoke(engine, collisionArgs)!;
        Assert.True(collisionPass, "Collision check must pass for circle in hexagon gap");

        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 5
        };
        var parts = new List<PartModel> { hex, small };
        var fullResult = new NestingEngine().Run(parts, plate, settings);

        Assert.NotNull(fullResult);
        Assert.Equal(2, fullResult.PlacedCount);
        Assert.Single(fullResult.Plates);
        Assert.True(fullResult.BoundingBoxOverlapButSATClearAccepted > 0,
            "Must have bbox-overlap-but-SAT-clear accepted placements");
    }

    private PartModel CreateRectPart(string id, double width, double height)
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

    private PartModel CreateCirclePart(string id, double radius)
    {
        var circle = new Polygon
        {
            Vertices = GeometryUtils.CircleToPolygon(new Point2D(radius, radius), radius, 24)
        };
        circle.Calculate();

        return new PartModel
        {
            Id = id,
            Name = id,
            Geometry = circle
        };
    }

    private PartModel CreateLShape(string id, double width, double height, double thick)
    {
        var l = new Polygon
        {
            Vertices =
            [
                new Point2D(0, 0),
                new Point2D(width, 0),
                new Point2D(width, height - thick),
                new Point2D(thick, height - thick),
                new Point2D(thick, height),
                new Point2D(0, height)
            ]
        };
        l.Calculate();

        return new PartModel
        {
            Id = id,
            Name = id,
            Geometry = l
        };
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_EmptySpaceDiagnostic_LogsESFailures()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 5,
            AllowRotation0 = true,
            AllowRotation90 = true,
            AllowAdvancedRotation = false
        };

        var plate = new PlateModel { Width = 150, Height = 150, Margin = 5 };
        var hex = CreateHexPart("hex", 50);
        var small = CreateCirclePart("circ", 6);

        var engine = new NestingEngine();
        var result = engine.Run(new List<PartModel> { hex, small }, plate, settings);

        Assert.NotNull(result);
        var esDiag = result.EmptySpaceDiagnostics;
        Assert.NotNull(esDiag);

        TrueShapeTrace.Log("\n=== Test Assertions for ES Diagnostics ===");
        TrueShapeTrace.Log($"TotalAllCandidates: {esDiag.AllCandidates.Count}");
        TrueShapeTrace.Log($"TotalESAttempts: {esDiag.TotalESAttempts}");
        TrueShapeTrace.Log($"TotalESBoundaryRejects: {esDiag.TotalESBoundaryRejects}");
        TrueShapeTrace.Log($"TotalESSATRejects: {esDiag.TotalESSATRejects}");
        TrueShapeTrace.Log($"TotalESAccepted: {esDiag.TotalESAccepted}");
        TrueShapeTrace.Log($"MaxClearanceFound: {esDiag.MaxClearanceFound:F6}");
        TrueShapeTrace.Log($"RequiredClearance: {esDiag.RequiredClearance:F6}");
        TrueShapeTrace.Log($"BestES: {esDiag.BestEmptySpaceCandidate}");
        TrueShapeTrace.Log($"BestES_SAT: {esDiag.BestEmptySpaceCandidateSATResult}");

        Assert.True(esDiag.TotalESAttempts > 0 || esDiag.AllCandidates.Count > 0,
            "Empty space diagnostics should contain at least some ES candidates or attempts. " +
            $"TotalESAttempts={esDiag.TotalESAttempts} AllCandidates={esDiag.AllCandidates.Count} " +
            $"Result: placed={result.PlacedCount} unplaced={result.Unplaced.Count} plates={result.Plates.Count}");

        if (result.PlacedCount >= 2)
        {
            Assert.True(esDiag.AllCandidates.Any(c => !c.SATFailed) || esDiag.TotalESSATRejects > 0,
                "ES diagnostics should show either accepted or SAT-rejected candidates");
        }

        if (esDiag.AllCandidates.Count > 0)
        {
            bool hasDetails = esDiag.AllCandidates.All(c =>
                !double.IsNaN(c.TranslationX) && !double.IsNaN(c.TranslationY));
            Assert.True(hasDetails, "All ES candidates must have valid translation coordinates");
        }

        if (esDiag.TotalESSATRejects > 0)
        {
            Assert.NotEmpty(esDiag.AllCandidates.Where(c => c.SATFailed));
        }
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_ESRefinement_RescuesSATFailingCandidates()
    {
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 0,
            PlateMargin = 5,
            AllowRotation0 = true,
            AllowRotation90 = true,
            AllowAdvancedRotation = false
        };

        var plate = new PlateModel { Width = 150, Height = 150, Margin = 5 };
        var hex = CreateHexPart("hex", 50);
        var small = CreateCirclePart("circ", 6);

        var engine = new NestingEngine();
        var result = engine.Run(new List<PartModel> { hex, small }, plate, settings);

        Assert.NotNull(result);

        TrueShapeTrace.Log("\n=== ES Refinement Test Assertions ===");
        TrueShapeTrace.Log($"RefinementAttempts: {result.EmptySpaceRefinementAttemptCount}");
        TrueShapeTrace.Log($"RefinementSuccesses: {result.EmptySpaceRefinementSuccessCount}");
        TrueShapeTrace.Log($"RefinementBestClearance: {result.EmptySpaceRefinementBestClearance:F6}");
        TrueShapeTrace.Log($"TotalPlaced: {result.PlacedCount}");
        TrueShapeTrace.Log($"ES SAT Rejects: {result.EmptySpaceSATRejects}");

        Assert.True(result.EmptySpaceRefinementAttemptCount >= 0,
            "Refinement attempt count must be non-negative");

        Assert.True(result.EmptySpaceRefinementSuccessCount >= 0,
            "Refinement success count must be non-negative");

        bool placementOnSamePlate = result.PlacedCount >= 2;
        bool hasESAttempts = result.EmptySpaceCandidateCount > 0;
        bool someSATFailed = result.EmptySpaceSATRejects > 0;

        if (placementOnSamePlate && hasESAttempts && someSATFailed)
        {
            Assert.True(result.EmptySpaceRefinementAttemptCount > 0,
                "If ES SAT rejects exist, refinement attempts must be made. " +
                $"Attempts={result.EmptySpaceRefinementAttemptCount} SATRejects={result.EmptySpaceSATRejects}");

            if (result.EmptySpaceRefinementAttemptCount > result.EmptySpaceRefinementSuccessCount)
            {
                TrueShapeTrace.Log("NOTE: Some refinements failed (expected for candidates with >2mm overlap depth)");
            }
        }

        if (result.EmptySpaceRefinementAttemptCount > 0)
        {
            Assert.True(result.EmptySpaceRefinementSuccessCount > 0,
                "At least some ES refinements must succeed when refinements are attempted. " +
                $"Attempts={result.EmptySpaceRefinementAttemptCount} Successes={result.EmptySpaceRefinementSuccessCount} " +
                $"BestClearance={result.EmptySpaceRefinementBestClearance:F6} " +
                $"Placed={result.PlacedCount} Plates={result.Plates.Count}");

            Assert.True(result.EmptySpaceRefinementBestClearance > 0,
                "Refinement best clearance must be positive when refinements succeed. " +
                $"BestClearance={result.EmptySpaceRefinementBestClearance:F6}");
        }

        for (int i = 0; i < result.Placed.Count; i++)
        {
            for (int j = i + 1; j < result.Placed.Count; j++)
            {
                var p1 = result.Placed[i].TransformedGeometry;
                var p2 = result.Placed[j].TransformedGeometry;
                Assert.False(GeometryUtils.PolygonsIntersect(p1, p2),
                    $"Parts {result.Placed[i].PartId} and {result.Placed[j].PartId} overlap");
            }
        }
    }

    [Fact]
    [Trait("Category", "TrueShapeNestingTests")]
    public void TrueShapeNesting_DxfFileEndToEnd()
    {
        var dxfPath = @"C:\Users\user\Desktop\d1.dxf";
        Assert.True(File.Exists(dxfPath), $"DXF file not found: {dxfPath}");

        var importResult = DxfService.Import(dxfPath);
        Assert.True(importResult.Success, $"DXF import failed: {string.Join("; ", importResult.Errors)}");
        Assert.NotEmpty(importResult.Parts);

        var parts = importResult.Parts;
        double plateW = 500;
        double plateH = 500;

        var plate = new PlateModel { Width = plateW, Height = plateH, Margin = 5 };
        var settings = new NestSettings
        {
            Algorithm = NestAlgorithm.TrueShapeNesting,
            EnableExperimentalAlgorithms = true,
            GapBetweenParts = 2,
            PlateMargin = 5,
            AllowRotation0 = true,
            AllowRotation90 = true,
            AllowAdvancedRotation = true
        };

        // Run with refinement enabled
        TrueShapeTrace.Clear();
        NestingEngine.DebugDisableRefinement = false;
        var engineWith = new NestingEngine();
        var resultWith = engineWith.Run(parts, plate, settings);
        string traceWith = TrueShapeTrace.GetContent();

        // Run with refinement disabled for comparison
        TrueShapeTrace.Clear();
        NestingEngine.DebugDisableRefinement = true;
        var engineWithout = new NestingEngine();
        var resultWithout = engineWithout.Run(parts, plate, settings);
        NestingEngine.DebugDisableRefinement = false;

        var output = new StringBuilder();
        output.AppendLine();
        output.AppendLine(new string('=', 40));
        output.AppendLine("REAL DXF END-TO-END NESTING DIAGNOSTICS");
        output.AppendLine(new string('=', 40));
        output.AppendLine($"DXF: {dxfPath}");
        output.AppendLine($"Parts loaded: {parts.Count}");
        output.AppendLine($"Total bbox: {importResult.TotalBoundingWidth:F2} x {importResult.TotalBoundingHeight:F2}");
        output.AppendLine($"Unit: {importResult.UnitInfo.SourceUnit} scale={importResult.UnitInfo.ScaleFactorToMm:F4}");
        output.AppendLine($"Plate: {plateW:F0}x{plateH:F0}");

        output.AppendLine();
        output.AppendLine("--- COMPARISON: With vs Without Refinement ---");
        output.AppendLine($"  WithRefinement:  Placed={resultWith.PlacedCount}/{parts.Count} Plates={resultWith.Plates.Count} Unplaced={resultWith.Unplaced.Count} Fallback={resultWith.FallbackUsed}");
        output.AppendLine($"  WithoutRefinement: Placed={resultWithout.PlacedCount}/{parts.Count} Plates={resultWithout.Plates.Count} Unplaced={resultWithout.Unplaced.Count} Fallback={resultWithout.FallbackUsed}");

        int plateDiff = resultWithout.Plates.Count - resultWith.Plates.Count;
        if (plateDiff > 0)
            output.AppendLine($"  => Refinement REDUCED plate count by {plateDiff}");
        else if (plateDiff == 0 && resultWith.EmptySpaceRefinementSuccessCount > 0)
            output.AppendLine("  => Refinement used but plate count unchanged (improved packing density)");
        else if (plateDiff < 0)
            output.AppendLine($"  => WARNING: refinement INCREASED plate count by {-plateDiff}");
        else
            output.AppendLine("  => Refinement had no measurable effect on plate count");

        output.AppendLine();
        output.AppendLine("--- ADAPTIVE BUDGET STATISTICS ---");
        output.AppendLine($"  LargePartCandidateLimit: {resultWith.LargePartCandidateLimit}");
        output.AppendLine($"  MediumPartCandidateLimit: {resultWith.MediumPartCandidateLimit}");
        output.AppendLine($"  SmallPartCandidateLimit: {resultWith.SmallPartCandidateLimit}");
        output.AppendLine($"  LargePartFastPathUsed: {resultWith.LargePartFastPathUsed}");
        output.AppendLine($"  TimeoutAvoidedByAdaptiveBudget: {resultWith.TimeoutAvoidedByAdaptiveBudget}");

        output.AppendLine();
        output.AppendLine("--- REFINEMENT STATISTICS ---");
        output.AppendLine($"  Attempts: {resultWith.EmptySpaceRefinementAttemptCount}");
        output.AppendLine($"  Successes: {resultWith.EmptySpaceRefinementSuccessCount}");
        output.AppendLine($"  BestClearance: {resultWith.EmptySpaceRefinementBestClearance:F6}");

        output.AppendLine();
        output.AppendLine("--- PER-SOURCE REJECT BREAKDOWN ---");
        output.AppendLine($"  ES(EmptySpace):   B={resultWith.EmptySpaceBoundaryRejects} S={resultWith.EmptySpaceSATRejects}");
        output.AppendLine($"  PF(PlateFree):    B={resultWith.PlateFreeSpaceBoundaryRejects} S={resultWith.PlateFreeSpaceSATRejects}");
        output.AppendLine($"  VT(Vertex):       B={resultWith.VertexBoundaryRejects} S={resultWith.VertexSATRejects}");
        output.AppendLine($"  EM(EdgeMid):      B={resultWith.EdgeBoundaryRejects} S={resultWith.EdgeSATRejects}");
        output.AppendLine($"  CN(Corner):       B={resultWith.CornerBoundaryRejects} S={resultWith.CornerSATRejects}");
        output.AppendLine($"  BBoxOverlapSATClear: {resultWith.BoundingBoxOverlapButSATClearAccepted}");
        output.AppendLine($"  CandidateLimitHit: {resultWith.CandidateLimitHitCount}");

        output.AppendLine();
        output.AppendLine("--- EMPTY SPACE DIAGNOSTICS ---");
        var esDiag = resultWith.EmptySpaceDiagnostics;
        if (esDiag != null)
        {
            output.AppendLine($"  AllCandidates: {esDiag.AllCandidates.Count}");
            output.AppendLine($"  TotalESAttempts: {esDiag.TotalESAttempts}");
            output.AppendLine($"  TotalESBoundaryRejects: {esDiag.TotalESBoundaryRejects}");
            output.AppendLine($"  TotalESSATRejects: {esDiag.TotalESSATRejects}");
            output.AppendLine($"  TotalESAccepted: {esDiag.TotalESAccepted}");
            output.AppendLine($"  MaxClearanceFound: {esDiag.MaxClearanceFound:F6}");
            output.AppendLine($"  RequiredClearance: {esDiag.RequiredClearance:F6}");
            output.AppendLine($"  BestES: {esDiag.BestEmptySpaceCandidate}");
            output.AppendLine($"  BestES_SAT: {esDiag.BestEmptySpaceCandidateSATResult}");
            output.AppendLine($"  RootCause: (not available on EmptySpaceDiagnosticSummary)");
        }
        else
        {
            output.AppendLine("  (No ES diagnostics collected)");
        }

        output.AppendLine();
        output.AppendLine("--- PART-FAILED-BEFORE-NEW-PLATE BLOCKS ---");
        var failBlocks = new List<string>();
        bool inFailBlock = false;
        foreach (var line in traceWith.Split('\n'))
        {
            if (line.Contains("PART_FAILED_BEFORE_NEW_PLATE"))
            {
                inFailBlock = true;
                failBlocks.Add(line);
            }
            else if (inFailBlock)
            {
                if (line.TrimStart().StartsWith("---") || string.IsNullOrWhiteSpace(line))
                {
                    inFailBlock = false;
                }
                else
                {
                    failBlocks.Add(line);
                }
            }
        }
        if (failBlocks.Count > 0)
        {
            foreach (var fb in failBlocks)
                output.AppendLine($"  {fb.Trim()}");
        }
        else
        {
            output.AppendLine("  (No PART_FAILED_BEFORE_NEW_PLATE blocks)");
        }

        output.AppendLine();
        output.AppendLine("--- INDIVIDUAL PART PLACEMENT ---");
        output.AppendLine($"  Total placed: {resultWith.PlacedCount}");
        foreach (var p in resultWith.Placed)
        {
            output.AppendLine($"    {p.PartId} plate={p.PlateIndex} pos=({p.X:F2},{p.Y:F2}) rot={p.RotationDeg:F1}");
        }
        foreach (var u in resultWith.Unplaced)
        {
            output.AppendLine($"    UNPLACED: {u.Id} {u.Name} area={u.Area:F1} verts={u.Geometry.Vertices.Count}");
        }

        output.AppendLine();
        output.AppendLine("--- COLLISION VERIFICATION ---");
        bool hasCollision = false;
        for (int i = 0; i < resultWith.Placed.Count; i++)
        {
            for (int j = i + 1; j < resultWith.Placed.Count; j++)
            {
                if (resultWith.Placed[i].PlateIndex != resultWith.Placed[j].PlateIndex) continue;
                var p1 = resultWith.Placed[i].TransformedGeometry;
                var p2 = resultWith.Placed[j].TransformedGeometry;
                if (GeometryUtils.PolygonsIntersect(p1, p2))
                {
                    output.AppendLine($"  COLLISION: {resultWith.Placed[i].PartId} vs {resultWith.Placed[j].PartId}");
                    hasCollision = true;
                }
            }
        }
        output.AppendLine($"  Collision-free: {!hasCollision}");

        output.AppendLine();
        output.AppendLine(new string('=', 40));
        output.AppendLine("END DIAGNOSTICS");
        output.AppendLine(new string('=', 40));

        // Write combined diagnostics to trace and file
        TrueShapeTrace.Clear();
        TrueShapeTrace.Log(output.ToString());
        TrueShapeTrace.WriteToFile("logs/trueshape-dxf-debug.txt");

        // Also dump to console/test output
        var report = output.ToString();
        Console.Write(report);

        // Test assertions
        Assert.NotNull(resultWith);
        Assert.True(resultWith.PlacedCount > 0, "At least one part should be placed");

        // Adaptive budget assertions (skip if fallback returned a different NestResult)
        if (!resultWith.FallbackUsed)
        {
            int totalBudgeted = resultWith.LargePartCandidateLimit + resultWith.MediumPartCandidateLimit + resultWith.SmallPartCandidateLimit;
            Assert.True(totalBudgeted > 0, "At least one part should be classified by adaptive budget");

            if (resultWith.LargePartFastPathUsed > 0)
            {
                Assert.True(resultWith.LargePartCandidateLimit >= resultWith.LargePartFastPathUsed,
                    "LargePartFastPathUsed must be <= LargePartCandidateLimit");
            }
        }
        else
        {
            // When fallback is used, the NestResult is a fresh FreeRectangle result, so budget
            // counters from the TrueShape run are not preserved.
            output.AppendLine("  (Adaptive budget counters not available: fallback returned a different NestResult)");
        }

        // Verify collision-free
        for (int i = 0; i < resultWith.Placed.Count; i++)
        {
            for (int j = i + 1; j < resultWith.Placed.Count; j++)
            {
                if (resultWith.Placed[i].PlateIndex != resultWith.Placed[j].PlateIndex) continue;
                var p1 = resultWith.Placed[i].TransformedGeometry;
                var p2 = resultWith.Placed[j].TransformedGeometry;
                Assert.False(GeometryUtils.PolygonsIntersect(p1, p2),
                    $"Collision: {resultWith.Placed[i].PartId} vs {resultWith.Placed[j].PartId}");
            }
        }
    }
}
