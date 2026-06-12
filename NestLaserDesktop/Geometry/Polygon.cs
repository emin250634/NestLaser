using System;
using System.Collections.Generic;
using System.Linq;

namespace NestLaserDesktop.Geometry;

public class Polygon
{
    public List<Point2D> Vertices { get; set; } = new();
    public double Area { get; private set; }
    public BoundingBox Bounds { get; private set; }

    public void Calculate()
    {
        CalculateArea();
        CalculateBounds();
    }

    private void CalculateArea()
    {
        if (Vertices.Count < 3) { Area = 0; return; }

        double a = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            int j = (i + 1) % Vertices.Count;
            a += Vertices[i].X * Vertices[j].Y;
            a -= Vertices[j].X * Vertices[i].Y;
        }
        Area = Math.Abs(a) / 2.0;
    }

    private void CalculateBounds()
    {
        if (Vertices.Count == 0)
        {
            Bounds = new BoundingBox(0, 0, 0, 0);
            return;
        }

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var v in Vertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
        }

        Bounds = new BoundingBox(minX, minY, maxX, maxY);
    }

    public void NormalizeWinding()
    {
        if (Vertices.Count < 3) return;

        double a = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            int j = (i + 1) % Vertices.Count;
            a += Vertices[i].X * Vertices[j].Y;
            a -= Vertices[j].X * Vertices[i].Y;
        }

        if (a < 0)
            Vertices.Reverse();
    }

    public Polygon Clone() => new()
    {
        Vertices = new List<Point2D>(Vertices),
        Area = Area,
        Bounds = Bounds
    };

    public Polygon Transform(double tx, double ty, bool rotate90)
    {
        var result = new Polygon();
        foreach (var v in Vertices)
        {
            if (rotate90)
                result.Vertices.Add(new Point2D(tx + v.Y, ty - v.X + v.Y));
            else
                result.Vertices.Add(new Point2D(tx + v.X, ty + v.Y));
        }
        result.Calculate();
        return result;
    }
}
