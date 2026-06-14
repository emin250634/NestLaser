using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting.Algorithms;

public class ShapeAwarePlacement : INestingAlgorithm
{
    public string AlgorithmName => "Shape-Aware Bottom-Left Fill";
    public string AlgorithmVersion => "2.0";
    public bool IsExperimental => true;

    private readonly Stopwatch _stopwatch = new();
    private readonly Dictionary<(string, double, int, int, int), bool> _collisionCache = new();

    private const int MaxCandidatesPerPart = 150;
    private const int MaxPlacementAttempts = 5000;

    public NestResult Nest(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        _stopwatch.Restart();
        _collisionCache.Clear();

        var result = new NestResult
        {
            AlgorithmName = AlgorithmName,
            PolygonCollisionEnabled = true
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

        int totalAttempts = 0;

        foreach (var part in sorted)
        {
            if (totalAttempts > MaxPlacementAttempts)
            {
                result.Warnings.Add($"Max placement attempts ({MaxPlacementAttempts}) reached, stopping");
                break;
            }

            bool placed = TryPlacePolygon(part, settings, result, currentPlate);

            if (!placed)
            {
                currentPlate = NestingHelper.ClonePlate(plate);
                result.Plates.Add(currentPlate);
                placed = TryPlacePolygon(part, settings, result, currentPlate);
            }

            if (!placed)
                result.Unplaced.Add(part);
            else
                totalAttempts++;
        }

        FinalizeResult(result);

        _stopwatch.Stop();
        result.NestingTimeMs = _stopwatch.ElapsedMilliseconds;
        return result;
    }

    private bool TryPlacePolygon(PartModel part, NestSettings settings, NestResult result, PlateModel plate)
    {
        double gap = Math.Max(0, settings.GapBetweenParts);
        int plateIndex = result.Plates.IndexOf(plate);

        var candidates = new List<ScoredCandidate>();

        var rotations = new List<double> { 0 };
        if (settings.AllowRotation90)
        {
            rotations.Add(90);
            rotations.Add(180);
            rotations.Add(270);
        }

        var anchorPoints = GenerateAnchorPoints(result, plate, plateIndex, gap);

        foreach (double rotation in rotations)
        {
            var oriented = NestingHelper.BuildOrientedGeometry(part.Geometry, rotation);
            if (oriented.Vertices.Count < 3) continue;

            var orientedBounds = oriented.Bounds;
            double partW = orientedBounds.Width + gap;
            double partH = orientedBounds.Height + gap;

            var localAnchors = GenerateLocalAnchors(oriented);

            foreach (var anchor in anchorPoints)
            {
                foreach (var localAnchor in localAnchors)
                {
                    if (candidates.Count >= MaxCandidatesPerPart) break;

                    double tx = anchor.X - localAnchor.X;
                    double ty = anchor.Y - localAnchor.Y;

                    var translated = GeometryUtils.Translate(oriented, tx, ty);

                    if (!NestingHelper.IsGeometryInsideUsableArea(translated, plate))
                        continue;

                    if (!PassesCollisionCheck(translated, result, plateIndex, part))
                    {
                        result.RejectedBySATCount++;
                        continue;
                    }

                    double score = CalculateShapeScore(translated, result, plateIndex, plate, anchor);

                    candidates.Add(new ScoredCandidate
                    {
                        Geometry = translated,
                        RotationDeg = rotation,
                        Score = score,
                        PlacementX = translated.Bounds.MinX,
                        PlacementY = translated.Bounds.MinY
                    });
                }
                if (candidates.Count >= MaxCandidatesPerPart) break;
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
            X = best.PlacementX,
            Y = best.PlacementY,
            RotationDeg = best.RotationDeg,
            PlateIndex = plateIndex,
            Width = best.Geometry.Bounds.Width,
            Height = best.Geometry.Bounds.Height,
            PlacementTranslationX = best.PlacementX,
            PlacementTranslationY = best.PlacementY,
            PlacementScore = best.Score,
            TransformedGeometry = best.Geometry
        };

        result.Placed.Add(placement);
        result.UsedArea += part.Area;

        return true;
    }

    private List<Point2D> GenerateAnchorPoints(NestResult result, PlateModel plate, int plateIndex, double gap)
    {
        var anchors = new HashSet<(double, double)>(new Point2DHashComparer(0.5));
        var resultAnchors = new List<Point2D>();

        void AddAnchor(double x, double y)
        {
            var key = (Math.Round(x, 1), Math.Round(y, 1));
            if (anchors.Add(key))
                resultAnchors.Add(new Point2D(x, y));
        }

        AddAnchor(plate.Margin, plate.Margin);

        foreach (var placed in result.Placed.Where(p => p.PlateIndex == plateIndex))
        {
            var geom = placed.TransformedGeometry;

            AddAnchor(geom.Bounds.MinX, geom.Bounds.MinY);
            AddAnchor(geom.Bounds.MaxX, geom.Bounds.MinY);
            AddAnchor(geom.Bounds.MinX, geom.Bounds.MaxY);
            AddAnchor(geom.Bounds.MaxX, geom.Bounds.MaxY);

            foreach (var v in geom.Vertices)
            {
                AddAnchor(v.X, v.Y);
                AddAnchor(v.X + gap, v.Y);
                AddAnchor(v.X - gap, v.Y);
                AddAnchor(v.X, v.Y + gap);
                AddAnchor(v.X, v.Y - gap);
            }

            for (int i = 0; i < geom.Vertices.Count; i++)
            {
                var v1 = geom.Vertices[i];
                var v2 = geom.Vertices[(i + 1) % geom.Vertices.Count];
                AddAnchor((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2);
            }
        }

        AddAnchor(plate.Margin, plate.Height - plate.Margin);
        AddAnchor(plate.Width - plate.Margin, plate.Margin);
        AddAnchor(plate.Width - plate.Margin, plate.Height - plate.Margin);

        return resultAnchors;
    }

    private List<Point2D> GenerateLocalAnchors(Polygon polygon)
    {
        var anchors = new List<Point2D>();

        anchors.Add(polygon.Bounds.Center);
        anchors.Add(new Point2D(polygon.Bounds.MinX, polygon.Bounds.MinY));
        anchors.Add(new Point2D(polygon.Bounds.MaxX, polygon.Bounds.MinY));
        anchors.Add(new Point2D(polygon.Bounds.MinX, polygon.Bounds.MaxY));
        anchors.Add(new Point2D(polygon.Bounds.MaxX, polygon.Bounds.MaxY));

        foreach (var v in polygon.Vertices)
            anchors.Add(v);

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var v1 = polygon.Vertices[i];
            var v2 = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
            anchors.Add(new Point2D((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2));
        }

        return anchors
            .GroupBy(p => (Math.Round(p.X, 2), Math.Round(p.Y, 2)))
            .Select(g => g.First())
            .ToList();
    }

    private double CalculateShapeScore(Polygon candidate, NestResult result, int plateIndex, PlateModel plate, Point2D anchor)
    {
        double score = 0;

        double bottomLeftScore = candidate.Bounds.MinY * 100000 + candidate.Bounds.MinX * 10;
        score += bottomLeftScore;

        double edgeContact = 0;
        foreach (var placed in result.Placed.Where(p => p.PlateIndex == plateIndex))
        {
            var pg = placed.TransformedGeometry;
            foreach (var cv in candidate.Vertices)
            {
                foreach (var pv in pg.Vertices)
                {
                    double dist = Math.Sqrt((cv.X - pv.X) * (cv.X - pv.X) + (cv.Y - pv.Y) * (cv.Y - pv.Y));
                    if (dist < 5) edgeContact += (5 - dist);
                }
            }
        }
        score -= edgeContact * 50;

        double bboxArea = candidate.Bounds.Width * candidate.Bounds.Height;
        double waste = bboxArea - candidate.Area;
        score += waste * 0.1;

        return -score;
    }

    private bool PassesCollisionCheck(Polygon candidate, NestResult result, int plateIndex, PartModel part)
    {
        var candidateBounds = candidate.Bounds;

        foreach (var placement in result.Placed.Where(p => p.PlateIndex == plateIndex))
        {
            var other = placement.TransformedGeometry;

            if (!candidateBounds.Intersects(other.Bounds))
            {
                result.BoundingBoxRejectCount++;
                result.CandidatePositionsTested++;
                continue;
            }

            result.CandidatePositionsTested++;
            result.CollisionCheckCount++;

            var cacheKey = (part.Id, 0.0, (int)Math.Round(candidateBounds.MinX), (int)Math.Round(candidateBounds.MinY), result.Placed.IndexOf(placement));
            if (_collisionCache.TryGetValue(cacheKey, out bool cachedResult))
            {
                if (cachedResult) return false;
                continue;
            }

            bool intersects = GeometryUtils.PolygonsIntersect(candidate, other);
            _collisionCache[cacheKey] = intersects;

            if (intersects)
                return false;

            result.BoundingBoxOverlapButSATClearAccepted++;
        }

        return true;
    }

    private void FinalizeResult(NestResult result)
    {
        double totalBoundArea = 0;
        foreach (var placement in result.Placed)
        {
            if (placement.TransformedGeometry != null)
                totalBoundArea += placement.TransformedGeometry.Bounds.Width * placement.TransformedGeometry.Bounds.Height;
        }
        result.UsedBoundingArea = totalBoundArea;
    }

    private class ScoredCandidate
    {
        public Polygon Geometry { get; set; } = new();
        public double RotationDeg { get; set; }
        public double Score { get; set; }
        public double PlacementX { get; set; }
        public double PlacementY { get; set; }
    }

    private class Point2DHashComparer : IEqualityComparer<(double, double)>
    {
        private readonly double _tolerance;

        public Point2DHashComparer(double tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Equals((double, double) x, (double, double) y)
        {
            return Math.Abs(x.Item1 - y.Item1) < _tolerance &&
                   Math.Abs(x.Item2 - y.Item2) < _tolerance;
        }

        public int GetHashCode((double, double) obj)
        {
            int ix = (int)Math.Round(obj.Item1 / _tolerance);
            int iy = (int)Math.Round(obj.Item2 / _tolerance);
            return HashCode.Combine(ix, iy);
        }
    }
}