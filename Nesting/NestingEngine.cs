﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public class NestingEngine
{
    public static bool DebugForceExistingPlateMode { get; set; } = false;
    public static bool DebugDisableRefinement { get; set; } = false;

    private List<FreeRect>? _lastFreeRects;
    private readonly Dictionary<(string partId, double rot, int x, int y, int otherIdx), bool> _collisionCache = new();
    private readonly Stopwatch _stopwatch = new();
    private const long DefaultTimeoutMs = 10000; // 10 seconds timeout per algorithm attempt
    private const int MaxCandidatesPerPart = 200;

    public NestResult Run(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        _stopwatch.Restart();
        _collisionCache.Clear();

        NestResult result;

        switch (settings.Algorithm)
        {
            case NestAlgorithm.ShapeAwarePolygon:
                result = RunShapeAware(parts, plate, settings);
                if (result.FallbackUsed)
                {
                    result.FallbackUsed = true;
                    result.Warnings.Add("Shape-Aware Polygon timed out, fallback to Free Rectangle used.");
                    result.AlgorithmUsed = "Shape-Aware Polygon (Fallback)";
                    return result;
                }
                result.AlgorithmUsed = "Shape-Aware Polygon";
                break;

            case NestAlgorithm.TrueShapeNesting:
                result = RunTrueShapeNesting(parts, plate, settings);
                if (result.FallbackUsed)
                {
                    result.FallbackUsed = true;
                    result.Warnings.Add("True Shape Nesting timed out, fallback to Free Rectangle used.");
                    result.AlgorithmUsed = "True Shape Nesting (Fallback)";
                    return result;
                }
                result.AlgorithmUsed = "True Shape Nesting";
                break;

            case NestAlgorithm.IrregularExperimental:
                result = RunIrregular(parts, plate, settings);
                if (result.FallbackUsed || result.IsTimeout || result.PlacedCount < parts.Count * 0.1)
                {
                    var fallbackResult = RunFreeRectangle(parts, plate, settings);
                    fallbackResult.FallbackUsed = true;
                    fallbackResult.Warnings.Add("Irregular nesting unsuccessful or timed out, fallback to Free Rectangle used.");
                    fallbackResult.AlgorithmUsed = "Free Rectangle (Fallback)";
                    return fallbackResult;
                }
                result.AlgorithmUsed = "Irregular Experimental";
                break;

            case NestAlgorithm.PolygonCollision:
                result = RunPolygonCollision(parts, plate, settings);
                result.AlgorithmUsed = "Polygon Collision";
                break;

            case NestAlgorithm.FreeRectangle:
            default:
                result = RunFreeRectangle(parts, plate, settings);
                result.AlgorithmUsed = "Free Rectangle";
                break;
        }

        _stopwatch.Stop();
        result.NestingTimeMs = _stopwatch.ElapsedMilliseconds;
        return result;
    }

    private NestResult RunFreeRectangle(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        var result = new NestResult
        {
            AlgorithmName = "Free Rectangle (Guillotine)",
            PolygonCollisionEnabled = false
        };

        if (parts.Count == 0) return result;

        var sorted = parts
            .OrderByDescending(p => p.Area)
            .ThenByDescending(p => Math.Max(p.Width, p.Height))
            .ToList();

        var currentPlate = ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var freeRects = InitFreeRects(currentPlate);

        foreach (var part in sorted)
        {
            bool placed = TryPlaceFreeRect(part, settings, result, currentPlate, freeRects);

            if (!placed)
            {
                currentPlate = ClonePlate(plate);
                result.Plates.Add(currentPlate);
                freeRects = InitFreeRects(currentPlate);
                placed = TryPlaceFreeRect(part, settings, result, currentPlate, freeRects);
            }

            if (!placed)
                result.Unplaced.Add(part);
        }

        FinalizeResult(result, freeRects);
        return result;
    }

    private bool TryPlaceFreeRect(PartModel part, NestSettings settings, NestResult result,
        PlateModel plate, List<FreeRect> freeRects)
    {
        double gap = Math.Max(0, settings.GapBetweenParts);
        int plateIndex = result.Plates.IndexOf(plate);

        var candidates = new List<PlacementCandidate>();

        foreach (var rect in freeRects)
        {
            foreach (double rotation in GetAllowedRotations(settings))
            {
                var oriented = BuildOrientedGeometry(part.Geometry, rotation);
                var bounds = oriented.Bounds;
                double fitW = bounds.Width + gap;
                double fitH = bounds.Height + gap;

                if (fitW > rect.W + 1e-6 || fitH > rect.H + 1e-6)
                    continue;

                double x = rect.X + plate.Margin;
                double y = rect.Y + plate.Margin;
                var translated = GeometryUtils.Translate(oriented, x, y);

                if (!IsGeometryInsideUsableArea(translated, plate))
                    continue;

                double score = rect.Y * 1000000 + rect.X; // Simple bottom-left

                candidates.Add(new PlacementCandidate
                {
                    Rect = rect,
                    Geometry = translated,
                    RotationDeg = rotation,
                    FitW = fitW,
                    FitH = fitH,
                    Score = score
                });
            }
        }

        if (candidates.Count == 0) return false;

        var best = candidates.OrderBy(c => c.Score).First();

        var placement = new NestPlacement
        {
            PartId = part.Id,
            PartName = part.Name,
            Part = part,
            X = best.Geometry.Bounds.MinX,
            Y = best.Geometry.Bounds.MinY,
            RotationDeg = best.RotationDeg,
            PlateIndex = plateIndex,
            Width = best.FitW - gap,
            Height = best.FitH - gap,
            PlacementTranslationX = best.Geometry.Bounds.MinX,
            PlacementTranslationY = best.Geometry.Bounds.MinY,
            PlacementScore = best.Score,
            TransformedGeometry = best.Geometry
        };

        result.Placed.Add(placement);
        result.UsedArea += part.Area;

        SplitFreeRects(freeRects, best.Rect, best.FitW, best.FitH);
        PruneFreeRects(freeRects);
        MergeFreeRects(freeRects);

        return true;
    }

    private NestResult RunPolygonCollision(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        // Similar to RunFreeRectangle but with SAT check
        var result = new NestResult
        {
            AlgorithmName = "Polygon Collision",
            PolygonCollisionEnabled = true
        };

        if (parts.Count == 0) return result;

        var sorted = parts.OrderByDescending(p => p.Area).ToList();
        var currentPlate = ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var freeRects = InitFreeRects(currentPlate);

        foreach (var part in sorted)
        {
            bool placed = TryPlacePolygonCollision(part, settings, result, currentPlate, freeRects);
            if (!placed)
            {
                currentPlate = ClonePlate(plate);
                result.Plates.Add(currentPlate);
                freeRects = InitFreeRects(currentPlate);
                placed = TryPlacePolygonCollision(part, settings, result, currentPlate, freeRects);
            }
            if (!placed) result.Unplaced.Add(part);
        }

        FinalizeResult(result, freeRects);
        return result;
    }

    private bool TryPlacePolygonCollision(PartModel part, NestSettings settings, NestResult result,
        PlateModel plate, List<FreeRect> freeRects)
    {
        double gap = Math.Max(0, settings.GapBetweenParts);
        int plateIndex = result.Plates.IndexOf(plate);
        var candidates = new List<PlacementCandidate>();

        foreach (var rect in freeRects)
        {
            foreach (double rotation in GetAllowedRotations(settings))
            {
                var oriented = BuildOrientedGeometry(part.Geometry, rotation);
                var bounds = oriented.Bounds;
                double fitW = bounds.Width + gap;
                double fitH = bounds.Height + gap;

                if (fitW > rect.W + 1e-6 || fitH > rect.H + 1e-6) continue;

                var translated = GeometryUtils.Translate(oriented, rect.X + plate.Margin, rect.Y + plate.Margin);
                if (!IsGeometryInsideUsableArea(translated, plate)) continue;

                if (!PassesCollisionCheck(translated, result, plateIndex, part, rotation, out int bbr, out int cc, out int ch, out _))
                    continue;

                candidates.Add(new PlacementCandidate
                {
                    Rect = rect,
                    Geometry = translated,
                    RotationDeg = rotation,
                    FitW = fitW,
                    FitH = fitH,
                    Score = rect.Y * 1000 + rect.X
                });
            }
        }

        if (candidates.Count == 0) return false;
        var best = candidates.OrderBy(c => c.Score).First();

        var placement = new NestPlacement
        {
            PartId = part.Id, PartName = part.Name, Part = part,
            X = best.Geometry.Bounds.MinX, Y = best.Geometry.Bounds.MinY,
            RotationDeg = best.RotationDeg, PlateIndex = plateIndex,
            Width = best.Geometry.Bounds.Width, Height = best.Geometry.Bounds.Height,
            PlacementTranslationX = best.Geometry.Bounds.MinX,
            PlacementTranslationY = best.Geometry.Bounds.MinY,
            TransformedGeometry = best.Geometry
        };

        result.Placed.Add(placement);
        result.UsedArea += part.Area;
        SplitFreeRects(freeRects, best.Rect, best.FitW, best.FitH);
        PruneFreeRects(freeRects);
        MergeFreeRects(freeRects);
        return true;
    }

    private NestResult RunIrregular(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        var result = new NestResult
        {
            AlgorithmName = "Irregular Geometry Nesting (Faz 8B)",
            PolygonCollisionEnabled = true
        };

        if (parts.Count == 0) return result;

        var sorted = parts.OrderByDescending(p => p.Area).ToList();
        var currentPlate = ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var freeRects = InitFreeRects(currentPlate);

        foreach (var part in sorted)
        {
            if (_stopwatch.ElapsedMilliseconds > DefaultTimeoutMs)
            {
                result.IsTimeout = true;
                result.Unplaced.AddRange(sorted.SkipWhile(p => p != part));
                break;
            }

            result.PlacementAttempts++;
            bool placed = TryPlaceIrregular(part, settings, result, currentPlate, freeRects);

            if (!placed)
            {
                currentPlate = ClonePlate(plate);
                result.Plates.Add(currentPlate);
                freeRects = InitFreeRects(currentPlate);
                placed = TryPlaceIrregular(part, settings, result, currentPlate, freeRects);
            }

            if (!placed)
                result.Unplaced.Add(part);
        }

        FinalizeResult(result, freeRects);
        return result;
    }


    private NestResult RunShapeAware(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        var result = new NestResult
        {
            AlgorithmName = "Shape-Aware Polygon Nesting",
            PolygonCollisionEnabled = true
        };
        if (parts.Count == 0) return result;
        var sorted = parts.OrderByDescending(p => p.Area).ToList();
        var currentPlate = ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var freeRects = InitFreeRects(currentPlate);
        long startTime = _stopwatch.ElapsedMilliseconds;
        const long shapeAwareTimeoutMs = 15000;
        foreach (var part in sorted)
        {
            if (_stopwatch.ElapsedMilliseconds - startTime > shapeAwareTimeoutMs)
            {
                result.IsTimeout = true;
                var fallbackResult = RunFreeRectangle(sorted.Skip(result.PlacedCount).ToList(), plate, settings);
                fallbackResult.FallbackUsed = true;
                fallbackResult.Warnings.Add("Shape-Aware Polygon timed out after 15s, fallback to Free Rectangle.");
                fallbackResult.AlgorithmUsed = "Shape-Aware Polygon (Fallback)";
                return fallbackResult;
            }
            result.PlacementAttempts++;
            bool placed = TryPlaceShapeAware(part, settings, result, currentPlate, freeRects);
            if (!placed)
            {
                currentPlate = ClonePlate(plate);
                result.Plates.Add(currentPlate);
                freeRects = InitFreeRects(currentPlate);
                placed = TryPlaceShapeAware(part, settings, result, currentPlate, freeRects);
            }
            if (!placed)
                result.Unplaced.Add(part);
        }
        FinalizeResult(result, freeRects);
        return result;
    }

    private bool TryPlaceShapeAware(PartModel part, NestSettings settings, NestResult result,
        PlateModel plate, List<FreeRect> freeRects)
    {
        double gap = Math.Max(0, settings.GapBetweenParts);
        int plateIndex = result.Plates.IndexOf(plate);
        var rotations = GetAllowedRotations(settings).ToList();
        var candidates = new List<PlacementCandidate>();
        var shapeAwareAnchors = GetShapeAwareAnchorPoints(result, plate, freeRects, plateIndex);
        foreach (double rotation in rotations)
        {
            var oriented = BuildOrientedGeometry(part.Geometry, rotation);
            if (oriented.Vertices.Count < 3) continue;
            var bounds = oriented.Bounds;
            double w = bounds.Width;
            double h = bounds.Height;
            foreach (var anchor in shapeAwareAnchors)
            {
                if (candidates.Count >= MaxCandidatesPerPart * 2) break;
                AddCandidateIfValid(anchor.X, anchor.Y, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
                if (candidates.Count >= MaxCandidatesPerPart * 2) break;
                AddCandidateIfValid(anchor.X - w - gap, anchor.Y, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
                if (candidates.Count >= MaxCandidatesPerPart * 2) break;
                AddCandidateIfValid(anchor.X, anchor.Y - h - gap, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
                if (candidates.Count >= MaxCandidatesPerPart * 2) break;
                AddCandidateIfValid(anchor.X - w - gap, anchor.Y - h - gap, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
            }
            if (candidates.Count >= MaxCandidatesPerPart * 2) break;
        }
        if (candidates.Count == 0) return false;
        var best = candidates.OrderBy(c => c.Score).First();
        var placement = new NestPlacement
        {
            PartId = part.Id,
            PartName = part.Name,
            Part = part,
            X = best.Geometry.Bounds.MinX,
            Y = best.Geometry.Bounds.MinY,
            RotationDeg = best.RotationDeg,
            PlateIndex = plateIndex,
            Width = best.Geometry.Bounds.Width,
            Height = best.Geometry.Bounds.Height,
            PlacementTranslationX = best.TranslationX,
            PlacementTranslationY = best.TranslationY,
            PlacementScore = best.Score,
            TransformedGeometry = best.Geometry
        };
        result.Placed.Add(placement);
        result.UsedArea += part.Area;
        result.CollisionCheckCount += best.CollisionChecks;
        result.BoundingBoxRejectCount += best.BoundingBoxRejects;
        result.CollisionCacheHits += best.CacheHits;
        result.CandidatePositionsTested += candidates.Count;
        var targetRect = freeRects.FirstOrDefault(r =>
            placement.X >= r.X + plate.Margin - 1e-6 &&
            placement.Y >= r.Y + plate.Margin - 1e-6 &&
            placement.X + placement.Width <= r.X + r.W + plate.Margin + 1e-6 &&
            placement.Y + placement.Height <= r.Y + r.H + plate.Margin + 1e-6);
        if (targetRect != null)
        {
            SplitFreeRects(freeRects, targetRect, placement.Width + gap, placement.Height + gap);
            PruneFreeRects(freeRects);
            MergeFreeRects(freeRects);
        }
        return true;
    }

    private HashSet<Point2D> GetShapeAwareAnchorPoints(NestResult result, PlateModel plate, List<FreeRect> freeRects, int plateIndex)
    {
        var anchors = new HashSet<Point2D>(new Point2DComparer(0.05));
        anchors.Add(new Point2D(plate.Margin, plate.Margin));
        anchors.Add(new Point2D(plate.Width - plate.Margin, plate.Margin));
        anchors.Add(new Point2D(plate.Margin, plate.Height - plate.Margin));
        anchors.Add(new Point2D(plate.Width - plate.Margin, plate.Height - plate.Margin));
        foreach (var rect in freeRects)
        {
            anchors.Add(new Point2D(rect.X + plate.Margin, rect.Y + plate.Margin));
            anchors.Add(new Point2D(rect.X + rect.W + plate.Margin, rect.Y + plate.Margin));
            anchors.Add(new Point2D(rect.X + plate.Margin, rect.Y + rect.H + plate.Margin));
            anchors.Add(new Point2D(rect.X + rect.W + plate.Margin, rect.Y + rect.H + plate.Margin));
        }
        foreach (var p in result.Placed.Where(pl => pl.PlateIndex == plateIndex))
        {
            var b = p.TransformedGeometry.Bounds;
            anchors.Add(new Point2D(b.MinX, b.MinY));
            anchors.Add(new Point2D(b.MaxX, b.MinY));
            anchors.Add(new Point2D(b.MinX, b.MaxY));
            anchors.Add(new Point2D(b.MaxX, b.MaxY));
            foreach (var v in p.TransformedGeometry.Vertices)
            {
                anchors.Add(v);
            }
            for (int i = 0; i < p.TransformedGeometry.Vertices.Count; i++)
            {
                var v1 = p.TransformedGeometry.Vertices[i];
                var v2 = p.TransformedGeometry.Vertices[(i + 1) % p.TransformedGeometry.Vertices.Count];
                double midX = (v1.X + v2.X) / 2;
                double midY = (v1.Y + v2.Y) / 2;
                anchors.Add(new Point2D(midX, midY));
            }
        }
        return anchors;
    }


    private NestResult RunTrueShapeNesting(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        var result = new NestResult
        {
            AlgorithmName = "True Shape Nesting (NFP-Based)",
            PolygonCollisionEnabled = true
        };

        if (parts.Count == 0) return result;

        var gap = Math.Max(0, settings.GapBetweenParts);
        long startTime = _stopwatch.ElapsedMilliseconds;
        const long trueShapeTimeoutMs = 15000;

        TrueShapeTrace.Clear();
        TrueShapeTrace.LogSeparator('=');
        TrueShapeTrace.Log("TRUESHAPE NESTING RUN");
        TrueShapeTrace.Log($"Plate: {plate.Width}x{plate.Height} Margin={plate.Margin} Gap={gap}");
        TrueShapeTrace.Log($"Parts: {parts.Count}");
        TrueShapeTrace.DumpAllParts(parts);
        TrueShapeTrace.LogSeparator();

        var sorted = parts.OrderByDescending(p => p.Area).ToList();
        var currentPlate = ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var freeRects = InitFreeRects(currentPlate);

        foreach (var part in sorted)
        {
            if (_stopwatch.ElapsedMilliseconds - startTime > trueShapeTimeoutMs)
            {
                if (!result.IsTimeout)
                {
                    result.IsTimeout = true;
                    TrueShapeTrace.Log("  TRUE_SHAPE_TIMEOUT_REACHED: continuing with multi-plate continuation");
                }
            }

            result.PlacementAttempts++;
            bool isSmallPart = IsSmallPartCandidate(part, currentPlate);
            int currentPlateIdx = result.Plates.IndexOf(currentPlate);
            TrueShapeTrace.Log($"\nAttempt part: {part.Id}|{part.Name} area={part.Area:F3} w={part.Width:F3} h={part.Height:F3} isSmall={isSmallPart} currentPlate={currentPlateIdx}");
            TrueShapeTrace.LogPartGeometry(part);

            long partStart = _stopwatch.ElapsedMilliseconds;
            bool placed = TryPlaceTrueShape(part, settings, result, currentPlate, freeRects, gap);
            TrueShapeTrace.Log($"  Step1(currentPlate={currentPlateIdx},freeRects): placed={placed}");

            if (!placed && isSmallPart)
            {
                placed = TryPlaceTrueShape(part, settings, result, currentPlate, null, gap, preferGapFit: true);
                TrueShapeTrace.Log($"  Step2(currentPlate={currentPlateIdx},gapPriority): placed={placed}");
            }

            if (!placed && isSmallPart)
            {
                placed = TryPlaceTrueShapeOnEarlierPlates(part, settings, result, gap);
                TrueShapeTrace.Log($"  Step3(earlierPlates): placed={placed}");
                if (placed)
                    result.NewPlateAvoidedCount++;
            }

            if (!placed)
            {
                placed = TryGapFillBeforeNewPlate(part, settings, result, gap);
                TrueShapeTrace.Log($"  Step4b(gapFillBeforeNewPlate): placed={placed}");
            }

            if (!placed && !isSmallPart)
            {
                placed = TryPlaceShapeAware(part, settings, result, currentPlate, freeRects);
                TrueShapeTrace.Log($"  Step4(shapeAware): placed={placed}");
            }

            if (!placed)
            {
                TrueShapeTrace.Log($"=== PART_FAILED_BEFORE_NEW_PLATE ===");
                TrueShapeTrace.Log($"Part: {part.Id}|{part.Name} area={part.Area:F3} bbox={part.Width:F3}x{part.Height:F3}");
                TrueShapeTrace.Log($"Existing plates: {result.Plates.Count}, placed parts: {result.PlacedCount}");
                TrueShapeTrace.Log($"CandidateLimitHit: {result.CandidateLimitHitCount}");
                TrueShapeTrace.Log($"Reject breakdown by source:");
                TrueShapeTrace.Log($"  ES(EmptySpace): BOUNDARY={result.EmptySpaceBoundaryRejects} SAT={result.EmptySpaceSATRejects}");
                TrueShapeTrace.Log($"  PF(PlateFree):   BOUNDARY={result.PlateFreeSpaceBoundaryRejects} SAT={result.PlateFreeSpaceSATRejects}");
                TrueShapeTrace.Log($"  VT(Vertex):      BOUNDARY={result.VertexBoundaryRejects} SAT={result.VertexSATRejects}");
                TrueShapeTrace.Log($"  EM(EdgeMid):     BOUNDARY={result.EdgeBoundaryRejects} SAT={result.EdgeSATRejects}");
                TrueShapeTrace.Log($"  CN(Corner):      BOUNDARY={result.CornerBoundaryRejects} SAT={result.CornerSATRejects}");
                TrueShapeTrace.Log($"BBoxOverlapSATClearAccepted: {result.BoundingBoxOverlapButSATClearAccepted}");
                TrueShapeTrace.Log($"Total rejected: BOUNDARY={result.RejectedByBoundaryCount} SAT={result.RejectedBySATCount}");
                TrueShapeTrace.Log($"Placed on same plate before failure: {result.AcceptedShapeAwareCount}");
                TrueShapeTrace.Log($"TryGapFillBeforeNewPlate result: {placed}");

                if (DebugForceExistingPlateMode && isSmallPart)
                {
                    result.DebugForceExistingPlateModeFailures++;
                    TrueShapeTrace.Log($"  DebugForceExistingPlateMode: BLOCKING new plate for small part {part.Id}");
                    result.Unplaced.Add(part);
                    TrueShapeTrace.LogTime(part.Id, _stopwatch.ElapsedMilliseconds - partStart);
                    continue;
                }

                result.NewPlateOpenedAfterGapFillFailed++;
                result.TrueShapePlateOpenCount++;
                TrueShapeTrace.Log($"  TRUE_SHAPE_NEW_PLATE_OPENED: plate {result.Plates.Count} opened for part {part.Id}|{part.Name}");
                TrueShapeTrace.Log($"  Step5: opening NEW PLATE");
                currentPlate = ClonePlate(plate);
                result.Plates.Add(currentPlate);
                freeRects = InitFreeRects(currentPlate);
                currentPlateIdx = result.Plates.IndexOf(currentPlate);
                isSmallPart = IsSmallPartCandidate(part, currentPlate);
                placed = TryPlaceTrueShape(part, settings, result, currentPlate, freeRects, gap);
                TrueShapeTrace.Log($"  Step6(newPlate={currentPlateIdx},freeRects): placed={placed}");
                if (!placed && isSmallPart)
                {
                    placed = TryPlaceTrueShape(part, settings, result, currentPlate, null, gap, preferGapFit: true);
                    TrueShapeTrace.Log($"  Step7(newPlate={currentPlateIdx},gapPriority): placed={placed}");
                }
                if (!placed && !isSmallPart)
                {
                    placed = TryPlaceShapeAware(part, settings, result, currentPlate, freeRects);
                    TrueShapeTrace.Log($"  Step8(newPlate={currentPlateIdx},shapeAware): placed={placed}");
                }
                if (!placed && isSmallPart)
                {
                    placed = TryPlaceTrueShapeOnEarlierPlates(part, settings, result, gap);
                    TrueShapeTrace.Log($"  Step9(earlierPlates after new plate): placed={placed}");
                    if (placed)
                        result.NewPlateAvoidedCount++;
                }
                if (placed)
                {
                    result.TrueShapeMultiPlateContinuationCount++;
                    TrueShapeTrace.Log($"  TRUE_SHAPE_CONTINUE_ON_NEXT_PLATE: {part.Id}|{part.Name} placed on new plate {currentPlateIdx}");
                }
                else
                {
                    TrueShapeTrace.Log($"  PART_MOVED_TO_NEXT_PLATE: {part.Id}|{part.Name} could not be placed on new plate {currentPlateIdx} either");
                }
            }

            TrueShapeTrace.LogTime(part.Id, _stopwatch.ElapsedMilliseconds - partStart);

            if (!placed)
                result.Unplaced.Add(part);
        }

        result.PreGapFillUnplacedCount = result.Unplaced.Count;
        var smallParts = result.Unplaced.Where(p => p.Area < sorted.Average(x => x.Area) * 0.3).ToList();
        result.GapFillInputCount = smallParts.Count;
        if (smallParts.Count > 0)
        {
            var gapFillResult = TryGapFill(smallParts, result, plate, settings, gap, startTime, trueShapeTimeoutMs);
            result.TrueShapeCandidateCount += gapFillResult.TrueShapeCandidateCount;
            result.VertexToVertexCandidateCount += gapFillResult.VertexToVertexCandidateCount;
            result.VertexToEdgeCandidateCount += gapFillResult.VertexToEdgeCandidateCount;
            result.EdgeToEdgeCandidateCount += gapFillResult.EdgeToEdgeCandidateCount;
            result.GapFillAttemptCount += gapFillResult.GapFillAttemptCount;
            result.GapFillSuccessCount += gapFillResult.GapFillSuccessCount;
            result.RejectedByBoundaryCount += gapFillResult.RejectedByBoundaryCount;
            result.RejectedByBoundingBoxCount += gapFillResult.RejectedByBoundingBoxCount;
            result.RejectedBySATCount += gapFillResult.RejectedBySATCount;
            result.AcceptedShapeAwareCount += gapFillResult.AcceptedShapeAwareCount;
            result.BoundingBoxRejectCount += gapFillResult.BoundingBoxRejectCount;
            foreach (var p in gapFillResult.Placed)
            {
                var existing = result.Unplaced.FirstOrDefault(x => x.Id == p.PartId);
                if (existing != null)
                {
                    result.Unplaced.Remove(existing);
                    result.Placed.Add(p);
                    result.UsedArea += p.Part?.Area ?? 0;
                }
            }
        }

        if (result.IsTimeout && result.Unplaced.Count > 0)
        {
            TrueShapeTrace.Log($"  TRUE_SHAPE_MULTIPLATE_COMPLETE: timeout reached, {result.Unplaced.Count} parts still unplaced, running FreeRectangle fallback");
            var fallbackParts = result.Unplaced.ToList();
            var fallbackResult = RunFreeRectangle(fallbackParts, plate, settings);
            foreach (var p in fallbackResult.Placed)
            {
                var existing = result.Unplaced.FirstOrDefault(x => x.Id == p.PartId);
                if (existing != null)
                {
                    result.Unplaced.Remove(existing);
                    result.Placed.Add(p);
                    result.UsedArea += p.Part?.Area ?? 0;
                    result.FallbackAvoidedByMultiPlate++;
                }
            }
            if (fallbackResult.Placed.Count > 0)
            {
                result.FallbackUsed = true;
                if (!result.Warnings.Contains("True Shape Nesting timed out, fallback to Free Rectangle used for some parts."))
                    result.Warnings.Add("True Shape Nesting timed out, fallback to Free Rectangle used for some parts.");
                result.AlgorithmUsed = "True Shape Nesting (Fallback)";
                TrueShapeTrace.Log($"  TRUE_SHAPE_FALLBACK_MERGED: {fallbackResult.Placed.Count} placed by FreeRectangle, {result.Unplaced.Count} still unplaced");
            }
        }

        RunSecondPlateRecoveryPass(result, settings, gap);

        result.PreValidationPlacedCount = result.Placed.Count;
        FinalizeResult(result, freeRects);

        ComputeEmptySpaceDiagnosticSummary(result);
        TrueShapeTrace.DumpEmptySpaceDiagnostics(result);
        TrueShapeTrace.DumpResult(result);
        TrueShapeTrace.WriteToFile("logs/trueshape-debug.txt");

        return result;
    }


    private void RunSecondPlateRecoveryPass(NestResult result, NestSettings settings, double gap)
    {
        if (result.Plates.Count < 2)
            return;

        TrueShapeTrace.LogSeparator('=');
        TrueShapeTrace.Log("SECOND_PLATE_RECOVERY_START");
        TrueShapeTrace.Log($"Before plates: {result.Plates.Count}");
        TrueShapeTrace.Log($"Placed count: {result.Placed.Count}");

        int movedCount = 0;
        int attempts = 0;

        for (int sourcePlateIndex = result.Plates.Count - 1; sourcePlateIndex >= 1; sourcePlateIndex--)
        {
            var sourcePlate = result.Plates[sourcePlateIndex];

            var movable = result.Placed
                .Where(p => p.PlateIndex == sourcePlateIndex)
                .Where(p => p.Part != null)
                .Where(p => IsSmallPartCandidate(p.Part!, sourcePlate))
                .OrderBy(p => p.Part!.Area)
                .ToList();

            foreach (var placement in movable)
            {
                var part = placement.Part;
                if (part == null)
                    continue;

                attempts++;
                TrueShapeTrace.Log($"RECOVERY_TRY_PART: {part.Id}|{part.Name} from Plate{sourcePlateIndex}");

                result.Placed.Remove(placement);
                result.UsedArea -= part.Area;

                bool moved = false;

                for (int targetPlateIndex = 0; targetPlateIndex < sourcePlateIndex; targetPlateIndex++)
                {
                    var targetPlate = result.Plates[targetPlateIndex];

                    if (TryPlaceTrueShape(part, settings, result, targetPlate, null, gap, preferGapFit: true))
                    {
                        moved = true;
                        movedCount++;
                        TrueShapeTrace.Log($"RECOVERY_MOVED_PART: {part.Id}|{part.Name} Plate{sourcePlateIndex} -> Plate{targetPlateIndex}");
                        break;
                    }
                }

                if (!moved)
                {
                    result.Placed.Add(placement);
                    result.UsedArea += part.Area;
                    TrueShapeTrace.Log($"RECOVERY_FAILED_PART: {part.Id}|{part.Name} kept on Plate{sourcePlateIndex}");
                }
            }
        }

        int beforeRemove = result.Plates.Count;

        for (int plateIndex = result.Plates.Count - 1; plateIndex >= 1; plateIndex--)
        {
            bool hasParts = result.Placed.Any(p => p.PlateIndex == plateIndex);
            if (!hasParts)
            {
                result.Plates.RemoveAt(plateIndex);

                foreach (var p in result.Placed.Where(p => p.PlateIndex > plateIndex))
                    p.PlateIndex--;
            }
        }

        int removedPlates = beforeRemove - result.Plates.Count;

        TrueShapeTrace.Log($"SECOND_PLATE_RECOVERY_DONE: attempts={attempts} moved={movedCount} removedPlates={removedPlates} afterPlates={result.Plates.Count}");
        TrueShapeTrace.LogSeparator('=');
    }

    private static void ComputeEmptySpaceDiagnosticSummary(NestResult result)
    {
        var diag = result.EmptySpaceDiagnostics;
        diag.TotalESAttempts = result.EmptySpaceCandidateCount;
        diag.TotalESBoundaryRejects = result.EmptySpaceBoundaryRejects;
        diag.TotalESSATRejects = result.EmptySpaceSATRejects;
        diag.TotalESAccepted = diag.AllCandidates.Count(c => !c.SATFailed);

        if (diag.AllCandidates.Count == 0)
        {
            diag.MaxClearanceFound = 0;
            diag.RequiredClearance = 0;
            diag.BestEmptySpaceCandidate = "(none)";
            diag.BestEmptySpaceCandidateSATResult = false;
            return;
        }

        diag.RequiredClearance = diag.AllCandidates
            .Select(c => c.PartCriticalDimension)
            .DefaultIfEmpty(0)
            .Max();

        var bestAccepted = diag.AllCandidates
            .Where(c => !c.SATFailed)
            .OrderByDescending(c => c.MinClearanceToAllPlaced)
            .FirstOrDefault();

        if (bestAccepted != null)
        {
            diag.MaxClearanceFound = bestAccepted.MinClearanceToAllPlaced;
            diag.BestEmptySpaceCandidate = $"t=({bestAccepted.TranslationX:F3},{bestAccepted.TranslationY:F3}) rot={bestAccepted.RotationDeg:F1} clearance={bestAccepted.MinClearanceToAllPlaced:F3}";
            diag.BestEmptySpaceCandidateSATResult = true;
        }
        else
        {
            var bestRejected = diag.AllCandidates
                .Where(c => c.SATFailed)
                .OrderByDescending(c => c.MinClearanceToAllPlaced)
                .FirstOrDefault();

            if (bestRejected != null)
            {
                diag.MaxClearanceFound = bestRejected.MinClearanceToAllPlaced;
                diag.BestEmptySpaceCandidate = $"t=({bestRejected.TranslationX:F3},{bestRejected.TranslationY:F3}) rot={bestRejected.RotationDeg:F1} overlap={bestRejected.OverlapDepth:F3} with placed[{bestRejected.OverlapWithPlacedIndex}]";
                diag.BestEmptySpaceCandidateSATResult = false;
            }
        }
    }

    private bool TryPlaceTrueShape(PartModel part, NestSettings settings, NestResult result,
        PlateModel plate, List<FreeRect>? freeRects, double gap, bool preferGapFit = false)
    {
        int plateIndex = result.Plates.IndexOf(plate);
        var allRotations = GetAllowedRotations(settings).ToList();
        var candidates = new List<PlacementCandidate>();
        bool denseSampling = IsSmallPartCandidate(part, plate);
        bool gapPriority = preferGapFit || (denseSampling && result.Placed.Any(p => p.PlateIndex == plateIndex));

        var (partClass, classLabel) = ClassifyPart(part, plate);

        int maxCandidates;
        List<double> rotations;
        bool useEmptySpace;

        switch (partClass)
        {
            case PartClass.Large:
                maxCandidates = 300;
                rotations = allRotations.Where(r => r == 0 || r == 90).ToList();
                useEmptySpace = false;
                result.LargePartCandidateLimit++;
                result.LargePartFastPathUsed++;
                break;
            case PartClass.Medium:
                maxCandidates = 600;
                rotations = allRotations.Where(r => r == 0 || r == 90 || r == 180 || r == 270).ToList();
                useEmptySpace = true;
                result.MediumPartCandidateLimit++;
                break;
            default:
                maxCandidates = gapPriority ? 2400 : 1200;
                rotations = allRotations;
                useEmptySpace = denseSampling;
                result.SmallPartCandidateLimit++;
                break;
        }

        var rotStr = string.Join(",", rotations.Select(r => r.ToString("F0")));
        TrueShapeTrace.Log($"  PART_BUDGET_DECISION: {part.Id}|{part.Name} class={classLabel} bbox={part.Width:F1}x{part.Height:F1} area={part.Area:F1} verts={part.Geometry.Vertices.Count} rotations=[{rotStr}] maxCandidates={maxCandidates}");

        var nfpAnchors = GetNFPCandidatePoints(result, plate, freeRects, plateIndex, gap, part, useEmptySpace);

        foreach (double rotation in rotations)
        {
            var oriented = BuildOrientedGeometry(part.Geometry, rotation);
            if (oriented.Vertices.Count < 3) continue;

            var localAnchors = GetLocalAnchors(oriented);
            int anchorsUsed = 0;

            foreach (var (anchor, src) in nfpAnchors)
            {
                if (candidates.Count >= maxCandidates) { result.CandidateLimitHitCount++; break; }

                foreach (var localAnchor in localAnchors)
                {
                    if (candidates.Count >= maxCandidates) { result.CandidateLimitHitCount++; break; }

                    anchorsUsed++;
                    TryAddTrueShapeCandidate(anchor, localAnchor,
                        anchorsUsed % 2 == 0 ? AnchorMatchType.VertexToVertex : AnchorMatchType.VertexToEdge,
                        rotation, oriented, candidates, plate, result, plateIndex, gap, part, gapFill: false,
                        source: src);
                }
            }
            if (candidates.Count >= maxCandidates) { result.CandidateLimitHitCount++; break; }
        }

        if (candidates.Count == 0)
        {
            TrueShapeTrace.Log($"  TryPlaceTrueShape(P{plateIndex},gapPri={preferGapFit}): NO CANDIDATES");
            return false;
        }

        const int topN = 5;
        bool isGapPriority = preferGapFit || gapPriority;
        TrueShapeTrace.Log($"  TryPlaceTrueShape(P{plateIndex},gapPri={isGapPriority}): candidates={candidates.Count}, nfpAnchors={nfpAnchors.Count}, rotations={string.Join(",", rotations.Select(r => r.ToString("F0")))}");

        var scoredCandidates = candidates.Select(c => new
        {
            Candidate = c,
            Score = CalculateTrueShapeScoreCandidate(c, result, plate, plateIndex, gapFill: false, preferGapFit: gapPriority)
        }).OrderBy(x => x.Score).ToList();

        var best = scoredCandidates.First().Candidate;
        TrueShapeTrace.Log($"  Top candidate: score={best.Score:F3} t=({best.TranslationX:F3},{best.TranslationY:F3}) rot={best.RotationDeg:F1} anchor={best.AnchorType}");
        for (int ti = 1; ti < Math.Min(topN, scoredCandidates.Count); ti++)
        {
            var cc = scoredCandidates[ti].Candidate;
            TrueShapeTrace.Log($"    #{ti + 1}: score={cc.Score:F3} t=({cc.TranslationX:F3},{cc.TranslationY:F3}) rot={cc.RotationDeg:F1} anchor={cc.AnchorType}");
        }

        var placement = new NestPlacement
        {
            PartId = part.Id,
            PartName = part.Name,
            Part = part,
            X = best.Geometry.Bounds.MinX,
            Y = best.Geometry.Bounds.MinY,
            RotationDeg = best.RotationDeg,
            PlateIndex = plateIndex,
            Width = best.Geometry.Bounds.Width,
            Height = best.Geometry.Bounds.Height,
            PlacementTranslationX = best.TranslationX,
            PlacementTranslationY = best.TranslationY,
            PlacementScore = best.Score,
            TransformedGeometry = best.Geometry
        };

        result.Placed.Add(placement);
        result.UsedArea += part.Area;
        result.AcceptedShapeAwareCount++;

        if (freeRects != null)
        {
            var targetRect = freeRects.FirstOrDefault(r =>
                placement.X >= r.X + plate.Margin - 1e-6 &&
                placement.Y >= r.Y + plate.Margin - 1e-6 &&
                placement.X + placement.Width <= r.X + r.W + plate.Margin + 1e-6 &&
                placement.Y + placement.Height <= r.Y + r.H + plate.Margin + 1e-6);

            if (targetRect != null)
            {
                SplitFreeRects(freeRects, targetRect, placement.Width + gap, placement.Height + gap);
                PruneFreeRects(freeRects);
                MergeFreeRects(freeRects);
            }
        }

        return true;
    }

    private bool TryPlaceTrueShapeOnEarlierPlates(PartModel part, NestSettings settings, NestResult result, double gap)
    {
        if (result.Plates.Count < 2)
            return false;

        for (int plateIndex = 0; plateIndex < result.Plates.Count - 1; plateIndex++)
        {
            var plate = result.Plates[plateIndex];
            if (!IsSmallPartCandidate(part, plate))
                continue;

            if (TryPlaceTrueShape(part, settings, result, plate, null, gap, preferGapFit: true))
            {
                result.SmallPartGapSuccess++;
                result.SamePlateGapSuccess++;
                return true;
            }
        }

        return false;
    }

    private bool TryGapFillBeforeNewPlate(PartModel part, NestSettings settings, NestResult result, double gap)
    {
        result.GapFillBeforeNewPlateTried++;
        int totalPlates = result.Plates.Count;
        if (totalPlates == 0) return false;

        const int maxCandidatesPerPlate = 1200;
        var rotations = GetAllowedRotations(settings).ToList();

        for (int pi = 0; pi < totalPlates; pi++)
        {
            var plate = result.Plates[pi];
            var candidates = new List<PlacementCandidate>();

            foreach (double rotation in rotations)
            {
                var oriented = BuildOrientedGeometry(part.Geometry, rotation);
                if (oriented.Vertices.Count < 3) continue;

                var localAnchors = GetLocalAnchors(oriented);
                var gapAnchors = GetGapFillAnchors(result, plate, pi, gap, part);

                int anchorIdx = 0;
                foreach (var anchor in gapAnchors)
                {
                    if (candidates.Count >= maxCandidatesPerPlate) break;
                    foreach (var localAnchor in localAnchors)
                    {
                        if (candidates.Count >= maxCandidatesPerPlate) break;
                        anchorIdx++;
                        TryAddTrueShapeCandidate(anchor, localAnchor,
                            anchorIdx % 2 == 0 ? AnchorMatchType.VertexToVertex : AnchorMatchType.VertexToEdge,
                            rotation, oriented, candidates, plate, result, pi, gap, part, gapFill: true,
                            source: CandidateSource.GapFill);
                    }
                }
            }

            if (candidates.Count == 0) continue;

            double bestScore = double.MaxValue;
            PlacementCandidate? best = null;
            foreach (var c in candidates)
            {
                double score = CalculateTrueShapeScoreCandidate(c, result, plate, pi, gapFill: true);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            if (best != null)
            {
                var placement = new NestPlacement
                {
                    PartId = part.Id,
                    PartName = part.Name,
                    Part = part,
                    X = best.Geometry.Bounds.MinX,
                    Y = best.Geometry.Bounds.MinY,
                    RotationDeg = best.RotationDeg,
                    PlateIndex = pi,
                    Width = best.Geometry.Bounds.Width,
                    Height = best.Geometry.Bounds.Height,
                    PlacementTranslationX = best.TranslationX,
                    PlacementTranslationY = best.TranslationY,
                    PlacementScore = best.Score,
                    TransformedGeometry = best.Geometry
                };
                result.Placed.Add(placement);
                result.UsedArea += part.Area;
                result.AcceptedShapeAwareCount++;
                result.GapFillBeforeNewPlateAccepted++;
                result.SmallPartGapSuccess++;
                result.SamePlateGapSuccess++;
                TrueShapeTrace.LogPlacement(part.Id, pi, best.TranslationX, best.TranslationY, best.RotationDeg, true);
                return true;
            }
        }

        result.GapFillBeforeNewPlateRejected++;
        TrueShapeTrace.Log($"  TryGapFillBeforeNewPlate: FAILED after {totalPlates} plates");
        return false;
    }

    private List<(Point2D point, CandidateSource source)> GetNFPCandidatePoints(NestResult result, PlateModel plate, List<FreeRect>? freeRects, int plateIndex, double gap, PartModel part, bool denseSampling)
    {
        var dedup = new HashSet<Point2D>(new Point2DComparer(0.05));
        var ordered = new List<(Point2D, CandidateSource)>();

        void AddIfNew(Point2D pt, CandidateSource src)
        {
            if (dedup.Add(pt))
                ordered.Add((pt, src));
        }

        // Priority 1: Empty space samples (gap regions inside placed polygon bboxes)
        if (denseSampling)
        {
            foreach (var p in result.Placed.Where(pl => pl.PlateIndex == plateIndex))
            {
                foreach (var sample in GetEmptySpaceSamples(p.TransformedGeometry, Math.Max(2, gap / 2.0), 36))
                {
                    AddIfNew(sample, CandidateSource.EmptySpace);
                    result.EmptySpaceCandidateCount++;
                }
            }
        }

        // Priority 2: Plate free space samples (gap regions between placed parts)
        if (denseSampling)
        {
            var plateSamples = GetPlateFreeSpaceSamples(result, plate, plateIndex, gap, 72);
            foreach (var sample in plateSamples)
            {
                AddIfNew(sample, CandidateSource.PlateFreeSpace);
                result.PlateFreeSpaceCandidateCount++;
            }
            result.SmallPartGapCandidates += plateSamples.Count;
        }

        // Priority 3: Free rect centers (if available)
        if (freeRects != null)
        {
            foreach (var rect in freeRects)
            {
                AddIfNew(new Point2D(rect.X + plate.Margin, rect.Y + plate.Margin), CandidateSource.FreeRect);
                AddIfNew(new Point2D(rect.X + rect.W * 0.5 + plate.Margin, rect.Y + rect.H * 0.5 + plate.Margin), CandidateSource.FreeRect);
            }
        }

        // Priority 4: Placed polygon vertices
        foreach (var p in result.Placed.Where(pl => pl.PlateIndex == plateIndex))
        {
            foreach (var v in p.TransformedGeometry.Vertices)
            {
                AddIfNew(v, CandidateSource.Vertex);
                result.VertexAnchorCandidateCount++;
            }
        }

        // Priority 5: Edge midpoints of placed polygons
        foreach (var p in result.Placed.Where(pl => pl.PlateIndex == plateIndex))
        {
            var poly = p.TransformedGeometry;
            for (int i = 0; i < poly.Vertices.Count; i++)
            {
                var v1 = poly.Vertices[i];
                var v2 = poly.Vertices[(i + 1) % poly.Vertices.Count];
                AddIfNew(new Point2D((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2), CandidateSource.EdgeMidpoint);
                result.EdgeMidpointCandidateCount++;
            }
        }

        // Priority 6: Plate corners (always last — least useful for gap fill)
        AddIfNew(new Point2D(plate.Margin, plate.Margin), CandidateSource.Corner);
        AddIfNew(new Point2D(plate.Width - plate.Margin, plate.Margin), CandidateSource.Corner);
        AddIfNew(new Point2D(plate.Margin, plate.Height - plate.Margin), CandidateSource.Corner);
        AddIfNew(new Point2D(plate.Width - plate.Margin, plate.Height - plate.Margin), CandidateSource.Corner);
        result.CornerAnchorCandidateCount += 4;

        return ordered;
    }


    private double CalculateTrueShapeScoreCandidate(PlacementCandidate c, NestResult result, PlateModel plate, int plateIndex, bool gapFill = false, bool preferGapFit = false)
    {
        double score = 0;
        var geom = c.Geometry;
        var bounds = geom.Bounds;

        double currentMaxY = result.Placed.Where(p => p.PlateIndex == plateIndex).Select(p => p.TransformedGeometry.Bounds.MaxY).DefaultIfEmpty(0).Max();
        double usedHeight = Math.Max(currentMaxY, bounds.MaxY);
        double bboxWaste = Math.Max(0, (bounds.Width * bounds.Height) - geom.Area);

        double edgeContact = 0;
        foreach (var placed in result.Placed.Where(p => p.PlateIndex == plateIndex))
        {
            var pg = placed.TransformedGeometry;
            foreach (var v in geom.Vertices)
            {
                foreach (var pv in pg.Vertices)
                {
                    double dist = Math.Sqrt((v.X - pv.X) * (v.X - pv.X) + (v.Y - pv.Y) * (v.Y - pv.Y));
                    if (dist < 10) edgeContact += (10 - dist);
                }
            }
        }

        if (gapFill || preferGapFit)
        {
            score += bboxWaste * 25;
            score += usedHeight * 6;
            score += bounds.MinY * 8;
            score += bounds.MinX * 4;
            score += plateIndex * 220;
            score -= edgeContact * 18;
            score -= c.LocalGapFit * 260;
            score -= GetAnchorTypeBonus(c.AnchorType) * 220;
        }
        else
        {
            score += bounds.MinY * 300;
            score += bounds.MinX * 40;
            score += bboxWaste * 15;
            score += usedHeight * 20;
            score -= edgeContact * 20;
            score -= c.LocalGapFit * 60;
            score -= GetAnchorTypeBonus(c.AnchorType) * 100;
        }

        score += c.BoundingBoxRejects * 0.001;
        score += c.CollisionChecks * 0.01;

        return score;
    }

    private NestResult TryGapFill(List<PartModel> smallParts, NestResult existingResult,
        PlateModel plate, NestSettings settings, double gap, long startTime, long timeoutMs)
    {
        var fillResult = new NestResult
        {
            AlgorithmName = "True Shape Gap Fill",
            PolygonCollisionEnabled = true
        };

        if (smallParts.Count == 0) return fillResult;

        var sortedSmall = smallParts.OrderBy(p => p.Area).ToList();

        foreach (var part in sortedSmall)
        {
            if (_stopwatch.ElapsedMilliseconds - startTime > timeoutMs)
            {
                break;
            }

            NestPlacement? bestPlacement = null;
            double bestScore = double.MaxValue;

            for (int pi = 0; pi < existingResult.Plates.Count; pi++)
            {
                var testPlate = existingResult.Plates[pi];
                var candidates = new List<PlacementCandidate>();

                var rotations = GetAllowedRotations(settings).ToList();
                foreach (double rotation in rotations)
                {
                    var oriented = BuildOrientedGeometry(part.Geometry, rotation);
                    if (oriented.Vertices.Count < 3) continue;

                var localAnchors = GetLocalAnchors(oriented);
                    var smallAnchors = GetGapFillAnchors(existingResult, testPlate, pi, gap, part);
                    existingResult.SmallPartGapCandidates += smallAnchors.Count;

                    int anchorIdx = 0;
                    foreach (var anchor in smallAnchors)
                    {
                        if (candidates.Count >= 600) break;
                        foreach (var localAnchor in localAnchors)
                        {
                            if (candidates.Count >= 600) break;
                            anchorIdx++;
                            TryAddTrueShapeCandidate(anchor, localAnchor,
                                anchorIdx % 2 == 0 ? AnchorMatchType.VertexToVertex : AnchorMatchType.VertexToEdge,
                                rotation, oriented, candidates, testPlate, existingResult, pi, gap, part, gapFill: true,
                                source: CandidateSource.GapFill);
                        }
                    }
                }

                if (candidates.Count == 0) continue;

                foreach (var c in candidates)
                {
                    double score = CalculateTrueShapeScoreCandidate(c, existingResult, testPlate, pi, gapFill: true);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPlacement = new NestPlacement
                        {
                            PartId = part.Id,
                            PartName = part.Name,
                            Part = part,
                            X = c.Geometry.Bounds.MinX,
                            Y = c.Geometry.Bounds.MinY,
                            RotationDeg = c.RotationDeg,
                            PlateIndex = pi,
                            Width = c.Geometry.Bounds.Width,
                            Height = c.Geometry.Bounds.Height,
                            PlacementTranslationX = c.TranslationX,
                            PlacementTranslationY = c.TranslationY,
                            PlacementScore = c.Score,
                            TransformedGeometry = c.Geometry
                        };
                    }
                }
            }

            if (bestPlacement != null)
            {
                fillResult.Placed.Add(bestPlacement);
                fillResult.UsedArea += part.Area;
                fillResult.GapFillSuccessCount++;
                fillResult.SmallPartGapSuccess++;
                fillResult.SamePlateGapSuccess++;
                fillResult.AcceptedShapeAwareCount++;
            }
        }

        return fillResult;
    }

    private HashSet<Point2D> GetGapFillAnchors(NestResult result, PlateModel plate, int plateIndex, double gap, PartModel? part = null)
    {
        var anchors = new HashSet<Point2D>(new Point2DComparer(0.1));

        anchors.Add(new Point2D(plate.Margin, plate.Margin));
        anchors.Add(new Point2D(plate.Width - plate.Margin, plate.Margin));
        anchors.Add(new Point2D(plate.Margin, plate.Height - plate.Margin));
        anchors.Add(new Point2D(plate.Width - plate.Margin, plate.Height - plate.Margin));

        foreach (var p in result.Placed.Where(pl => pl.PlateIndex == plateIndex))
        {
            var pg = p.TransformedGeometry;

            var centroid = GetPolygonCentroid(pg);
            anchors.Add(centroid);
            anchors.Add(pg.Bounds.Center);

            foreach (var v in pg.Vertices)
            {
                anchors.Add(v);
            }

            for (int i = 0; i < pg.Vertices.Count; i++)
            {
                var v1 = pg.Vertices[i];
                var v2 = pg.Vertices[(i + 1) % pg.Vertices.Count];
                double midX = (v1.X + v2.X) / 2;
                double midY = (v1.Y + v2.Y) / 2;
                anchors.Add(new Point2D(midX, midY));
            }

            foreach (var sample in GetEmptySpaceSamples(pg, Math.Max(2, gap / 2.0), 24))
                anchors.Add(sample);
        }

        foreach (var sample in GetPlateFreeSpaceSamples(result, plate, plateIndex, gap, 48))
            anchors.Add(sample);

        return anchors;
    }

    private List<Point2D> GetLocalAnchors(Polygon oriented)
    {
        var anchors = new List<Point2D>();
        anchors.AddRange(oriented.Vertices);
        anchors.Add(oriented.Bounds.Center);
        anchors.Add(GetPolygonCentroid(oriented));

        for (int i = 0; i < oriented.Vertices.Count; i++)
        {
            var v1 = oriented.Vertices[i];
            var v2 = oriented.Vertices[(i + 1) % oriented.Vertices.Count];
            anchors.Add(new Point2D((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2));
        }

        return anchors
            .GroupBy(v => (X: Math.Round(v.X, 3), Y: Math.Round(v.Y, 3)))
            .Select(g => g.First())
            .ToList();
    }

    private static Point2D GetPolygonCentroid(Polygon polygon)
    {
        if (polygon.Vertices.Count < 3)
            return polygon.Bounds.Center;

        double areaTimesSix = 0;
        double cx = 0;
        double cy = 0;

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var p0 = polygon.Vertices[i];
            var p1 = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
            double cross = p0.X * p1.Y - p1.X * p0.Y;
            areaTimesSix += cross;
            cx += (p0.X + p1.X) * cross;
            cy += (p0.Y + p1.Y) * cross;
        }

        if (Math.Abs(areaTimesSix) < 1e-9)
            return polygon.Bounds.Center;

        double factor = 1.0 / (3.0 * areaTimesSix);
        return new Point2D(cx * factor, cy * factor);
    }

    private enum PartClass { Small, Medium, Large }

    private (PartClass classification, string label) ClassifyPart(PartModel part, PlateModel plate)
    {
        double areaRatio = plate.TotalArea > 0 ? part.Area / plate.TotalArea : 0;
        double widthRatio = plate.Width > 0 ? part.Width / plate.Width : 0;
        double heightRatio = plate.Height > 0 ? part.Height / plate.Height : 0;
        int vertexCount = part.Geometry.Vertices.Count;

        if ((vertexCount > 12 && areaRatio > 0.03) || areaRatio > 0.10 || widthRatio > 0.40 || heightRatio > 0.40)
            return (PartClass.Large, "large");

        if ((vertexCount > 6 && areaRatio > 0.02) || areaRatio > 0.05 || widthRatio > 0.20 || heightRatio > 0.20)
            return (PartClass.Medium, "medium");

        return (PartClass.Small, "small");
    }

    private bool IsSmallPartCandidate(PartModel part, PlateModel plate)
    {
        double areaRatio = plate.TotalArea > 0 ? part.Area / plate.TotalArea : 0;
        double widthRatio = plate.Width > 0 ? part.Width / plate.Width : 0;
        double heightRatio = plate.Height > 0 ? part.Height / plate.Height : 0;
        return areaRatio <= 0.05 || widthRatio <= 0.25 || heightRatio <= 0.25;
    }

    private List<Point2D> GetEmptySpaceSamples(Polygon polygon, double step, int limit)
    {
        var samples = new List<Point2D>();
        if (polygon.Vertices.Count < 3)
            return samples;

        var bounds = polygon.Bounds;
        double actualStep = Math.Max(2, step);

        for (double y = bounds.MinY + actualStep * 0.5; y <= bounds.MaxY && samples.Count < limit; y += actualStep)
        {
            for (double x = bounds.MinX + actualStep * 0.5; x <= bounds.MaxX && samples.Count < limit; x += actualStep)
            {
                var point = new Point2D(x, y);
                if (!GeometryUtils.PointInPolygon(point, polygon.Vertices))
                    samples.Add(point);
            }
        }

        return samples;
    }

    private List<Point2D> GetPlateFreeSpaceSamples(NestResult result, PlateModel plate, int plateIndex, double gap, int limit)
    {
        var samples = new List<Point2D>();
        double step = Math.Max(2, gap / 2.0);
        double probeRadius = Math.Max(step * 2.0, gap * 2.0);

        for (double y = plate.Margin + step * 0.5; y <= plate.Height - plate.Margin && samples.Count < limit; y += step)
        {
            for (double x = plate.Margin + step * 0.5; x <= plate.Width - plate.Margin && samples.Count < limit; x += step)
            {
                var point = new Point2D(x, y);

                bool insideAnyPolygon = false;
                bool nearAnyPlacement = false;

                foreach (var placement in result.Placed.Where(p => p.PlateIndex == plateIndex))
                {
                    var poly = placement.TransformedGeometry;
                    if (GeometryUtils.PointInPolygon(point, poly.Vertices))
                    {
                        insideAnyPolygon = true;
                        break;
                    }

                    var bounds = poly.Bounds;
                    if (point.X >= bounds.MinX - probeRadius && point.X <= bounds.MaxX + probeRadius &&
                        point.Y >= bounds.MinY - probeRadius && point.Y <= bounds.MaxY + probeRadius)
                    {
                        nearAnyPlacement = true;
                    }
                }

                if (!insideAnyPolygon && nearAnyPlacement)
                    samples.Add(point);
            }
        }

        return samples;
    }

    private bool TryAddTrueShapeCandidate(Point2D targetPoint, Point2D localAnchor, AnchorMatchType anchorType,
        double rotation, Polygon oriented, List<PlacementCandidate> candidates, PlateModel plate, NestResult result,
        int plateIndex, double gap, PartModel part, bool gapFill, CandidateSource source = CandidateSource.Unknown)
    {
        result.TrueShapeCandidateCount++;
        result.CandidatePositionsTested++;
        if (gapFill)
            result.GapFillAttemptCount++;

        string srcStr = source switch
        {
            CandidateSource.EmptySpace => "ES",
            CandidateSource.PlateFreeSpace => "PF",
            CandidateSource.FreeRect => "FR",
            CandidateSource.Vertex => "VT",
            CandidateSource.EdgeMidpoint => "EM",
            CandidateSource.Corner => "CN",
            _ => "UK"
        };

        string atStr = anchorType switch
        {
            AnchorMatchType.VertexToVertex => "V2V",
            AnchorMatchType.VertexToEdge => "V2E",
            AnchorMatchType.EdgeToEdge => "E2E",
            _ => "?"
        };

        switch (anchorType)
        {
            case AnchorMatchType.VertexToVertex:
                result.VertexToVertexCandidateCount++;
                break;
            case AnchorMatchType.VertexToEdge:
                result.VertexToEdgeCandidateCount++;
                break;
            case AnchorMatchType.EdgeToEdge:
                result.EdgeToEdgeCandidateCount++;
                break;
        }

        double translationX = targetPoint.X - localAnchor.X;
        double translationY = targetPoint.Y - localAnchor.Y;

        if (anchorType == AnchorMatchType.VertexToEdge)
        {
            translationX += gap * 0.5;
            translationY -= gap * 0.5;
        }
        var translated = TransformPolygonForPlacement(part.Geometry, rotation, new Point2D(translationX, translationY));

        if (!IsGeometryInsideUsableArea(translated, plate))
        {
            result.RejectedByBoundaryCount++;
            TrackSourceReject(result, srcStr, "BOUNDARY");
            TrueShapeTrace.LogCandidateAttempt(part.Id, translationX, translationY, rotation, $"{srcStr}/{atStr}", $"P{plateIndex}{(gapFill ? "g" : "")}", "BOUNDARY", false);
            return false;
        }

        if (!PassesCollisionCheck(translated, result, plateIndex, part, rotation, out int bboxRejects, out int collisionChecks, out int cacheHits, out bool rejectedBySat))
        {
            result.RejectedByBoundingBoxCount += bboxRejects;
            result.BoundingBoxRejectCount += bboxRejects;
            result.CollisionCheckCount += collisionChecks;
            result.CollisionCacheHits += cacheHits;
            string rej = rejectedBySat ? "SAT" : "BBOX";
            if (rejectedBySat)
                result.RejectedBySATCount++;
            TrackSourceReject(result, srcStr, rej);
            TrueShapeTrace.LogCandidateAttempt(part.Id, translationX, translationY, rotation, $"{srcStr}/{atStr}", $"P{plateIndex}{(gapFill ? "g" : "")}", rej, false);

            if (source == CandidateSource.EmptySpace && rejectedBySat)
            {
                var b = translated.Bounds;
                double criticalDim = Math.Sqrt(part.Area) * 0.5 + Math.Min(b.Width, b.Height) * 0.5;
                int overlapIdx = -1;
                double overlapDepth = 0;
                double overlapAxX = 0, overlapAxY = 0;
                double minClearance = double.MaxValue;
                for (int pi = 0; pi < result.Placed.Count; pi++)
                {
                    var pl = result.Placed[pi];
                    if (pl.PlateIndex != plateIndex) continue;
                    if (GeometryUtils.PolygonsIntersectDetailed(translated, pl.TransformedGeometry, out double od, out Point2D oa))
                    {
                        if (od < minClearance)
                        {
                            minClearance = od;
                            overlapIdx = pi;
                            overlapDepth = od;
                            overlapAxX = oa.X;
                            overlapAxY = oa.Y;
                        }
                    }
                }
                result.EmptySpaceDiagnostics.AllCandidates.Add(new EmptySpaceCandidateDiagnostic
                {
                    TargetPoint = targetPoint,
                    LocalAnchor = localAnchor,
                    TranslationX = translationX,
                    TranslationY = translationY,
                    RotationDeg = rotation,
                    PartWidth = b.Width,
                    PartHeight = b.Height,
                    PartArea = part.Area,
                    PartCriticalDimension = criticalDim,
                    NearestPlacedDistance = MeasureMinClearance(translated, result, plateIndex, out _),
                    MinClearanceToAllPlaced = -minClearance,
                    SATFailed = true,
                    OverlapDepth = overlapDepth,
                    OverlapAxisX = overlapAxX,
                    OverlapAxisY = overlapAxY,
                    OverlapWithPlacedIndex = overlapIdx,
                    FailureReason = overlapIdx >= 0 ? $"Overlaps placed[{overlapIdx}] by {overlapDepth:F3}" : "Overlap unknown"
                });

                bool overlapRescuable = !DebugDisableRefinement && minClearance < 2.5;
                result.EmptySpaceRefinementAttemptCount++;
                if (overlapRescuable && TryRefineEmptySpaceCandidate(targetPoint, localAnchor, anchorType,
                    rotation, oriented, candidates, plate, result, plateIndex, gap, part,
                    translationX, translationY, srcStr, atStr, gapFill, out double refinedClearance))
                {
                    result.EmptySpaceRefinementSuccessCount++;
                    if (refinedClearance > result.EmptySpaceRefinementBestClearance)
                        result.EmptySpaceRefinementBestClearance = refinedClearance;

                    result.RejectedByBoundingBoxCount += bboxRejects;
                    result.BoundingBoxRejectCount += bboxRejects;
                    result.CollisionCheckCount += collisionChecks;
                    result.CollisionCacheHits += cacheHits;

                    return true;
                }
            }

            return false;
        }

        double localGapFit = CalculateLocalGapFit(translated, plate, gap, gapFill);
        var candidate = new PlacementCandidate
        {
            Geometry = translated,
            RotationDeg = rotation,
            BoundingBoxRejects = bboxRejects,
            CollisionChecks = collisionChecks,
            CacheHits = cacheHits,
            AnchorType = anchorType,
            LocalGapFit = localGapFit,
            TargetPoint = targetPoint,
            AnchorPoint = localAnchor,
            TranslationX = translationX,
            TranslationY = translationY
        };
        candidate.Score = CalculateTrueShapeScoreCandidate(candidate, result, plate, plateIndex, gapFill);

        candidates.Add(candidate);
        TrueShapeTrace.LogCandidateAttempt(part.Id, translationX, translationY, rotation, $"{srcStr}/{atStr}", $"P{plateIndex}{(gapFill ? "g" : "")}", "", true);

        result.RejectedByBoundingBoxCount += bboxRejects;
        result.BoundingBoxRejectCount += bboxRejects;
        result.CollisionCheckCount += collisionChecks;
        result.CollisionCacheHits += cacheHits;

        if (source == CandidateSource.EmptySpace)
        {
            var b = translated.Bounds;
            double criticalDim = Math.Sqrt(part.Area) * 0.5 + Math.Min(b.Width, b.Height) * 0.5;
            double clearance = MeasureMinClearance(translated, result, plateIndex, out _);
            result.EmptySpaceDiagnostics.AllCandidates.Add(new EmptySpaceCandidateDiagnostic
            {
                TargetPoint = targetPoint,
                LocalAnchor = localAnchor,
                TranslationX = translationX,
                TranslationY = translationY,
                RotationDeg = rotation,
                PartWidth = b.Width,
                PartHeight = b.Height,
                PartArea = part.Area,
                PartCriticalDimension = criticalDim,
                NearestPlacedDistance = clearance,
                MinClearanceToAllPlaced = clearance,
                SATFailed = false,
                OverlapDepth = 0,
                FailureReason = "Accepted"
            });
        }

        return true;
    }

    private static double CalculateLocalGapFit(Polygon geometry, PlateModel plate, double gap, bool gapFill)
    {
        var bounds = geometry.Bounds;
        double bboxWaste = Math.Max(0, (bounds.Width * bounds.Height) - geometry.Area);
        double edgeClearance = Math.Max(0, Math.Min(bounds.MinX - plate.Margin, bounds.MinY - plate.Margin));
        double gapBias = gapFill ? 1.25 : 0.75;
        return gapBias / (1.0 + bboxWaste + edgeClearance + gap);
    }

    private static double GetAnchorTypeBonus(AnchorMatchType anchorType)
    {
        return anchorType switch
        {
            AnchorMatchType.EdgeToEdge => 1.0,
            AnchorMatchType.VertexToEdge => 0.75,
            AnchorMatchType.VertexToVertex => 0.5,
            _ => 0
        };
    }

    private enum AnchorMatchType
    {
        VertexToVertex,
        VertexToEdge,
        EdgeToEdge
    }

    private enum CandidateSource
    {
        Unknown,
        EmptySpace,
        PlateFreeSpace,
        FreeRect,
        Vertex,
        EdgeMidpoint,
        Corner,
        GapFill
    }

    private static List<(double dx, double dy)>? _cachedRefinementOffsets;
    private static List<(double dx, double dy)> RefinementOffsets =>
        _cachedRefinementOffsets ??= GenerateRefinementOffsets();

    private static List<(double dx, double dy)> GenerateRefinementOffsets()
    {
        var offsets = new List<(double dx, double dy)>();
        for (double dx = -2; dx <= 2; dx += 0.5)
            for (double dy = -2; dy <= 2; dy += 0.5)
                if (Math.Abs(dx) > 1e-9 || Math.Abs(dy) > 1e-9)
                    offsets.Add((dx: dx, dy: dy));
        return offsets.OrderBy(o => o.dx * o.dx + o.dy * o.dy).ToList();
    }

    private bool TryRefineEmptySpaceCandidate(
        Point2D targetPoint, Point2D localAnchor, AnchorMatchType anchorType,
        double rotation, Polygon oriented, List<PlacementCandidate> candidates,
        PlateModel plate, NestResult result, int plateIndex, double gap,
        PartModel part, double originalTx, double originalTy,
        string srcStr, string atStr, bool gapFill, out double bestClearance)
    {
        bestClearance = 0;
        var bestRefined = (PlacementCandidate?)null;
        double bestScore = double.MaxValue;
        int attempts = 0;

        TrueShapeTrace.Log($"  ES_REFINEMENT_TRIED: {part.Id} original=({originalTx:F3},{originalTy:F3}) rot={rotation:F1}");

        foreach (var (dx, dy) in RefinementOffsets)
        {
            if (attempts >= 8) break;
            attempts++;

            double newTx = originalTx + dx;
            double newTy = originalTy + dy;
            var newTranslated = TransformPolygonForPlacement(part.Geometry, rotation, new Point2D(newTx, newTy));

            if (!IsGeometryInsideUsableArea(newTranslated, plate))
                continue;

            if (!PassesCollisionCheck(newTranslated, result, plateIndex, part, rotation,
                out int bboxRejects, out int collisionChecks, out int cacheHits, out bool rejectedBySat))
            {
                result.RejectedByBoundingBoxCount += bboxRejects;
                result.BoundingBoxRejectCount += bboxRejects;
                result.CollisionCheckCount += collisionChecks;
                result.CollisionCacheHits += cacheHits;
                if (rejectedBySat)
                    result.RejectedBySATCount++;
                continue;
            }

            result.RejectedByBoundingBoxCount += bboxRejects;
            result.BoundingBoxRejectCount += bboxRejects;
            result.CollisionCheckCount += collisionChecks;
            result.CollisionCacheHits += cacheHits;

            double localGapFit = CalculateLocalGapFit(newTranslated, plate, gap, gapFill);
            var refinedCandidate = new PlacementCandidate
            {
                Geometry = newTranslated,
                RotationDeg = rotation,
                BoundingBoxRejects = bboxRejects,
                CollisionChecks = collisionChecks,
                CacheHits = cacheHits,
                AnchorType = anchorType,
                LocalGapFit = localGapFit,
                TargetPoint = targetPoint,
                AnchorPoint = localAnchor,
                TranslationX = newTx,
                TranslationY = newTy
            };
            refinedCandidate.Score = CalculateTrueShapeScoreCandidate(refinedCandidate, result, plate, plateIndex, gapFill);

            if (refinedCandidate.Score < bestScore)
            {
                bestScore = refinedCandidate.Score;
                bestRefined = refinedCandidate;
            }
        }

        if (bestRefined != null)
        {
            bestClearance = MeasureMinClearance(bestRefined.Geometry, result, plateIndex, out _);
            candidates.Add(bestRefined);
            TrueShapeTrace.LogCandidateAttempt(part.Id, bestRefined.TranslationX, bestRefined.TranslationY,
                rotation, $"ES_REF/{atStr}", $"P{plateIndex}{(gapFill ? "g" : "")}", "", true);
            TrueShapeTrace.Log($"  ES_REFINEMENT_ACCEPTED: {part.Id} refined t=({bestRefined.TranslationX:F3},{bestRefined.TranslationY:F3}) clearance={bestClearance:F3} attempts={attempts}");
            return true;
        }

        TrueShapeTrace.Log($"  ES_REFINEMENT_FAILED: {part.Id} original t=({originalTx:F3},{originalTy:F3}) attempts={attempts}");
        return false;
    }

    private static double PointToSegmentDistance(Point2D p, Point2D a, Point2D b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double lengthSq = dx * dx + dy * dy;
        if (lengthSq < 1e-12)
            return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
        double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
        double projX = a.X + t * dx;
        double projY = a.Y + t * dy;
        return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
    }

    private static double MeasureMinClearance(Polygon candidate, NestResult result, int plateIndex, out int closestPlacedIndex)
    {
        double minDist = double.MaxValue;
        closestPlacedIndex = -1;

        for (int i = 0; i < result.Placed.Count; i++)
        {
            var placement = result.Placed[i];
            if (placement.PlateIndex != plateIndex) continue;

            var other = placement.TransformedGeometry;
            if (other.Vertices.Count < 3) continue;

            for (int vi = 0; vi < candidate.Vertices.Count; vi++)
            {
                var cv = candidate.Vertices[vi];
                for (int oi = 0; oi < other.Vertices.Count; oi++)
                {
                    var ov1 = other.Vertices[oi];
                    var ov2 = other.Vertices[(oi + 1) % other.Vertices.Count];
                    double d = PointToSegmentDistance(cv, ov1, ov2);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestPlacedIndex = i;
                    }
                }
            }

            for (int oi = 0; oi < other.Vertices.Count; oi++)
            {
                var ov = other.Vertices[oi];
                for (int vi = 0; vi < candidate.Vertices.Count; vi++)
                {
                    var cv1 = candidate.Vertices[vi];
                    var cv2 = candidate.Vertices[(vi + 1) % candidate.Vertices.Count];
                    double d = PointToSegmentDistance(ov, cv1, cv2);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestPlacedIndex = i;
                    }
                }
            }
        }

        return minDist;
    }

    private static void TrackSourceReject(NestResult result, string source, string rejectType)
    {
        switch (source)
        {
            case "ES":
                if (rejectType == "BOUNDARY") result.EmptySpaceBoundaryRejects++;
                else result.EmptySpaceSATRejects++;
                break;
            case "PF":
                if (rejectType == "BOUNDARY") result.PlateFreeSpaceBoundaryRejects++;
                else result.PlateFreeSpaceSATRejects++;
                break;
            case "VT":
                if (rejectType == "BOUNDARY") result.VertexBoundaryRejects++;
                else result.VertexSATRejects++;
                break;
            case "EM":
                if (rejectType == "BOUNDARY") result.EdgeBoundaryRejects++;
                else result.EdgeSATRejects++;
                break;
            case "CN":
                if (rejectType == "BOUNDARY") result.CornerBoundaryRejects++;
                else result.CornerSATRejects++;
                break;
        }
    }

    private bool TryPlaceIrregular(PartModel part, NestSettings settings, NestResult result,
        PlateModel plate, List<FreeRect> freeRects)
    {
        double gap = Math.Max(0, settings.GapBetweenParts);
        int plateIndex = result.Plates.IndexOf(plate);

        var rotations = GetAllowedRotations(settings).ToList();
        var candidates = new List<PlacementCandidate>();

        var anchors = GetAnchorPoints(result, plate, freeRects, plateIndex);

        foreach (double rotation in rotations)
        {
            var oriented = BuildOrientedGeometry(part.Geometry, rotation);
            if (oriented.Vertices.Count < 3) continue;

            var bounds = oriented.Bounds;
            double w = bounds.Width;
            double h = bounds.Height;

            foreach (var anchor in anchors)
            {
                if (candidates.Count >= MaxCandidatesPerPart) break;

                AddCandidateIfValid(anchor.X, anchor.Y, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
                if (candidates.Count >= MaxCandidatesPerPart) break;
                AddCandidateIfValid(anchor.X - w - gap, anchor.Y, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
                if (candidates.Count >= MaxCandidatesPerPart) break;
                AddCandidateIfValid(anchor.X, anchor.Y - h - gap, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
                if (candidates.Count >= MaxCandidatesPerPart) break;
                AddCandidateIfValid(anchor.X - w - gap, anchor.Y - h - gap, rotation, oriented, candidates, plate, result, plateIndex, gap, part);
            }
            if (candidates.Count >= MaxCandidatesPerPart) break;
        }

        if (candidates.Count == 0) return false;

        var best = candidates.OrderBy(c => c.Score).First();

        var placement = new NestPlacement
        {
            PartId = part.Id,
            PartName = part.Name,
            Part = part,
            X = best.Geometry.Bounds.MinX,
            Y = best.Geometry.Bounds.MinY,
            RotationDeg = best.RotationDeg,
            PlateIndex = plateIndex,
            Width = best.Geometry.Bounds.Width,
            Height = best.Geometry.Bounds.Height,
            PlacementTranslationX = best.Geometry.Bounds.MinX,
            PlacementTranslationY = best.Geometry.Bounds.MinY,
            PlacementScore = best.Score,
            TransformedGeometry = best.Geometry
        };

        result.Placed.Add(placement);
        result.UsedArea += part.Area;
        result.CollisionCheckCount += best.CollisionChecks;
        result.BoundingBoxRejectCount += best.BoundingBoxRejects;
        result.CollisionCacheHits += best.CacheHits;
        result.CandidatePositionsTested += candidates.Count;

        var targetRect = freeRects.FirstOrDefault(r => 
            placement.X >= r.X + plate.Margin - 1e-6 && 
            placement.Y >= r.Y + plate.Margin - 1e-6 &&
            placement.X + placement.Width <= r.X + r.W + plate.Margin + 1e-6 &&
            placement.Y + placement.Height <= r.Y + r.H + plate.Margin + 1e-6);

        if (targetRect != null)
        {
            SplitFreeRects(freeRects, targetRect, placement.Width + gap, placement.Height + gap);
            PruneFreeRects(freeRects);
            MergeFreeRects(freeRects);
        }

        return true;
    }

    private void FinalizeResult(NestResult result, List<FreeRect> freeRects)
    {
        result.UsedBoundingArea = result.Placed.Sum(p => p.Width * p.Height);
        _lastFreeRects = freeRects;

        if (freeRects.Count > 0)
        {
            result.LargestEmptyArea = freeRects.Max(fr => fr.W * fr.H);
            double totalGapArea = freeRects.Sum(fr => fr.W * fr.H);
            result.AverageGap = result.PlacedCount > 0 ? totalGapArea / result.PlacedCount : 0;
        }

        if (result.Placed.Count > 0)
        {
            result.AveragePlacementScore = result.Placed.Average(p => p.PlacementScore);
        }

        ValidateFinalPlacements(result);
    }

    private HashSet<Point2D> GetAnchorPoints(NestResult result, PlateModel plate, List<FreeRect> freeRects, int plateIndex)
    {
        var anchors = new HashSet<Point2D>(new Point2DComparer(0.1));

        // Plate origin
        anchors.Add(new Point2D(plate.Margin, plate.Margin));

        // Free Rect corners
        foreach (var rect in freeRects)
        {
            anchors.Add(new Point2D(rect.X + plate.Margin, rect.Y + plate.Margin));
            anchors.Add(new Point2D(rect.X + rect.W + plate.Margin, rect.Y + plate.Margin));
            anchors.Add(new Point2D(rect.X + plate.Margin, rect.Y + rect.H + plate.Margin));
            anchors.Add(new Point2D(rect.X + rect.W + plate.Margin, rect.Y + rect.H + plate.Margin));
        }

        // Existing part corners and vertices
        foreach (var p in result.Placed.Where(pl => pl.PlateIndex == plateIndex))
        {
            var b = p.TransformedGeometry.Bounds;
            anchors.Add(new Point2D(b.MinX, b.MinY));
            anchors.Add(new Point2D(b.MaxX, b.MinY));
            anchors.Add(new Point2D(b.MinX, b.MaxY));
            anchors.Add(new Point2D(b.MaxX, b.MaxY));

            foreach (var v in p.TransformedGeometry.Vertices)
            {
                anchors.Add(v);
            }
        }

        return anchors;
    }

    private void AddCandidateIfValid(double x, double y, double rotation, Polygon oriented, 
        List<PlacementCandidate> candidates, PlateModel plate, NestResult result, int plateIndex, double gap, PartModel part)
    {
        var translated = GeometryUtils.Translate(oriented, x, y);

        if (!IsGeometryInsideUsableArea(translated, plate))
            return;

        if (!PassesCollisionCheck(translated, result, plateIndex, part, rotation, out int bboxRejects, out int collisionChecks, out int cacheHits, out _))
            return;

        double score = ScoreCandidate(translated, result, plateIndex);

        candidates.Add(new PlacementCandidate
        {
            Geometry = translated,
            RotationDeg = rotation,
            Score = score,
            BoundingBoxRejects = bboxRejects,
            CollisionChecks = collisionChecks,
            CacheHits = cacheHits
        });
    }

    private static IEnumerable<double> GetAllowedRotations(NestSettings settings)
    {
        if (settings.AllowAdvancedRotation)
        {
            for (int i = 0; i < 360; i += 15)
                yield return i;
        }
        else
        {
            yield return 0;
            yield return 90;
            yield return 180;
            yield return 270;
        }
    }

    private static Polygon BuildOrientedGeometry(Polygon source, double rotationDeg)
    {
        if (!IsValidSourcePolygon(source))
            return GeometryUtils.CreateRectangle(Math.Max(0, source.Bounds.Width), Math.Max(0, source.Bounds.Height));

        var normalized = GeometryUtils.Translate(source, -source.Bounds.MinX, -source.Bounds.MinY);
        return GeometryUtils.RotateAndNormalize(normalized, rotationDeg);
    }

    private static Polygon TransformPolygonForPlacement(Polygon source, double rotationDeg, Point2D translation)
    {
        var oriented = BuildOrientedGeometry(source, rotationDeg);
        if (oriented.Vertices.Count == 0)
            return oriented;

        return GeometryUtils.Translate(oriented, translation.X, translation.Y);
    }

    private bool PassesCollisionCheck(Polygon candidate, NestResult result, int plateIndex,
        PartModel part, double rotation, out int bboxRejects, out int collisionChecks, out int cacheHits, out bool rejectedBySAT)
    {
        bboxRejects = 0;
        collisionChecks = 0;
        cacheHits = 0;
        rejectedBySAT = false;

        var candidateBounds = candidate.Bounds;

        for (int i = 0; i < result.Placed.Count; i++)
        {
            var placement = result.Placed[i];
            if (placement.PlateIndex != plateIndex) continue;

            var other = placement.TransformedGeometry;

            bool bboxIntersects = candidateBounds.Intersects(other.Bounds);

            // Bounding box is only a fast prefilter; overlap must still be validated by SAT.
            if (!bboxIntersects)
            {
                bboxRejects++;
                continue;
            }

            // Cache Check - key includes part.Id, rotation, and rounded position
            var cacheKey = (part.Id, rotation, (int)Math.Round(candidateBounds.MinX * 10), (int)Math.Round(candidateBounds.MinY * 10), i);
            if (_collisionCache.TryGetValue(cacheKey, out bool collides))
            {
                cacheHits++;
                if (collides)
                {
                    rejectedBySAT = true;
                    return false;
                }
                result.BoundingBoxOverlapButSATClearAccepted++;
                continue;
            }

            // Exact SAT Check
            collisionChecks++;
            bool intersects = GeometryUtils.PolygonsIntersect(candidate, other);
            _collisionCache[cacheKey] = intersects;

            if (intersects)
            {
                rejectedBySAT = true;
                return false;
            }

            result.BoundingBoxOverlapButSATClearAccepted++;
        }

        return true;
    }

    private static double ScoreCandidate(Polygon geometry, NestResult result, int plateIndex)
    {
        var b = geometry.Bounds;
        
        // Priority per FAZ 8B:
        // 1. En dÃ¼ÅŸÃ¼k Y (MinY)
        // 2. En dÃ¼ÅŸÃ¼k X (MinX)
        // 3. En dÃ¼ÅŸÃ¼k kullanÄ±lan yÃ¼kseklik (MaxY of entire plate)
        // 4. En dÃ¼ÅŸÃ¼k boÅŸ alan (approx by MaxX)
        // 5. En yÃ¼ksek sÄ±kÄ±ÅŸÄ±klÄ±k (approx by bounding box area)

        double currentMaxY = result.Placed.Where(p => p.PlateIndex == plateIndex).Select(p => p.TransformedGeometry.Bounds.MaxY).DefaultIfEmpty(0).Max();
        double usedHeight = Math.Max(currentMaxY, b.MaxY);

        return (b.MinY * 1e12) + (b.MinX * 1e9) + (usedHeight * 1e6) + (b.MaxX * 1e3) + (b.Width * b.Height);
    }

    private void SplitFreeRects(List<FreeRect> freeRects, FreeRect target, double partW, double partH)
    {
        freeRects.Remove(target);

        double rightW = target.W - partW;
        if (rightW > 1e-6)
        {
            freeRects.Add(new FreeRect(
                target.X + partW,
                target.Y,
                rightW,
                target.H)); // Corrected: should be target.H for Maximal Rectangles logic
        }

        double topH = target.H - partH;
        if (topH > 1e-6)
        {
            freeRects.Add(new FreeRect(
                target.X,
                target.Y + partH,
                target.W,
                topH));
        }
    }

    private void PruneFreeRects(List<FreeRect> freeRects)
    {
        for (int i = freeRects.Count - 1; i >= 0; i--)
        {
            for (int j = freeRects.Count - 1; j >= 0; j--)
            {
                if (i == j) continue;
                if (i >= freeRects.Count || j >= freeRects.Count) continue;

                if (freeRects[i].Contains(freeRects[j]))
                {
                    freeRects.RemoveAt(j);
                    if (j < i) i--;
                }
            }
        }
    }

    private void MergeFreeRects(List<FreeRect> freeRects)
    {
        bool merged = true;
        while (merged)
        {
            merged = false;

            for (int i = 0; i < freeRects.Count; i++)
            {
                for (int j = i + 1; j < freeRects.Count; j++)
                {
                    var a = freeRects[i];
                    var b = freeRects[j];

                    if (Math.Abs(a.H - b.H) < 1e-6 &&
                        Math.Abs(a.Y - b.Y) < 1e-6 &&
                        Math.Abs(a.X + a.W - b.X) < 1e-6)
                    {
                        freeRects[i] = new FreeRect(a.X, a.Y, a.W + b.W, a.H);
                        freeRects.RemoveAt(j);
                        merged = true;
                        break;
                    }

                    if (Math.Abs(a.H - b.H) < 1e-6 &&
                        Math.Abs(a.Y - b.Y) < 1e-6 &&
                        Math.Abs(b.X + b.W - a.X) < 1e-6)
                    {
                        freeRects[i] = new FreeRect(b.X, b.Y, b.W + a.W, b.H);
                        freeRects.RemoveAt(j);
                        merged = true;
                        break;
                    }

                    if (Math.Abs(a.W - b.W) < 1e-6 &&
                        Math.Abs(a.X - b.X) < 1e-6 &&
                        Math.Abs(a.Y + a.H - b.Y) < 1e-6)
                    {
                        freeRects[i] = new FreeRect(a.X, a.Y, a.W, a.H + b.H);
                        freeRects.RemoveAt(j);
                        merged = true;
                        break;
                    }

                    if (Math.Abs(a.W - b.W) < 1e-6 &&
                        Math.Abs(a.X - b.X) < 1e-6 &&
                        Math.Abs(b.Y + b.H - a.Y) < 1e-6)
                    {
                        freeRects[i] = new FreeRect(b.X, b.Y, b.W, b.H + a.H);
                        freeRects.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
                if (merged) break;
            }
        }
    }

    private void ValidateFinalPlacements(NestResult result)
    {
        var valid = new List<NestPlacement>();
        var newUnplaced = new List<PartModel>(result.Unplaced);

        foreach (var placement in result.Placed.OrderBy(p => p.PlateIndex).ThenBy(p => p.PlacementScore))
        {
            var plate = result.Plates.ElementAtOrDefault(placement.PlateIndex);
            if (plate == null)
            {
                result.Warnings.Add($"{placement.PartName}: plaka referansÄ± bulunamadÄ±.");
                if (placement.Part != null)
                    newUnplaced.Add(placement.Part);
                continue;
            }

            if (!IsGeometryInsideUsableArea(placement.TransformedGeometry, plate))
            {
                result.Warnings.Add($"{placement.PartName}: final validation sÄ±rasÄ±nda plaka sÄ±nÄ±rÄ± dÄ±ÅŸÄ±nda bulundu ve yerleÅŸtirilmeyenlere alÄ±ndÄ±.");
                if (placement.Part != null)
                    newUnplaced.Add(placement.Part);
                continue;
            }

            bool collision = false;
            foreach (var other in valid.Where(p => p.PlateIndex == placement.PlateIndex))
            {
                if (!placement.TransformedGeometry.Bounds.Intersects(other.TransformedGeometry.Bounds))
                    continue;

                result.CollisionCheckCount++;
                if (GeometryUtils.PolygonsIntersect(placement.TransformedGeometry, other.TransformedGeometry))
                {
                    collision = true;
                    break;
                }
            }

            if (collision)
            {
                result.Warnings.Add($"{placement.PartName}: final validation sÄ±rasÄ±nda Ã§akÄ±ÅŸma bulundu ve yerleÅŸtirilmeyenlere alÄ±ndÄ±.");
                if (placement.Part != null)
                    newUnplaced.Add(placement.Part);
                continue;
            }

            valid.Add(placement);
        }

        result.Placed = valid;
        result.Unplaced = newUnplaced;
        result.UsedArea = valid.Sum(p => p.Part?.Area ?? 0);
        result.UsedBoundingArea = valid.Sum(p => p.Width * p.Height);
    }

    private static bool IsGeometryInsideUsableArea(Polygon geometry, PlateModel plate)
    {
        var b = geometry.Bounds;
        return b.MinX >= plate.Margin - 1e-6 &&
               b.MinY >= plate.Margin - 1e-6 &&
               b.MaxX <= plate.Width - plate.Margin + 1e-6 &&
               b.MaxY <= plate.Height - plate.Margin + 1e-6;
    }

    private static bool IsValidSourcePolygon(Polygon polygon)
    {
        if (polygon == null || polygon.Vertices == null || polygon.Vertices.Count == 0)
            return false;

        return polygon.Vertices.All(v =>
            !double.IsNaN(v.X) && !double.IsNaN(v.Y) &&
            !double.IsInfinity(v.X) && !double.IsInfinity(v.Y));
    }

    private PlateModel ClonePlate(PlateModel source) => new()
    {
        Id = source.Id,
        Width = source.Width,
        Height = source.Height,
        Margin = source.Margin,
        Gap = source.Gap,
        MaterialThickness = source.MaterialThickness
    };

    private class FreeRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }

        public FreeRect(double x, double y, double w, double h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public bool Contains(FreeRect other)
            => X <= other.X + 1e-6 && Y <= other.Y + 1e-6 &&
               X + W >= other.X + other.W - 1e-6 && Y + H >= other.Y + other.H - 1e-6;
    }

    private List<FreeRect> InitFreeRects(PlateModel plate)
    {
        return new List<FreeRect>
        {
            new(0, 0, plate.UsableWidth, plate.UsableHeight)
        };
    }

    private sealed class PlacementCandidate
    {
        public FreeRect Rect { get; set; } = null!;
        public Polygon Geometry { get; set; } = new();
        public double RotationDeg { get; set; }
        public double FitW { get; set; }
        public double FitH { get; set; }
        public double Score { get; set; }
        public int BoundingBoxRejects { get; set; }
        public int CollisionChecks { get; set; }
        public int CacheHits { get; set; }
        public AnchorMatchType AnchorType { get; set; }
        public double LocalGapFit { get; set; }
        public Point2D TargetPoint { get; set; }
        public Point2D AnchorPoint { get; set; }
        public double TranslationX { get; set; }
        public double TranslationY { get; set; }
    }

    private class Point2DComparer : IEqualityComparer<Point2D>
    {
        private readonly double _epsilon;
        public Point2DComparer(double epsilon) => _epsilon = epsilon;
        public bool Equals(Point2D a, Point2D b)
        {
            return Math.Abs(a.X - b.X) < _epsilon && Math.Abs(a.Y - b.Y) < _epsilon;
        }
        public int GetHashCode(Point2D p)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Math.Round(p.X / _epsilon).GetHashCode();
                hash = hash * 23 + Math.Round(p.Y / _epsilon).GetHashCode();
                return hash;
            }
        }
    }
}
