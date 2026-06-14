using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public class CostValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool HasUnitPrice { get; init; }
}

public static class CostWorkflowService
{
    public static CostValidationResult ValidateInputs(
        IReadOnlyCollection<PartModel> parts,
        MaterialProfile? material,
        MachineProfile? machine)
    {
        if (parts.Count == 0)
            return new CostValidationResult { Message = "Maliyet hesaplama: Önce DXF yükleyin." };

        if (material == null)
            return new CostValidationResult { Message = "Maliyet hesaplama: Lütfen bir malzeme seçin." };

        if (machine == null)
            return new CostValidationResult { Message = "Maliyet hesaplama: Lütfen bir makine seçin." };

        return new CostValidationResult
        {
            IsValid = true,
            HasUnitPrice = material.UnitPrice > 0,
            Message = material.UnitPrice > 0
                ? string.Empty
                : "Uyarı: Seçili malzeme için birim fiyat tanımlanmamış. Tahmini maliyet 0 olarak hesaplanacak."
        };
    }

    public static CostSettings BuildCalculationSettings(
        CostSettings baseSettings,
        string currency,
        double profitMarginPercent,
        double vatPercent)
    {
        return new CostSettings
        {
            Currency = currency,
            DefaultProfitMarginPercent = profitMarginPercent,
            VatPercent = vatPercent,
            MachineHourlyRate = baseSettings.MachineHourlyRate,
            OperatorHourlyRate = baseSettings.OperatorHourlyRate,
            ElectricityCostPerHour = baseSettings.ElectricityCostPerHour,
            ConsumableCostPerJob = baseSettings.ConsumableCostPerJob,
            DefaultEngravingRateMm2PerMinute = baseSettings.DefaultEngravingRateMm2PerMinute,
            IncludeWasteInCost = baseSettings.IncludeWasteInCost,
            IncludeOperatorCost = baseSettings.IncludeOperatorCost,
            IncludeElectricityCost = baseSettings.IncludeElectricityCost,
            IncludeConsumables = baseSettings.IncludeConsumables,
            RoundFinalPrice = baseSettings.RoundFinalPrice
        };
    }

    public static Task<JobCostEstimate> CalculateAsync(
        string projectName,
        MaterialProfile material,
        MachineProfile machine,
        IReadOnlyList<PartModel> parts,
        IReadOnlyList<LayerModel> layers,
        IReadOnlyList<LaserOperation> operations,
        NestResult? nestResult,
        double plateWidth,
        double plateHeight,
        CostSettings settings,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("Maliyet formülleri hesaplanıyor...", 45));
            var estimate = CostEstimationService.Calculate(
                projectName,
                material,
                machine,
                parts,
                layers,
                operations,
                nestResult,
                plateWidth,
                plateHeight,
                settings);

            progress?.Report(new WorkflowProgress("Teklif özeti hazırlanıyor...", 90));
            return estimate;
        }, cancellationToken);
    }

    public static string BuildStatus(JobCostEstimate estimate)
    {
        string warnings = string.Empty;
        if (estimate.TotalEstimatedTimeMinutes <= 0)
            warnings += " | Süre hesaplanamadı (operasyon hızları kontrol edilsin)";
        if (estimate.MaterialCost <= 0)
            warnings += " | Malzeme maliyeti 0 (birim fiyat kontrol edilsin)";

        return $"Maliyet hesaplandı: Toplam {estimate.TotalProductionCost:F2} {estimate.Currency} | Önerilen: {estimate.SuggestedPrice:F2} {estimate.Currency}{warnings}";
    }

    public static string GenerateQuotation(JobCostEstimate estimate)
        => CostEstimationService.GenerateQuotationText(estimate);
}
