using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public static class TrueShapeTrace
{
    private static readonly StringBuilder _sb = new();
    private static bool _enabled = true;

    public static void Enable() => _enabled = true;
    public static void Disable() => _enabled = false;

    public static string GetContent()
    {
        return _sb.ToString();
    }

    public static void Clear()
    {
        if (!_enabled) return;
        _sb.Clear();
    }

    public static void Log(string message)
    {
        if (!_enabled) return;
        _sb.AppendLine(message);
    }

    public static void LogPartGeometry(PartModel part)
    {
        if (!_enabled) return;
        var g = part.Geometry;
        var b = g.Bounds;
        Log($"  GEOMETRY: {part.Id}|{part.Name} | Verts={g.Vertices.Count} Area={g.Area:F3} W={b.Width:F3} H={b.Height:F3} BBox=({b.MinX:F3},{b.MinY:F3})-({b.MaxX:F3},{b.MaxY:F3})");
    }

    public static void LogCandidateAttempt(string partId, double tx, double ty, double rot,
        string anchorType, string plateLabel, string rejectReason, bool accepted)
    {
        if (!_enabled) return;
        string status = accepted ? "ACCEPTED" : $"REJECTED({rejectReason})";
        Log($"    CANDIDATE: {partId} t=({tx:F3},{ty:F3}) rot={rot:F1} type={anchorType} plate={plateLabel} => {status}");
    }

    public static void LogPlateAttemptSummary(string partId, int plateIndex, bool gapPriority,
        int generated, int boundaryReject, int satReject, int accepted)
    {
        if (!_enabled) return;
        Log($"  PLATE{plateIndex}{(gapPriority ? "(gap)" : "")}: gen={generated} boundRej={boundaryReject} satRej={satReject} accepted={accepted}");
    }

    public static void LogPlacement(string partId, int finalPlate, double x, double y, double rot, bool gapFill)
    {
        if (!_enabled) return;
        Log($"  PLACED: {partId} => Plate{finalPlate} at ({x:F3},{y:F3}) rot={rot:F1} {(gapFill ? "(gap-fill)" : "")}");
    }

    public static void LogTime(string partId, long ms)
    {
        if (!_enabled) return;
        Log($"  TIME: {partId} took {ms}ms");
    }

    public static void LogSeparator(char ch = '-', int count = 60)
    {
        if (!_enabled) return;
        _sb.AppendLine(new string(ch, count));
    }

    public static void WriteToFile(string filePath)
    {
        if (!_enabled || _sb.Length == 0) return;
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, _sb.ToString());
        }
        catch
        {
        }
    }

    public static string GetTrace() => _sb.ToString();

    public static void DumpEmptySpaceDiagnostics(NestResult result)
    {
        if (!_enabled) return;
        var diag = result.EmptySpaceDiagnostics;
        if (diag.AllCandidates.Count == 0) return;

        LogSeparator('=');
        Log("EMPTY SPACE CANDIDATE DIAGNOSTICS");
        LogSeparator('=');
        Log($"Total ES samples generated: {diag.TotalESAttempts}");
        Log($"ES boundary rejects: {diag.TotalESBoundaryRejects}");
        Log($"ES SAT rejects: {diag.TotalESSATRejects}");
        Log($"ES accepted (via SAT): {diag.TotalESAccepted}");
        Log($"Required clearance (critical dim): {diag.RequiredClearance:F3}");
        Log($"Max clearance found: {diag.MaxClearanceFound:F3}");
        Log($"Best ES candidate: {diag.BestEmptySpaceCandidate}");
        Log($"Best ES candidate SAT result: {diag.BestEmptySpaceCandidateSATResult}");

        int maxShow = Math.Min(diag.AllCandidates.Count, 20);
        if (maxShow < diag.AllCandidates.Count)
            Log($"Showing first {maxShow} of {diag.AllCandidates.Count} candidates:");

        LogSeparator();
        for (int i = 0; i < maxShow; i++)
        {
            var c = diag.AllCandidates[i];
            string sat = c.SATFailed ? $"SAT-FAIL overlap={c.OverlapDepth:F3} with[{c.OverlapWithPlacedIndex}] ({c.FailureReason})" : "SAT-PASS";
            Log($"  [{i}] t=({c.TranslationX:F3},{c.TranslationY:F3}) rot={c.RotationDeg:F1} target=({c.TargetPoint.X:F3},{c.TargetPoint.Y:F3}) anchor=({c.LocalAnchor.X:F3},{c.LocalAnchor.Y:F3}) part={c.PartWidth:F3}x{c.PartHeight:F3} critDim={c.PartCriticalDimension:F3} clearance={c.MinClearanceToAllPlaced:F3} nearestDist={c.NearestPlacedDistance:F3} => {sat}");
        }

        int satCount = diag.AllCandidates.Count(c => c.SATFailed);
        int passCount = diag.AllCandidates.Count - satCount;
        double avgOverlap = diag.AllCandidates.Where(c => c.SATFailed).Select(c => c.OverlapDepth).DefaultIfEmpty(0).Average();
        double avgClearance = diag.AllCandidates.Where(c => !c.SATFailed).Select(c => c.MinClearanceToAllPlaced).DefaultIfEmpty(0).Average();
        Log($"\n  SAT fails: {satCount}, SAT passes: {passCount}");
        Log($"  Avg overlap depth (failed): {avgOverlap:F3}");
        Log($"  Avg clearance (passed): {avgClearance:F3}");

        // Root cause determination
        double smallestGap = diag.AllCandidates.Where(c => c.SATFailed).Select(c => -c.MinClearanceToAllPlaced).DefaultIfEmpty(double.MaxValue).Min();
        double largestGap = diag.AllCandidates.Where(c => !c.SATFailed).Select(c => c.MinClearanceToAllPlaced).DefaultIfEmpty(0).Max();
        bool anyPass = passCount > 0;

        LogSeparator();
        Log("ROOT CAUSE ANALYSIS:");
        if (result.EmptySpaceRefinementAttemptCount > 0)
        {
            Log($"  EmptySpaceRefinementAttemptCount: {result.EmptySpaceRefinementAttemptCount}");
            Log($"  EmptySpaceRefinementSuccessCount: {result.EmptySpaceRefinementSuccessCount}");
            Log($"  EmptySpaceRefinementBestClearance: {result.EmptySpaceRefinementBestClearance:F3}");
        }
        if (anyPass && largestGap >= diag.RequiredClearance)
            Log("  VERDICT: ES candidates CAN physically fit. Failures likely due to anchor translation missing true gap center.");
        else if (anyPass && largestGap < diag.RequiredClearance)
            Log($"  VERDICT: Some ES candidates pass SAT but clearance ({largestGap:F3}) < required ({diag.RequiredClearance:F3}). Gap is barely sufficient but anchor alignment off.");
        else if (!anyPass && smallestGap > 0)
            Log($"  VERDICT: PHYSICALLY INSUFFICIENT GAP. Closest any ES candidate gets without overlap: {smallestGap:F3} < required {diag.RequiredClearance:F3}");
        else if (!anyPass && diag.TotalESAttempts < 10)
            Log($"  VERDICT: GRID STEP TOO COARSE. Only {diag.TotalESAttempts} ES samples generated. Increase density or lower step minimum.");
        else if (!anyPass)
            Log($"  VERDICT: PHYSICALLY INSUFFICIENT GAP OR ANCHOR MISALIGNMENT. {diag.TotalESSATRejects} candidates all overlap by avg {avgOverlap:F3}. Required clearance: {diag.RequiredClearance:F3}");
        else
            Log("  VERDICT: Undetermined.");
    }

    public static void DumpAllParts(IEnumerable<PartModel> parts)
    {
        if (!_enabled) return;
        LogSeparator('=');
        Log("PART GEOMETRY DUMP");
        LogSeparator('=');
        foreach (var part in parts.OrderByDescending(p => p.Area))
        {
            Log($"Part: {part.Id}|{part.Name}");
            LogPartGeometry(part);
        }
    }

    public static void DumpResult(NestResult result)
    {
        if (!_enabled) return;
        LogSeparator('=');
        Log("PLACEMENT RESULT");
        LogSeparator('=');
        Log($"Total placed: {result.PlacedCount}");
        Log($"Total unplaced: {result.Unplaced.Count}");
        Log($"Plates used: {result.Plates.Count}");
        Log($"TrueShapeCandidateCount: {result.TrueShapeCandidateCount}");
        Log($"VertexToVertexCandidateCount: {result.VertexToVertexCandidateCount}");
        Log($"VertexToEdgeCandidateCount: {result.VertexToEdgeCandidateCount}");
        Log($"EdgeToEdgeCandidateCount: {result.EdgeToEdgeCandidateCount}");
        Log($"GapFillAttemptCount: {result.GapFillAttemptCount}");
        Log($"GapFillSuccessCount: {result.GapFillSuccessCount}");
        Log($"RejectedByBoundaryCount: {result.RejectedByBoundaryCount}");
        Log($"RejectedByBoundingBoxCount: {result.RejectedByBoundingBoxCount}");
        Log($"RejectedBySATCount: {result.RejectedBySATCount}");
        Log($"BoundingBoxOverlapButSATClearAccepted: {result.BoundingBoxOverlapButSATClearAccepted}");
        Log($"SmallPartGapSuccess: {result.SmallPartGapSuccess}");
        Log($"NewPlateAvoidedCount: {result.NewPlateAvoidedCount}");
        Log($"BoundingBoxRejectCount: {result.BoundingBoxRejectCount}");
        Log($"CollisionCheckCount: {result.CollisionCheckCount}");
        Log($"CollisionCacheHits: {result.CollisionCacheHits}");
        Log($"CandidateLimitHitCount: {result.CandidateLimitHitCount}");
        Log($"EmptySpaceCandidateCount: {result.EmptySpaceCandidateCount}");
        Log($"PlateFreeSpaceCandidateCount: {result.PlateFreeSpaceCandidateCount}");
        Log($"VertexAnchorCandidateCount: {result.VertexAnchorCandidateCount}");
        Log($"EdgeMidpointCandidateCount: {result.EdgeMidpointCandidateCount}");
        Log($"CornerAnchorCandidateCount: {result.CornerAnchorCandidateCount}");
        Log($"GapFillBeforeNewPlateTried: {result.GapFillBeforeNewPlateTried}");
        Log($"GapFillBeforeNewPlateAccepted: {result.GapFillBeforeNewPlateAccepted}");
        Log($"GapFillBeforeNewPlateRejected: {result.GapFillBeforeNewPlateRejected}");
        Log($"NewPlateOpenedAfterGapFillFailed: {result.NewPlateOpenedAfterGapFillFailed}");
        Log($"DebugForceExistingPlateModeFailures: {result.DebugForceExistingPlateModeFailures}");
        Log($"Per-source rejects: ES(B={result.EmptySpaceBoundaryRejects} S={result.EmptySpaceSATRejects}) PF(B={result.PlateFreeSpaceBoundaryRejects} S={result.PlateFreeSpaceSATRejects}) VT(B={result.VertexBoundaryRejects} S={result.VertexSATRejects}) EM(B={result.EdgeBoundaryRejects} S={result.EdgeSATRejects}) CN(B={result.CornerBoundaryRejects} S={result.CornerSATRejects})");
        Log($"ESRefinementAttempts: {result.EmptySpaceRefinementAttemptCount}");
        Log($"ESRefinementSuccesses: {result.EmptySpaceRefinementSuccessCount}");
        Log($"ESRefinementBestClearance: {result.EmptySpaceRefinementBestClearance:F3}");
        Log($"IsTimeout: {result.IsTimeout}");
        Log($"FallbackUsed: {result.FallbackUsed}");

        LogSeparator();
        foreach (var p in result.Placed)
        {
            var b = p.TransformedGeometry?.Bounds;
            if (b.HasValue)
                Log($"  Plate{p.PlateIndex}: {p.PartId} area={p.Part?.Area:F3} at ({b.Value.MinX:F3},{b.Value.MinY:F3}) rot={p.RotationDeg:F1}");
            else
                Log($"  Plate{p.PlateIndex}: {p.PartId} (no geometry)");
        }

        foreach (var u in result.Unplaced)
            Log($"  UNPLACED: {u.Id}|{u.Name} area={u.Area:F3}");

        if (result.TrueShapeDebugReport != null)
            Log($"\nDebugReport:\n{result.TrueShapeDebugReport}");
    }
}
