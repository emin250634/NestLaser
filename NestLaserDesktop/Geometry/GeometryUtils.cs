using System;
using System.Collections.Generic;

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
}
