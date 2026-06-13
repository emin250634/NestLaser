using System;
using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class DxfExportReport
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ReportPath { get; set; } = string.Empty;
    public DateTime ExportTime { get; set; }
    public int PartCount { get; set; }
    public int PlateCount { get; set; }
    public double Efficiency { get; set; }
    public double WastePercent { get; set; }
    public List<string> LayerSummaries { get; set; } = new();
    public List<string> OperationOrder { get; set; } = new();
    public int TotalOperationCount { get; set; }
    public int ActiveOperationCount { get; set; }
    public string? MaterialName { get; set; }
    public string? MachineName { get; set; }
    // Cost Estimation
    public double? TotalCutLengthMm { get; set; }
    public double? EngravingAreaMm2 { get; set; }
    public double? EstimatedTimeMinutes { get; set; }
    public double? MaterialCost { get; set; }
    public double? WasteCost { get; set; }
    public double? MachineCost { get; set; }
    public double? LaborCost { get; set; }
    public double? TotalProductionCost { get; set; }
    public double? SuggestedPrice { get; set; }
    public double? FinalPriceWithVat { get; set; }
    public string? CostCurrency { get; set; }
    public List<string> Warnings { get; set; } = new();
}
