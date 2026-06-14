using System;
using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Models;

public class PartModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Parça";
    public string SourceFile { get; set; } = string.Empty;
    public string LayerName { get; set; } = string.Empty;
    public string LayerId { get; set; } = string.Empty;
    public Polygon Geometry { get; set; } = new();
    public double Area => Geometry.Area;
    public double Width => Geometry.Bounds.Width;
    public double Height => Geometry.Bounds.Height;
    public int Quantity { get; set; } = 1;
    public bool IsScaled { get; set; }
    public double ScaleFactor { get; set; } = 1.0;
    public bool IsInnerCandidate { get; set; }
    public bool IsOuterCandidate { get; set; }
    public bool IsPlaced { get; set; }

    public PartModel Clone() => new()
    {
        Id = Id,
        Name = Name,
        SourceFile = SourceFile,
        LayerName = LayerName,
        LayerId = LayerId,
        Geometry = Geometry.Clone(),
        Quantity = Quantity,
        IsScaled = IsScaled,
        ScaleFactor = ScaleFactor,
        IsInnerCandidate = IsInnerCandidate,
        IsOuterCandidate = IsOuterCandidate,
        IsPlaced = IsPlaced
    };
}
