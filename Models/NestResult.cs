using System.Collections.Generic;
using System.Linq;
using NestLaserDesktop.Geometry;

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
    public int TrueShapeCandidateCount { get; set; }
    public int VertexToVertexCandidateCount { get; set; }
    public int VertexToEdgeCandidateCount { get; set; }
    public int EdgeToEdgeCandidateCount { get; set; }
    public int GapFillAttemptCount { get; set; }
    public int GapFillSuccessCount { get; set; }
    public int RejectedByBoundaryCount { get; set; }
    public int RejectedByBoundingBoxCount { get; set; }
    public int RejectedBySATCount { get; set; }
    public int AcceptedShapeAwareCount { get; set; }
    public int BoundingBoxOverlapButSATClearAccepted { get; set; }
    public int SmallPartGapCandidates { get; set; }
    public int SmallPartGapSuccess { get; set; }
    public int SamePlateGapSuccess { get; set; }
    public int NewPlateAvoidedCount { get; set; }
    public bool MultiPlateOverlayFixed { get; set; } = true;
    public int PreValidationPlacedCount { get; set; }
    public int PreGapFillUnplacedCount { get; set; }
    public int GapFillInputCount { get; set; }
    public int EmptySpaceCandidateCount { get; set; }
    public int PlateFreeSpaceCandidateCount { get; set; }
    public int VertexAnchorCandidateCount { get; set; }
    public int EdgeMidpointCandidateCount { get; set; }
    public int CornerAnchorCandidateCount { get; set; }
    public int CandidateLimitHitCount { get; set; }
    public int GapFillBeforeNewPlateTried { get; set; }
    public int GapFillBeforeNewPlateAccepted { get; set; }
    public int GapFillBeforeNewPlateRejected { get; set; }
    public int NewPlateOpenedAfterGapFillFailed { get; set; }
    public int DebugForceExistingPlateModeFailures { get; set; }
    public int EmptySpaceBoundaryRejects { get; set; }
    public int EmptySpaceSATRejects { get; set; }
    public int PlateFreeSpaceBoundaryRejects { get; set; }
    public int PlateFreeSpaceSATRejects { get; set; }
    public int VertexBoundaryRejects { get; set; }
    public int VertexSATRejects { get; set; }
    public int EdgeBoundaryRejects { get; set; }
    public int EdgeSATRejects { get; set; }
    public int CornerBoundaryRejects { get; set; }
    public int CornerSATRejects { get; set; }
    public int EmptySpaceRefinementAttemptCount { get; set; }
    public int EmptySpaceRefinementSuccessCount { get; set; }
    public double EmptySpaceRefinementBestClearance { get; set; }
    public int LargePartCandidateLimit { get; set; }
    public int MediumPartCandidateLimit { get; set; }
    public int SmallPartCandidateLimit { get; set; }
    public int LargePartFastPathUsed { get; set; }
    public int TimeoutAvoidedByAdaptiveBudget { get; set; }
    public int TrueShapePlateOpenCount { get; set; }
    public int TrueShapeMultiPlateContinuationCount { get; set; }
    public int FallbackAvoidedByMultiPlate { get; set; }
    public double AveragePlacementScore { get; set; }
    public double AverageGap { get; set; }
    public double LargestEmptyArea { get; set; }
    public List<string> Warnings { get; set; } = new();
    public EmptySpaceDiagnosticSummary EmptySpaceDiagnostics { get; set; } = new();
    public string TrueShapeDebugReport =>
        $"Candidates={TrueShapeCandidateCount} | V-V={VertexToVertexCandidateCount} | V-E={VertexToEdgeCandidateCount} | E-E={EdgeToEdgeCandidateCount} | GapFill={GapFillAttemptCount}/{GapFillSuccessCount} | SmallGap={SmallPartGapCandidates}/{SmallPartGapSuccess} | SamePlateGap={SamePlateGapSuccess} | NewPlateAvoided={NewPlateAvoidedCount} | GapFillInput={GapFillInputCount} | BoundaryRejects={RejectedByBoundaryCount} | BBoxRejects={RejectedByBoundingBoxCount} | SATRejects={RejectedBySATCount} | BBoxOverlapSATClear={BoundingBoxOverlapButSATClearAccepted} | Accepted={AcceptedShapeAwareCount} | LimitHit={CandidateLimitHitCount} | EmptySpaceCand={EmptySpaceCandidateCount} | PlateFreeCand={PlateFreeSpaceCandidateCount} | VertexCand={VertexAnchorCandidateCount} | EdgeMidCand={EdgeMidpointCandidateCount} | CornerCand={CornerAnchorCandidateCount} | GapFillBeforeNew={GapFillBeforeNewPlateTried}/{GapFillBeforeNewPlateAccepted}/{GapFillBeforeNewPlateRejected}/{NewPlateOpenedAfterGapFillFailed} | MultiPlateOverlayFixed={MultiPlateOverlayFixed} | PreValidationPlaced={PreValidationPlacedCount} | PreGapFillUnplaced={PreGapFillUnplacedCount} | Fallback={FallbackUsed}";

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

public class EmptySpaceCandidateDiagnostic
{
    public Point2D TargetPoint { get; set; }
    public Point2D LocalAnchor { get; set; }
    public double TranslationX { get; set; }
    public double TranslationY { get; set; }
    public double RotationDeg { get; set; }
    public double PartWidth { get; set; }
    public double PartHeight { get; set; }
    public double PartArea { get; set; }
    public double PartCriticalDimension { get; set; }
    public double NearestPlacedDistance { get; set; }
    public double MinClearanceToAllPlaced { get; set; }
    public bool SATFailed { get; set; }
    public double OverlapDepth { get; set; }
    public double OverlapAxisX { get; set; }
    public double OverlapAxisY { get; set; }
    public int OverlapWithPlacedIndex { get; set; }
    public string FailureReason { get; set; } = "";
}

public class EmptySpaceDiagnosticSummary
{
    public double MaxClearanceFound { get; set; }
    public double RequiredClearance { get; set; }
    public string BestEmptySpaceCandidate { get; set; } = "";
    public bool BestEmptySpaceCandidateSATResult { get; set; }
    public int TotalESAttempts { get; set; }
    public int TotalESBoundaryRejects { get; set; }
    public int TotalESSATRejects { get; set; }
    public int TotalESAccepted { get; set; }
    public List<EmptySpaceCandidateDiagnostic> AllCandidates { get; set; } = new();
}
