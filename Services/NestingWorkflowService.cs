using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NestLaserDesktop.Models;
using NestLaserDesktop.Nesting;

namespace NestLaserDesktop.Services;

public class BenchmarkWorkflowResult
{
    public NestResult FreeRectangle { get; init; } = new();
    public NestResult PolygonCollision { get; init; } = new();
    public NestResult Irregular { get; init; } = new();
    public NestResult Best { get; init; } = new();
    public string SummaryText { get; init; } = string.Empty;
}

public static class NestingWorkflowService
{
    public static Task<NestResult> RunNestingAsync(
        IReadOnlyList<PartModel> parts,
        PlateModel plate,
        NestSettings settings,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("Nesting motoru çalışıyor...", 35));
            var engine = new NestingEngine();
            var sw = Stopwatch.StartNew();
            var result = engine.Run(parts.ToList(), plate, settings);
            sw.Stop();
            result.NestingTimeMs = sw.ElapsedMilliseconds;
            progress?.Report(new WorkflowProgress("Nesting sonucu doğrulanıyor...", 90));
            return result;
        }, cancellationToken);
    }

    public static Task<BenchmarkWorkflowResult> RunBenchmarkAsync(
        IReadOnlyList<PartModel> parts,
        PlateModel plate,
        NestSettings settings,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("NESTING BENCHMARK RAPORU");
            sb.AppendLine("========================");
            sb.AppendLine($"Parça: {parts.Count} | Plaka: {plate.Width}x{plate.Height} mm | Boşluk: {settings.GapBetweenParts} mm");
            sb.AppendLine();

            progress?.Report(new WorkflowProgress("Benchmark: Free Rectangle", 20));
            var frResult = RunSingle(parts, plate, settings, NestAlgorithm.FreeRectangle, cancellationToken);
            AppendBenchmarkSection(sb, "1. Free Rectangle (Guillotine)", frResult, false);

            progress?.Report(new WorkflowProgress("Benchmark: Polygon Collision", 50));
            var pcResult = RunSingle(parts, plate, settings, NestAlgorithm.PolygonCollision, cancellationToken);
            AppendBenchmarkSection(sb, "2. Polygon Collision", pcResult, true);

            progress?.Report(new WorkflowProgress("Benchmark: Irregular Experimental", 75));
            var irResult = RunSingle(parts, plate, settings, NestAlgorithm.IrregularExperimental, cancellationToken);
            AppendBenchmarkSection(sb, "3. Irregular Experimental", irResult, true);
            if (irResult.FallbackUsed)
                sb.AppendLine("   Fallback kullanıldı: Evet");
            sb.AppendLine();

            var best = new[] { frResult, pcResult, irResult }
                .OrderByDescending(r => r.Efficiency)
                .ThenBy(r => r.NestingTimeMs)
                .First();

            sb.AppendLine("=== ÖNERİLEN ===");
            sb.AppendLine($"En iyi algoritma: {best.AlgorithmName}");
            sb.AppendLine($"Verim: %{best.Efficiency:F1} | Süre: {best.NestingTimeMs} ms | Yerleşen: {best.PlacedCount}/{best.TotalParts}");

            progress?.Report(new WorkflowProgress("Benchmark raporu hazırlanıyor...", 95));

            return new BenchmarkWorkflowResult
            {
                FreeRectangle = frResult,
                PolygonCollision = pcResult,
                Irregular = irResult,
                Best = best,
                SummaryText = sb.ToString()
            };
        }, cancellationToken);
    }

    private static NestResult RunSingle(
        IReadOnlyList<PartModel> parts,
        PlateModel plate,
        NestSettings baseSettings,
        NestAlgorithm algorithm,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var engine = new NestingEngine();
        var settings = new NestSettings
        {
            Algorithm = algorithm,
            AllowRotation0 = baseSettings.AllowRotation0,
            AllowRotation90 = baseSettings.AllowRotation90,
            AllowAdvancedRotation = baseSettings.AllowAdvancedRotation,
            GapBetweenParts = baseSettings.GapBetweenParts,
            PlateMargin = baseSettings.PlateMargin,
            MaxIterations = baseSettings.MaxIterations,
            OptimizeByArea = baseSettings.OptimizeByArea
        };

        return engine.Run(parts.ToList(), plate, settings);
    }

    private static void AppendBenchmarkSection(StringBuilder sb, string title, NestResult result, bool includeCollisionDetails)
    {
        sb.AppendLine(title);
        sb.AppendLine($"   Süre: {result.NestingTimeMs} ms");
        sb.AppendLine($"   Yerleşen: {result.PlacedCount} | Sığmayan: {result.UnplacedCount}");
        sb.AppendLine($"   Verim: %{result.Efficiency:F1} | Fire: %{result.WastePercent:F1}");
        sb.AppendLine($"   Plaka: {result.PlateCount} | Deneme: {result.PlacementAttempts}");
        sb.AppendLine($"   Çakışma Kontrolü: {result.CollisionCheckCount}");
        if (includeCollisionDetails)
            sb.AppendLine($"   BBox Red: {result.BoundingBoxRejectCount} | Cache Hit: {result.CollisionCacheHits}");
        sb.AppendLine();
    }
}
