using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class Part
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Part";
    public List<Point2D> Vertices { get; set; } = new();
    public double Width { get; set; }
    public double Height { get; set; }
    public double Area { get; set; }

    public void CalculateBounds()
    {
        if (Vertices.Count == 0) return;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var v in Vertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
        }

        Width = maxX - minX;
        Height = maxY - minY;

        Area = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            int j = (i + 1) % Vertices.Count;
            Area += Vertices[i].X * Vertices[j].Y;
            Area -= Vertices[j].X * Vertices[i].Y;
        }
        Area = System.Math.Abs(Area) / 2.0;
    }

    public Part Clone() => new()
    {
        Id = Id,
        Name = Name,
        Vertices = new List<Point2D>(Vertices),
        Width = Width,
        Height = Height,
        Area = Area
    };
}
