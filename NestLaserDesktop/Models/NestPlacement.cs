using System;
using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Models;

public class NestPlacement
{
    public PartModel Part { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double RotationDeg { get; set; }
    public Polygon TransformedGeometry { get; set; } = new();
}
