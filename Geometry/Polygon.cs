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

    public void Scale(double factor)
    {
        if (Vertices.Count == 0 || Math.Abs(factor - 1.0) < 1e-9) return;

        CalculateBounds();
        double cx = (Bounds.MinX + Bounds.MaxX) / 2.0;
        double cy = (Bounds.MinY + Bounds.MaxY) / 2.0;

        for (int i = 0; i < Vertices.Count; i++)
        {
            var v = Vertices[i];
            Vertices[i] = new Point2D(cx + (v.X - cx) * factor, cy + (v.Y - cy) * factor);
        }

        Calculate();
    }

    public void Scale(double scaleX, double scaleY)
    {
        if (Vertices.Count == 0 ||
            (Math.Abs(scaleX - 1.0) < 1e-9 && Math.Abs(scaleY - 1.0) < 1e-9))
            return;

        CalculateBounds();
        double cx = (Bounds.MinX + Bounds.MaxX) / 2.0;
        double cy = (Bounds.MinY + Bounds.MaxY) / 2.0;

        for (int i = 0; i < Vertices.Count; i++)
        {
            var v = Vertices[i];
            Vertices[i] = new Point2D(cx + (v.X - cx) * scaleX, cy + (v.Y - cy) * scaleY);
        }

        Calculate();
    }

    public void ScaleAround(double centerX, double centerY, double scaleX, double scaleY)
    {
        if (Vertices.Count == 0 ||
            (Math.Abs(scaleX - 1.0) < 1e-9 && Math.Abs(scaleY - 1.0) < 1e-9))
            return;

        for (int i = 0; i < Vertices.Count; i++)
        {
            var v = Vertices[i];
            Vertices[i] = new Point2D(centerX + (v.X - centerX) * scaleX, centerY + (v.Y - centerY) * scaleY);
        }

        Calculate();
    }

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

    public void Rotate90AroundCenter()
    {
        if (Vertices.Count < 2) return;
        CalculateBounds();
        double cx = (Bounds.MinX + Bounds.MaxX) / 2.0;
        double cy = (Bounds.MinY + Bounds.MaxY) / 2.0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            double dx = Vertices[i].X - cx;
            double dy = Vertices[i].Y - cy;
            Vertices[i] = new Point2D(cx - dy, cy + dx);
        }
        Calculate();
    }

    public void RotateAroundCenter(double degrees)
    {
        if (Vertices.Count < 2 || Math.Abs(degrees) < 1e-9) return;

        CalculateBounds();
        double cx = (Bounds.MinX + Bounds.MaxX) / 2.0;
        double cy = (Bounds.MinY + Bounds.MaxY) / 2.0;
        double radians = degrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        for (int i = 0; i < Vertices.Count; i++)
        {
            double dx = Vertices[i].X - cx;
            double dy = Vertices[i].Y - cy;
            Vertices[i] = new Point2D(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
        }

        Calculate();
    }

    public void RotateAround(double centerX, double centerY, double degrees)
    {
        if (Vertices.Count < 2 || Math.Abs(degrees) < 1e-9) return;

        double radians = degrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        for (int i = 0; i < Vertices.Count; i++)
        {
            double dx = Vertices[i].X - centerX;
            double dy = Vertices[i].Y - centerY;
            Vertices[i] = new Point2D(centerX + dx * cos - dy * sin, centerY + dx * sin + dy * cos);
        }

        Calculate();
    }

    public void MirrorX()
    {
        if (Vertices.Count < 2) return;
        CalculateBounds();
        double cx = (Bounds.MinX + Bounds.MaxX) / 2.0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] = new Point2D(2.0 * cx - Vertices[i].X, Vertices[i].Y);
        }
        NormalizeWinding();
        Calculate();
    }

    public void MirrorY()
    {
        if (Vertices.Count < 2) return;
        CalculateBounds();
        double cy = (Bounds.MinY + Bounds.MaxY) / 2.0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] = new Point2D(Vertices[i].X, 2.0 * cy - Vertices[i].Y);
        }
        NormalizeWinding();
        Calculate();
    }

    public bool IsValid()
    {
        if (Vertices.Count < 3) return false;
        if (Area < 1e-9) return false;
        foreach (var v in Vertices)
        {
            if (double.IsNaN(v.X) || double.IsNaN(v.Y) ||
                double.IsInfinity(v.X) || double.IsInfinity(v.Y))
                return false;
        }
        return true;
    }

    public int CleanupVertices(double tolerance = 1e-6)
    {
        if (Vertices.Count < 3) return 0;

        int removed = 0;

        var cleaned = new List<Point2D> { Vertices[0] };
        for (int i = 1; i < Vertices.Count; i++)
        {
            if (cleaned[^1].DistanceTo(Vertices[i]) < tolerance)
                removed++;
            else
                cleaned.Add(Vertices[i]);
        }

        if (cleaned.Count >= 2 && cleaned[0].DistanceTo(cleaned[^1]) < tolerance)
        {
            cleaned.RemoveAt(cleaned.Count - 1);
            removed++;
        }

        if (cleaned.Count < 3) { Vertices = cleaned; Calculate(); return removed; }

        bool changed = true;
        while (changed && cleaned.Count >= 3)
        {
            changed = false;
            for (int i = 0; i < cleaned.Count; i++)
            {
                var prev = cleaned[(i - 1 + cleaned.Count) % cleaned.Count];
                var curr = cleaned[i];
                var next = cleaned[(i + 1) % cleaned.Count];

                double cross = (prev.X - curr.X) * (next.Y - curr.Y) - (prev.Y - curr.Y) * (next.X - curr.X);
                if (Math.Abs(cross) < tolerance)
                {
                    cleaned.RemoveAt(i);
                    removed++;
                    changed = true;
                    break;
                }
            }
        }

        Vertices = cleaned;
        if (cleaned.Count >= 3) Calculate();
        return removed;
    }

    public void Move(double dx, double dy)
    {
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] = new Point2D(Vertices[i].X + dx, Vertices[i].Y + dy);
        }
        Calculate();
    }
}
