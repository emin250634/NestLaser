using System.Collections.Generic;
using System.Linq;

namespace NestLaserDesktop.Models;

public class UndoSnapshot
{
    public List<PartModel> Parts { get; set; } = new();
    public List<LayerModel> Layers { get; set; } = new();
    public List<LaserOperation> Operations { get; set; } = new();
    public List<int> SelectedIndices { get; set; } = new();
    public string? SelectedLayerId { get; set; }
    public NestResult? NestResult { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public double TotalPartsArea { get; set; }
    public string Description { get; set; } = "";
}

public class NestResult
{
    public List<NestPlacement> Placed { get; set; } = new();
    public List<PartModel> Unplaced { get; set; } = new();
    public List<PlateModel> Plates { get; set; } = new();
    public double UsedArea { get; set; }
    public double UsedBoundingArea { get; set; }
    public long NestingTimeMs { get; set; }
    public string AlgorithmName { get; set; } = "Free Rectangle (Guillotine)";
    public string AlgorithmUsed { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
    public bool IsTimeout { get; set; }
    public bool PolygonCollisionEnabled { get; set; }
    public int CollisionCheckCount { get; set; }
    public int BoundingBoxRejectCount { get; set; }
    public int PlacementAttempts { get; set; }
    public int CandidatePositionsTested { get; set; }
    public int CollisionCacheHits { get; set; }
    public double AveragePlacementScore { get; set; }
    public double AverageGap { get; set; }
    public double LargestEmptyArea { get; set; }
    public List<string> Warnings { get; set; } = new();

    public double TotalPlateArea => Plates.Sum(p => p.TotalArea);
    public double UsedPlateArea => Placed.Sum(p => p.Width * p.Height);
    public double Efficiency => TotalPlateArea > 0 ? (UsedArea / TotalPlateArea) * 100 : 0;
    public double WastePercent => TotalPlateArea > 0 ? ((TotalPlateArea - UsedArea) / TotalPlateArea) * 100 : 0;
    public double WasteRate => WastePercent;
    public int TotalParts => Placed.Count + Unplaced.Count;
    public int PlacedCount => Placed.Count;
    public int UnplacedCount => Unplaced.Count;
    public int PlateCount => Plates.Count;
    public string UsedAreaText => UsedArea > 0 ? $"{UsedArea:F0} mm²" : "--";
    public string TotalPlateAreaText => TotalPlateArea > 0 ? $"{TotalPlateArea:F0} mm²" : "--";
    public string NestingTimeText => NestingTimeMs > 0 ? $"{NestingTimeMs} ms" : "--";
    public string AverageGapText => AverageGap > 0 ? $"{AverageGap:F1} mm" : "--";
    public string LargestEmptyAreaText => LargestEmptyArea > 0 ? $"{LargestEmptyArea:F0} mm²" : "--";
}
