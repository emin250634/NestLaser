using System;

namespace NestLaserDesktop.Models;

public class MachineProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LaserType { get; set; } = string.Empty;
    public double WorkingAreaX { get; set; }
    public double WorkingAreaY { get; set; }
    public string Notes { get; set; } = string.Empty;
}
