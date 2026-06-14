using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class CostEstimationService
{
    private static readonly string _settingsPath;

    static CostEstimationService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NestLaser");
        _settingsPath = Path.Combine(folder, "cost-settings.json");
    }

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    // --- Settings ---

    public static CostSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = new CostSettings();
            SaveSettings(defaults);
            return defaults;
        }
        try
        {
            return SafeJsonFileService.Load<CostSettings>(_settingsPath, _jsonOptions) ?? new CostSettings();
        }
        catch (Exception ex) { AppLogger.LogError(ex, "Cost settings load failed"); return new CostSettings(); }
    }

    public static void SaveSettings(CostSettings settings)
    {
        string dir = Path.GetDirectoryName(_settingsPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        SafeJsonFileService.Save(_settingsPath, settings, _jsonOptions);
    }

    // --- Cutting Length ---

    public static void CalculateCuttingLengths(
        IReadOnlyList<PartModel> parts,
        IReadOnlyList<LayerModel> layers,
        IReadOnlyList<LaserOperation> operations,
        out double totalCutMm,
        out double innerCutMm,
        out double outerCutMm,
        out double markLengthMm,
        out double engravingAreaMm2)
    {
        totalCutMm = 0;
        innerCutMm = 0;
        outerCutMm = 0;
        markLengthMm = 0;
        engravingAreaMm2 = 0;

        var layerById = layers.ToDictionary(l => l.Id, l => l);

        foreach (var part in parts)
        {
            if (!layerById.TryGetValue(part.LayerId, out var layer)) continue;
            if (!layer.IsVisible) continue;
            if (layer.Type == LayerType.Reference) continue;

            double perimeter = CalculatePerimeter(part.Geometry);
            double area = part.Geometry.Area;

            var opsForPart = operations
                .Where(o => o.Enabled && o.LayerId == part.LayerId)
                .ToList();

            foreach (var op in opsForPart)
            {
                switch (op.Type)
                {
                    case OperationType.CutOuter:
                        outerCutMm += perimeter * op.PassCount;
                        totalCutMm += perimeter * op.PassCount;
                        break;
                    case OperationType.CutInner:
                        innerCutMm += perimeter * op.PassCount;
                        totalCutMm += perimeter * op.PassCount;
                        break;
                    case OperationType.Mark:
                        markLengthMm += perimeter;
                        break;
                    case OperationType.Engrave:
                        engravingAreaMm2 += area;
                        break;
                }
            }
        }
    }

    private static double CalculatePerimeter(Polygon polygon)
    {
        if (polygon.Vertices.Count < 2) return 0;
        double p = 0;
        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            int j = (i + 1) % polygon.Vertices.Count;
            p += polygon.Vertices[i].DistanceTo(polygon.Vertices[j]);
        }
        return p;
    }

    // --- Time Estimation ---

    public static double EstimateCutTimeMinutes(double lengthMm, double speed, SpeedUnit speedUnit, int passCount)
    {
        if (speed <= 0 || passCount <= 0) return 0;
        double speedMmPerMin = speedUnit == SpeedUnit.MmPerSecond ? speed * 60 : speed;
        if (speedMmPerMin <= 0) return 0;
        return (lengthMm / speedMmPerMin) * passCount;
    }

    public static double EstimateEngraveTimeMinutes(double areaMm2, double rateMm2PerMin)
    {
        if (rateMm2PerMin <= 0) return 0;
        return areaMm2 / rateMm2PerMin;
    }

    // --- Material Cost ---

    public static double CalculateMaterialCost(
        MaterialProfile material,
        double plateWidthMm,
        double plateHeightMm,
        int plateCount)
    {
        if (material.UnitPrice <= 0) return 0;

        switch (material.UnitType)
        {
            case UnitType.PerSheet:
                return material.UnitPrice * plateCount;

            case UnitType.PerSquareMeter:
            {
                double plateAreaM2 = (plateWidthMm * plateHeightMm) / 1_000_000.0;
                double totalAreaM2 = plateAreaM2 * plateCount;
                return material.UnitPrice * totalAreaM2;
            }

            case UnitType.PerKg:
            {
                double plateAreaMm2 = plateWidthMm * plateHeightMm;
                double volumeMm3 = plateAreaMm2 * material.ThicknessMm;
                double weightKg = (volumeMm3 / 1_000_000_000.0) * material.Density;
                if (material.Density <= 0) weightKg = plateAreaMm2 * material.ThicknessMm * 0.001 / 1_000_000;
                return material.UnitPrice * weightKg * plateCount;
            }

            default:
                return 0;
        }
    }

    public static double CalculateWasteCost(double materialCost, double wastePercent)
    {
        return materialCost * (wastePercent / 100.0);
    }

    // --- Main Estimate ---

    public static JobCostEstimate Calculate(
        string projectId,
        MaterialProfile? material,
        MachineProfile? machine,
        IReadOnlyList<PartModel> parts,
        IReadOnlyList<LayerModel> layers,
        IReadOnlyList<LaserOperation> operations,
        NestResult? nestResult,
        double plateWidth,
        double plateHeight,
        CostSettings? settings = null)
    {
        settings ??= LoadSettings();
        var estimate = new JobCostEstimate
        {
            ProjectId = projectId,
            Currency = settings.Currency,
            ProfitMarginPercent = settings.DefaultProfitMarginPercent,
            VatPercent = settings.VatPercent,
            MachineHourlyRate = settings.MachineHourlyRate,
            OperatorHourlyRate = settings.OperatorHourlyRate,
            ElectricityCostPerHour = settings.ElectricityCostPerHour,
            ConsumableCost = settings.ConsumableCostPerJob
        };

        // Material info
        if (material != null)
        {
            estimate.MaterialId = material.Id;
            estimate.MaterialName = material.DisplayName;
            estimate.PlateUnitCost = material.UnitPrice;
        }
        if (machine != null)
        {
            estimate.MachineId = machine.Id;
            estimate.MachineName = machine.Name;
        }

        // Plate stats
        estimate.PlateWidth = plateWidth;
        estimate.PlateHeight = plateHeight;
        estimate.PlateAreaMm2 = plateWidth * plateHeight;
        estimate.PlateCount = nestResult?.Plates.Count ?? 1;

        if (nestResult != null && nestResult.Plates.Count > 0)
        {
            estimate.UsedAreaMm2 = nestResult.UsedArea;
            estimate.EfficiencyPercent = nestResult.Efficiency;
            estimate.WastePercent = nestResult.WastePercent;
            estimate.WasteAreaMm2 = estimate.PlateAreaMm2 * estimate.PlateCount - estimate.UsedAreaMm2;
        }
        else
        {
            estimate.UsedAreaMm2 = parts.Sum(p => p.Geometry.Area);
            estimate.EfficiencyPercent = estimate.PlateAreaMm2 > 0
                ? (estimate.UsedAreaMm2 / (estimate.PlateAreaMm2 * estimate.PlateCount)) * 100 : 0;
            estimate.WastePercent = 100 - estimate.EfficiencyPercent;
            estimate.WasteAreaMm2 = (estimate.PlateAreaMm2 * estimate.PlateCount) - estimate.UsedAreaMm2;
        }

        // Cutting lengths
        CalculateCuttingLengths(parts, layers, operations,
            out double totalCut, out double innerCut, out double outerCut,
            out double markLen, out double engraveArea);

        estimate.TotalCutLengthMm = totalCut;
        estimate.InnerCutLengthMm = innerCut;
        estimate.OuterCutLengthMm = outerCut;
        estimate.MarkLengthMm = markLen;
        estimate.EngravingAreaMm2 = engraveArea;

        // Time estimates by operation
        double totalTimeMin = 0;
        foreach (var op in operations.Where(o => o.Enabled))
        {
            CalculateOperationGeometry(op, parts, layers, out double opLength, out double opArea);

            if (opLength > 0)
            {
                double t = EstimateCutTimeMinutes(opLength, op.Speed, op.SpeedUnit, op.PassCount);
                totalTimeMin += t;
                if (op.Type == OperationType.CutOuter || op.Type == OperationType.CutInner)
                    estimate.EstimatedCutTimeMinutes += t;
                else if (op.Type == OperationType.Mark)
                    estimate.EstimatedMarkTimeMinutes += t;
            }
            if (opArea > 0)
            {
                double t = EstimateEngraveTimeMinutes(opArea, settings.DefaultEngravingRateMm2PerMinute);
                totalTimeMin += t;
                estimate.EstimatedEngraveTimeMinutes += t;
            }
        }
        estimate.TotalEstimatedTimeMinutes = totalTimeMin;

        // Cost calculations
        if (material != null)
        {
            estimate.MaterialCost = CalculateMaterialCost(material, plateWidth, plateHeight, estimate.PlateCount);
            estimate.WasteCost = settings.IncludeWasteInCost
                ? CalculateWasteCost(estimate.MaterialCost, estimate.WastePercent) : 0;
        }

        double hours = estimate.TotalEstimatedTimeMinutes / 60.0;
        estimate.MachineCost = estimate.MachineHourlyRate * hours;
        estimate.LaborCost = settings.IncludeOperatorCost ? estimate.OperatorHourlyRate * hours : 0;
        estimate.ElectricityCost = settings.IncludeElectricityCost ? estimate.ElectricityCostPerHour * hours : 0;
        if (!settings.IncludeConsumables) estimate.ConsumableCost = 0;

        estimate.TotalProductionCost = estimate.MaterialCost + estimate.WasteCost
            + estimate.MachineCost + estimate.LaborCost
            + estimate.ElectricityCost + estimate.ConsumableCost;

        // Pricing
        estimate.SuggestedPrice = estimate.TotalProductionCost * (1 + estimate.ProfitMarginPercent / 100.0);
        estimate.FinalPriceWithVat = estimate.SuggestedPrice * (1 + estimate.VatPercent / 100.0);

        if (settings.RoundFinalPrice)
        {
            estimate.SuggestedPrice = RoundPrice(estimate.SuggestedPrice);
            estimate.FinalPriceWithVat = RoundPrice(estimate.FinalPriceWithVat);
        }

        estimate.UpdatedAt = DateTime.Now;
        return estimate;
    }

    private static void CalculateOperationGeometry(
        LaserOperation operation,
        IReadOnlyList<PartModel> parts,
        IReadOnlyList<LayerModel> layers,
        out double lengthMm,
        out double areaMm2)
    {
        lengthMm = 0;
        areaMm2 = 0;
        var layerById = layers.ToDictionary(l => l.Id, l => l);

        foreach (var part in parts)
        {
            if (!layerById.TryGetValue(part.LayerId, out var layer)) continue;
            if (!layer.IsVisible || layer.Type == LayerType.Reference) continue;
            if (part.LayerId != operation.LayerId) continue;

            switch (operation.Type)
            {
                case OperationType.CutOuter:
                case OperationType.CutInner:
                case OperationType.Mark:
                    lengthMm += CalculatePerimeter(part.Geometry);
                    break;
                case OperationType.Engrave:
                    areaMm2 += part.Geometry.Area;
                    break;
            }
        }
    }

    private static double RoundPrice(double price)
    {
        if (price <= 0) return 0;
        if (price < 10) return Math.Round(price, 1);
        if (price < 100) return Math.Round(price / 5) * 5;
        if (price < 1000) return Math.Round(price / 10) * 10;
        if (price < 10000) return Math.Round(price / 50) * 50;
        return Math.Round(price / 100) * 100;
    }

    // --- Quotation Text ---

    public static string GenerateQuotationText(JobCostEstimate est)
    {
        string c = est.Currency;
        return $"NestLaser Teklif Özeti\n" +
               $"========================\n" +
               $"Malzeme: {est.MaterialName}\n" +
               $"Makine: {est.MachineName}\n" +
               $"Plaka: {est.PlateWidth:F0} x {est.PlateHeight:F0} mm\n" +
               $"Proje: {est.ProjectId}\n" +
               $"Kullanılan Plaka: {est.PlateCount}\n" +
               $"Verimlilik: %{est.EfficiencyPercent:F1}\n" +
               $"Fire: %{est.WastePercent:F1}\n" +
               $"Kesim Uzunluğu: {est.TotalCutLengthMm:F0} mm\n" +
               $"Gravür Alanı: {est.EngravingAreaMm2:F0} mm²\n" +
               $"Tahmini Kesim Süresi: {est.EstimatedCutTimeMinutes:F0} dk\n" +
               $"Toplam Tahmini Süre: {est.TotalEstimatedTimeMinutes:F0} dk\n" +
               $"------------------------\n" +
               $"Malzeme Maliyeti: {c}{est.MaterialCost:F2}\n" +
               $"Fire Maliyeti: {c}{est.WasteCost:F2}\n" +
               $"Makine Maliyeti: {c}{est.MachineCost:F2}\n" +
               $"İşçilik Maliyeti: {c}{est.LaborCost:F2}\n" +
               $"Elektrik Maliyeti: {c}{est.ElectricityCost:F2}\n" +
               $"Sarf Maliyeti: {c}{est.ConsumableCost:F2}\n" +
               $"Toplam Üretim Maliyeti: {c}{est.TotalProductionCost:F2}\n" +
               $"------------------------\n" +
               $"Kar Marjı: %{est.ProfitMarginPercent:F0}\n" +
               $"Önerilen Satış Fiyatı: {c}{est.SuggestedPrice:F2}\n" +
               $"KDV (%{est.VatPercent:F0}): {c}{est.FinalPriceWithVat:F2}\n" +
               $"========================\n" +
               $"Oluşturulma: {est.CreatedAt:yyyy-MM-dd HH:mm:ss}";
    }
}
