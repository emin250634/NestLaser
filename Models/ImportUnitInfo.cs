using System;

namespace NestLaserDesktop.Models;

public enum DxfUnit
{
    Unitless,
    Millimeters,
    Centimeters,
    Meters,
    Inches,
    Feet
}

public class ImportUnitInfo
{
    public DxfUnit SourceUnit { get; set; } = DxfUnit.Millimeters;
    public DxfUnit TargetUnit { get; set; } = DxfUnit.Millimeters;
    public double ScaleFactorToMm { get; set; } = 1.0;
    public bool IsUnitDetected { get; set; }
    public string DetectionSource { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;

    public static ImportUnitInfo Default => new()
    {
        SourceUnit = DxfUnit.Millimeters,
        TargetUnit = DxfUnit.Millimeters,
        ScaleFactorToMm = 1.0,
        IsUnitDetected = false,
        DetectionSource = "Varsayılan"
    };

    public static double GetScaleToMm(DxfUnit unit) => unit switch
    {
        DxfUnit.Millimeters => 1.0,
        DxfUnit.Centimeters => 10.0,
        DxfUnit.Meters => 1000.0,
        DxfUnit.Inches => 25.4,
        DxfUnit.Feet => 304.8,
        DxfUnit.Unitless => 1.0,
        _ => 1.0
    };

    public static string GetUnitDisplayName(DxfUnit unit) => unit switch
    {
        DxfUnit.Unitless => "Unitless",
        DxfUnit.Millimeters => "mm",
        DxfUnit.Centimeters => "cm",
        DxfUnit.Meters => "m",
        DxfUnit.Inches => "in",
        DxfUnit.Feet => "ft",
        _ => "mm"
    };

    public static DxfUnit FromInsUnits(int value) => value switch
    {
        0 => DxfUnit.Unitless,
        1 => DxfUnit.Inches,
        2 => DxfUnit.Feet,
        4 => DxfUnit.Millimeters,
        5 => DxfUnit.Centimeters,
        6 => DxfUnit.Meters,
        _ => DxfUnit.Unitless
    };
}
