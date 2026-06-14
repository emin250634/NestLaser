using System;

namespace NestLaserDesktop.Models;

public class JobCostEstimate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string ProjectId { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public double PlateWidth { get; set; }
    public double PlateHeight { get; set; }
    public double PlateAreaMm2 { get; set; }
    public double UsedAreaMm2 { get; set; }
    public double WasteAreaMm2 { get; set; }
    public double EfficiencyPercent { get; set; }
    public double WastePercent { get; set; }
    public double PlateUnitCost { get; set; }
    public int PlateCount { get; set; }
    public double MaterialCost { get; set; }
    public double WasteCost { get; set; }
    public double TotalCutLengthMm { get; set; }
    public double InnerCutLengthMm { get; set; }
    public double OuterCutLengthMm { get; set; }
    public double MarkLengthMm { get; set; }
    public double EngravingAreaMm2 { get; set; }
    public double EstimatedCutTimeMinutes { get; set; }
    public double EstimatedEngraveTimeMinutes { get; set; }
    public double EstimatedMarkTimeMinutes { get; set; }
    public double TotalEstimatedTimeMinutes { get; set; }
    public double MachineHourlyRate { get; set; }
    public double OperatorHourlyRate { get; set; }
    public double ElectricityCostPerHour { get; set; }
    public double ConsumableCost { get; set; }
    public double LaborCost { get; set; }
    public double MachineCost { get; set; }
    public double ElectricityCost { get; set; }
    public double TotalProductionCost { get; set; }
    public double ProfitMarginPercent { get; set; }
    public double SuggestedPrice { get; set; }
    public double VatPercent { get; set; }
    public double FinalPriceWithVat { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
