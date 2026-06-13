using System;
using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Models;

public class NestPlacement
{
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public PartModel Part { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double RotationDeg { get; set; }
    public int PlateIndex { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double PlacementScore { get; set; }
    public Polygon TransformedGeometry { get; set; } = new();
}
