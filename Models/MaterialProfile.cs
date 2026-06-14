using System;

namespace NestLaserDesktop.Models;

public class MaterialProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double ThicknessMm { get; set; }
    public double Density { get; set; }
    public double UnitPrice { get; set; }
    public UnitType UnitType { get; set; } = UnitType.PerSheet;
    public string Notes { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public string DisplayName => ThicknessMm > 0 ? $"{Name} - {ThicknessMm:F1}mm" : Name;
    public string UnitPriceText => UnitPrice > 0 ? $"{UnitPrice:F2} {(UnitType == UnitType.PerSheet ? "/tabaka" : UnitType == UnitType.PerSquareMeter ? "/m²" : "/kg")}" : "--";
}
