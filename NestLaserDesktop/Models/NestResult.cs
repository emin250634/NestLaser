using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class NestResult
{
    public List<NestPlacement> Placed { get; set; } = new();
    public List<PartModel> Unplaced { get; set; } = new();
    public double UsedArea { get; set; }
    public double PlateArea { get; set; }
    public double Efficiency => PlateArea > 0 ? (UsedArea / PlateArea) * 100 : 0;
    public double WasteRate => 100 - Efficiency;
    public int TotalParts => Placed.Count + Unplaced.Count;
    public int PlacedCount => Placed.Count;
    public int UnplacedCount => Unplaced.Count;
}
