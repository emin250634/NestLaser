using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class ProfileSnapshot
{
    public MaterialProfile? Material { get; set; }
    public MachineProfile? Machine { get; set; }
    public List<MaterialOperationSetting> OperationSettings { get; set; } = new();
    public CostSettings? CostSettings { get; set; }
}
