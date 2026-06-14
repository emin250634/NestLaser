using System;

namespace NestLaserDesktop.Models;

public class MaterialOperationSetting
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string MaterialId { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public OperationType OperationType { get; set; }
    public double Power { get; set; } = 80;
    public double Speed { get; set; } = 20;
    public int PassCount { get; set; } = 1;
    public double Frequency { get; set; } = 20000;
    public bool AirAssist { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}
