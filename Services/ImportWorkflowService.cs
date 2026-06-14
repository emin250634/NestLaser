using System.Threading;
using System.Threading.Tasks;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public class ImportWorkflowResult
{
    public DxfImportResult ImportResult { get; init; } = new();
    public string VerificationText { get; init; } = string.Empty;
}

public static class ImportWorkflowService
{
    public static Task<ImportWorkflowResult> ImportDxfAsync(
        string filePath,
        bool autoDetect,
        DxfUnit sourceUnit,
        double manualScale,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("DXF birim bilgisi okunuyor...", 10));

            ImportUnitInfo? unitOverride = null;
            if (!autoDetect)
            {
                unitOverride = new ImportUnitInfo
                {
                    SourceUnit = sourceUnit,
                    ScaleFactorToMm = manualScale,
                    IsUnitDetected = true,
                    DetectionSource = "Manuel"
                };
            }

            progress?.Report(new WorkflowProgress("DXF geometrileri içe aktarılıyor...", 35));
            var result = DxfService.Import(filePath, unitOverride);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("Import doğrulaması hazırlanıyor...", 85));

            return new ImportWorkflowResult
            {
                ImportResult = result,
                VerificationText = BuildVerificationText(result)
            };
        }, cancellationToken);
    }

    public static string BuildVerificationText(DxfImportResult result)
    {
        string unit = ImportUnitInfo.GetUnitDisplayName(result.UnitInfo.SourceUnit);
        string detection = result.UnitInfo.IsUnitDetected
            ? $"Algılandı: {result.UnitInfo.DetectionSource}"
            : "Algılanamadı";

        return $"Kaynak Birim: {unit} | {detection} | Scale: {result.UnitInfo.ScaleFactorToMm:F4}";
    }
}
