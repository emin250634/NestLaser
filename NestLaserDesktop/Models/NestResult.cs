using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class NestPlacement
{
    public Part Part { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double RotationDeg { get; set; }
    public List<Point2D> TransformedVertices { get; set; } = new();
}

public class NestResult
{
    public List<NestPlacement> Placed { get; set; } = new();
    public List<Part> Unplaced { get; set; } = new();
    public double UsedArea { get; set; }
    public double PlateArea { get; set; }
    public double Efficiency => PlateArea > 0 ? (UsedArea / PlateArea) * 100 : 0;
    public double WasteRate => 100 - Efficiency;
}
