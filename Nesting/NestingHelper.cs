using System.Collections.Generic;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public static class NestingHelper
{
    public static IEnumerable<double> GetAllowedRotations(NestSettings settings)
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
            if (settings.AllowRotation90)
            {
                yield return 180;
                yield return 270;
            }
        }
    }

    public static Polygon BuildOrientedGeometry(Polygon source, double rotationDeg)
    {
        if (!IsValidSourcePolygon(source))
            return GeometryUtils.CreateRectangle(
                Math.Max(0, source.Bounds.Width),
                Math.Max(0, source.Bounds.Height));

        var normalized = GeometryUtils.Translate(source, -source.Bounds.MinX, -source.Bounds.MinY);
        return GeometryUtils.RotateAndNormalize(normalized, rotationDeg);
    }

    public static Polygon TransformPolygonForPlacement(Polygon source, double rotationDeg, Point2D translation)
    {
        var oriented = BuildOrientedGeometry(source, rotationDeg);
        if (oriented.Vertices.Count == 0)
            return oriented;

        return GeometryUtils.Translate(oriented, translation.X, translation.Y);
    }

    public static bool IsGeometryInsideUsableArea(Polygon geometry, PlateModel plate)
    {
        var b = geometry.Bounds;
        return b.MinX >= plate.Margin - 0.01 &&
               b.MinY >= plate.Margin - 0.01 &&
               b.MaxX <= plate.Width - plate.Margin + 0.01 &&
               b.MaxY <= plate.Height - plate.Margin + 0.01;
    }

    public static bool IsValidSourcePolygon(Polygon polygon)
    {
        if (polygon == null || polygon.Vertices == null || polygon.Vertices.Count == 0)
            return false;

        return polygon.Vertices.All(v =>
            !double.IsNaN(v.X) && !double.IsNaN(v.Y) &&
            !double.IsInfinity(v.X) && !double.IsInfinity(v.Y));
    }

    public static PlateModel ClonePlate(PlateModel source) => new()
    {
        Id = source.Id,
        Width = source.Width,
        Height = source.Height,
        Margin = source.Margin,
        Gap = source.Gap,
        MaterialThickness = source.MaterialThickness
    };

    public static List<FreeRect> InitFreeRects(PlateModel plate)
    {
        return new List<FreeRect>
        {
            new(0, 0, plate.UsableWidth, plate.UsableHeight)
        };
    }

    public static void SplitFreeRects(List<FreeRect> freeRects, FreeRect target, double partW, double partH)
    {
        freeRects.Remove(target);

        double rightW = target.W - partW;
        if (rightW > 1e-6)
        {
            freeRects.Add(new FreeRect(
                target.X + partW,
                target.Y,
                rightW,
                target.H));
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

    public static void PruneFreeRects(List<FreeRect> freeRects)
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

    public static void MergeFreeRects(List<FreeRect> freeRects)
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
}