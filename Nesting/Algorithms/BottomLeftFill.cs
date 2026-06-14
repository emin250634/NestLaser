using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public class BottomLeftFill : INestingAlgorithm
{
    public string AlgorithmName => "Bottom-Left Fill (Guillotine)";
    public string AlgorithmVersion => "1.0";
    public bool IsExperimental => false;

    private readonly Dictionary<(string partId, double rot, int x, int y, int otherIdx), bool> _collisionCache = new();
    private readonly Stopwatch _stopwatch = new();

    public NestResult Nest(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        _stopwatch.Restart();
        _collisionCache.Clear();

        var result = new NestResult
        {
            AlgorithmName = AlgorithmName,
            PolygonCollisionEnabled = false
        };

        if (parts.Count == 0)
        {
            _stopwatch.Stop();
            result.NestingTimeMs = _stopwatch.ElapsedMilliseconds;
            return result;
        }

        var sorted = parts
            .OrderByDescending(p => p.Area)
            .ThenByDescending(p => Math.Max(p.Width, p.Height))
            .ToList();

        var currentPlate = NestingHelper.ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var freeRects = NestingHelper.InitFreeRects(currentPlate);

        foreach (var part in sorted)
        {
            bool placed = TryPlace(part, settings, result, currentPlate, freeRects);

            if (!placed)
            {
                currentPlate = NestingHelper.ClonePlate(plate);
                result.Plates.Add(currentPlate);
                freeRects = NestingHelper.InitFreeRects(currentPlate);
                placed = TryPlace(part, settings, result, currentPlate, freeRects);
            }

            if (!placed)
                result.Unplaced.Add(part);
        }

        FinalizeResult(result, freeRects);

        _stopwatch.Stop();
        result.NestingTimeMs = _stopwatch.ElapsedMilliseconds;
        return result;
    }

    private bool TryPlace(PartModel part, NestSettings settings, NestResult result,
        PlateModel plate, List<FreeRect> freeRects)
    {
        double gap = Math.Max(0, settings.GapBetweenParts);
        int plateIndex = result.Plates.IndexOf(plate);

        var candidates = new List<PlacementCandidate>();

        foreach (var rect in freeRects)
        {
            foreach (double rotation in NestingHelper.GetAllowedRotations(settings))
            {
                var oriented = NestingHelper.BuildOrientedGeometry(part.Geometry, rotation);
                var bounds = oriented.Bounds;
                double fitW = bounds.Width + gap;
                double fitH = bounds.Height + gap;

                if (fitW > rect.W + 1e-6 || fitH > rect.H + 1e-6)
                    continue;

                double x = rect.X + plate.Margin;
                double y = rect.Y + plate.Margin;
                var translated = GeometryUtils.Translate(oriented, x, y);

                if (!NestingHelper.IsGeometryInsideUsableArea(translated, plate))
                    continue;

                double score = rect.Y * 1000000 + rect.X;

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

        if (candidates.Count == 0)
            return false;

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

        NestingHelper.SplitFreeRects(freeRects, best.Rect, best.FitW, best.FitH);
        NestingHelper.PruneFreeRects(freeRects);
        NestingHelper.MergeFreeRects(freeRects);

        return true;
    }

    private void FinalizeResult(NestResult result, List<FreeRect> freeRects)
    {
        if (result.Plates.Count == 0) return;

        double totalBoundArea = 0;
        foreach (var placement in result.Placed)
        {
            var geom = placement.TransformedGeometry;
            if (geom != null)
                totalBoundArea += geom.Bounds.Width * geom.Bounds.Height;
        }
        result.UsedBoundingArea = totalBoundArea;
    }

    private class PlacementCandidate
    {
        public FreeRect Rect { get; set; } = null!;
        public Polygon Geometry { get; set; } = new();
        public double RotationDeg { get; set; }
        public double FitW { get; set; }
        public double FitH { get; set; }
        public double Score { get; set; }
    }
}