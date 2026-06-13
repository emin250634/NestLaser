using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public class NestingEngine
{
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

                if (!PassesCollisionCheck(translated, result, plateIndex, part, rotation, out int bbr, out int cc, out int ch))
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

        if (!PassesCollisionCheck(translated, result, plateIndex, part, rotation, out int bboxRejects, out int collisionChecks, out int cacheHits))
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

    private bool PassesCollisionCheck(Polygon candidate, NestResult result, int plateIndex, 
        PartModel part, double rotation, out int bboxRejects, out int collisionChecks, out int cacheHits)
    {
        bboxRejects = 0;
        collisionChecks = 0;
        cacheHits = 0;

        var candidateBounds = candidate.Bounds;

        for (int i = 0; i < result.Placed.Count; i++)
        {
            var placement = result.Placed[i];
            if (placement.PlateIndex != plateIndex) continue;

            var other = placement.TransformedGeometry;
            
            // Bounding Box Pre-check
            if (!candidateBounds.Intersects(other.Bounds))
            {
                bboxRejects++;
                continue;
            }

            // Cache Check - key includes part.Id, rotation, and rounded position
            var cacheKey = (part.Id, rotation, (int)Math.Round(candidateBounds.MinX * 10), (int)Math.Round(candidateBounds.MinY * 10), i);
            if (_collisionCache.TryGetValue(cacheKey, out bool collides))
            {
                cacheHits++;
                if (collides) return false;
                continue;
            }

            // Exact SAT Check
            collisionChecks++;
            bool intersects = GeometryUtils.PolygonsIntersect(candidate, other);
            _collisionCache[cacheKey] = intersects;

            if (intersects)
                return false;
        }

        return true;
    }

    private static double ScoreCandidate(Polygon geometry, NestResult result, int plateIndex)
    {
        var b = geometry.Bounds;
        
        // Priority per FAZ 8B:
        // 1. En düşük Y (MinY)
        // 2. En düşük X (MinX)
        // 3. En düşük kullanılan yükseklik (MaxY of entire plate)
        // 4. En düşük boş alan (approx by MaxX)
        // 5. En yüksek sıkışıklık (approx by bounding box area)

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
                result.Warnings.Add($"{placement.PartName}: plaka referansı bulunamadı.");
                if (placement.Part != null)
                    newUnplaced.Add(placement.Part);
                continue;
            }

            if (!IsGeometryInsideUsableArea(placement.TransformedGeometry, plate))
            {
                result.Warnings.Add($"{placement.PartName}: final validation sırasında plaka sınırı dışında bulundu ve yerleştirilmeyenlere alındı.");
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
                result.Warnings.Add($"{placement.PartName}: final validation sırasında çakışma bulundu ve yerleştirilmeyenlere alındı.");
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
