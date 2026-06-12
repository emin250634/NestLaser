using System.Collections.Generic;
using System.Linq;

namespace NestLaserDesktop.Models;

public class NestResult
{
    public List<NestPlacement> Placed { get; set; } = new();
    public List<PartModel> Unplaced { get; set; } = new();
    public List<PlateModel> Plates { get; set; } = new();
    public double UsedArea { get; set; }
    public double TotalPlateArea => Plates.Sum(p => p.TotalArea);
    public double Efficiency => TotalPlateArea > 0 ? (UsedArea / TotalPlateArea) * 100 : 0;
    public double WasteRate => 100 - Efficiency;
    public int TotalParts => Placed.Count + Unplaced.Count;
    public int PlacedCount => Placed.Count;
    public int UnplacedCount => Unplaced.Count;
    public int PlateCount => Plates.Count;
    public string UsedAreaText => UsedArea > 0 ? $"{UsedArea:F0} mm²" : "--";
    public string TotalPlateAreaText => TotalPlateArea > 0 ? $"{TotalPlateArea:F0} mm²" : "--";
}
