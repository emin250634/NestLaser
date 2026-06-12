using System;
using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Models;

public class PartModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Parça";
    public string SourceFile { get; set; } = string.Empty;
    public string LayerName { get; set; } = string.Empty;
    public Polygon Geometry { get; set; } = new();
    public double Area => Geometry.Area;
    public double Width => Geometry.Bounds.Width;
    public double Height => Geometry.Bounds.Height;
    public int Quantity { get; set; } = 1;

    public PartModel Clone() => new()
    {
        Id = Id,
        Name = Name,
        SourceFile = SourceFile,
        LayerName = LayerName,
        Geometry = Geometry.Clone(),
        Quantity = Quantity
    };
}
