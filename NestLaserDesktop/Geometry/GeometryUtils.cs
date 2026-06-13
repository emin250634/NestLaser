using System;
using System.Collections.Generic;
using System.Linq;

namespace NestLaserDesktop.Geometry;

public static class GeometryUtils
{
    public static List<Point2D> CircleToPolygon(Point2D center, double radius, int segments = 36)
    {
        var vertices = new List<Point2D>();
        for (int i = 0; i < segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            vertices.Add(new Point2D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle)));
        }
        return vertices;
    }

    public static Polygon CreateRectangle(double width, double height)
    {
        var polygon = new Polygon();
        polygon.Vertices.Add(new Point2D(0, 0));
        polygon.Vertices.Add(new Point2D(width, 0));
        polygon.Vertices.Add(new Point2D(width, height));
        polygon.Vertices.Add(new Point2D(0, height));
        polygon.Calculate();
        return polygon;
    }

    public static double CrossProduct(Point2D o, Point2D a, Point2D b)
        => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

    public static Polygon Translate(Polygon polygon, double dx, double dy)
    {
        var result = new Polygon();
        if (polygon == null || polygon.Vertices.Count == 0)
            return result;

        result.Vertices = polygon.Vertices.Select(v => new Point2D(v.X + dx, v.Y + dy)).ToList();
        result.Calculate();
        return result;
    }

    public static Polygon RotateAroundOrigin(Polygon polygon, double degrees)
    {
        var result = new Polygon();
        if (polygon == null || polygon.Vertices.Count == 0)
            return result;

        double radians = degrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        foreach (var v in polygon.Vertices)
        {
            result.Vertices.Add(new Point2D(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos));
        }

        result.Calculate();
        return result;
    }

    public static Polygon RotateAndNormalize(Polygon polygon, double degrees)
    {
        var rotated = RotateAroundOrigin(polygon, degrees);
        if (rotated.Vertices.Count == 0)
            return rotated;

        double minX = rotated.Vertices.Min(v => v.X);
        double minY = rotated.Vertices.Min(v => v.Y);
        if (Math.Abs(minX) > 1e-9 || Math.Abs(minY) > 1e-9)
            rotated = Translate(rotated, -minX, -minY);

        rotated.Calculate();
        rotated.NormalizeWinding();
        return rotated;
    }

    public static bool PolygonsIntersect(Polygon a, Polygon b, double epsilon = 1e-9)
    {
        if (a == null || b == null) return false;
        if (a.Vertices.Count < 3 || b.Vertices.Count < 3) return false;

        var axes = GetSeparatingAxes(a).Concat(GetSeparatingAxes(b));
        foreach (var axis in axes)
        {
            ProjectPolygon(a, axis, out double minA, out double maxA);
            ProjectPolygon(b, axis, out double minB, out double maxB);

            if (maxA <= minB + epsilon || maxB <= minA + epsilon)
                return false;
        }

        return true;
    }

    private static IEnumerable<Point2D> GetSeparatingAxes(Polygon polygon)
    {
        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var p1 = polygon.Vertices[i];
            var p2 = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-9) continue;

            yield return new Point2D(-dy / length, dx / length);
        }
    }

    private static void ProjectPolygon(Polygon polygon, Point2D axis, out double min, out double max)
    {
        min = double.MaxValue;
        max = double.MinValue;

        foreach (var vertex in polygon.Vertices)
        {
            double projection = vertex.X * axis.X + vertex.Y * axis.Y;
            if (projection < min) min = projection;
            if (projection > max) max = projection;
        }
    }

    public static bool PointInPolygon(Point2D point, List<Point2D> vertices)
    {
        if (vertices.Count < 3) return false;
        bool inside = false;
        int j = vertices.Count - 1;
        for (int i = 0; i < vertices.Count; i++)
        {
            if ((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y) &&
                point.X < (vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X)
                inside = !inside;
            j = i;
        }
        return inside;
    }

    public static bool PolygonContainsPolygon(Polygon outer, Polygon inner)
    {
        if (outer == null || inner == null || outer.Vertices.Count < 3 || inner.Vertices.Count < 3)
            return false;

        var outerBounds = outer.Bounds;
        var innerBounds = inner.Bounds;

        if (!outerBounds.Contains(innerBounds))
            return false;

        int insideCount = 0;
        foreach (var v in inner.Vertices)
        {
            if (PointInPolygon(v, outer.Vertices))
                insideCount++;
        }

        return insideCount >= inner.Vertices.Count / 2;
    }

}
