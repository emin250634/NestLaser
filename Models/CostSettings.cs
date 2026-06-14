using System;

namespace NestLaserDesktop.Models;

public class CostSettings
{
    public string Currency { get; set; } = "TRY";
    public double DefaultProfitMarginPercent { get; set; } = 30;
    public double MachineHourlyRate { get; set; } = 150;
    public double OperatorHourlyRate { get; set; } = 80;
    public double ElectricityCostPerHour { get; set; } = 15;
    public double ConsumableCostPerJob { get; set; } = 25;
    public double DefaultEngravingRateMm2PerMinute { get; set; } = 500;
    public bool IncludeWasteInCost { get; set; } = true;
    public bool IncludeOperatorCost { get; set; } = true;
    public bool IncludeElectricityCost { get; set; } = true;
    public bool IncludeConsumables { get; set; } = true;
    public bool RoundFinalPrice { get; set; } = true;
    public double VatPercent { get; set; } = 20;
}
