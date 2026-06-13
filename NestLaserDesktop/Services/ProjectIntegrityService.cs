using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class ProjectIntegrityService
{
    public static ProjectRecoveryReport ValidateAndRepair(NestLaserProject project, ProjectRecoveryReport? report = null)
    {
        report ??= new ProjectRecoveryReport();

        project.Parts ??= new List<PartModel>();
        project.Layers ??= new List<LayerModel>();
        project.Operations ??= new List<LaserOperation>();
        project.ProfileSnapshot ??= new ProfileSnapshot();

        EnsureLayerSet(project, report);
        EnsurePartIds(project, report);
        EnsureLayerIds(project, report);
        EnsureOperationIds(project, report);
        RepairPartLayerReferences(project, report);
        RepairOperationLayerReferences(project, report);
        RepairProfileReferences(project, report);

        return report;
    }

    private static void EnsureLayerSet(NestLaserProject project, ProjectRecoveryReport report)
    {
        if (project.Layers.Count > 0) return;

        project.Layers.Add(new LayerModel
        {
            Name = "Kesim",
            Type = LayerType.Cut,
            Color = "#4EC9B0",
            IsVisible = true
        });
        report.RecoveredSections.Add("Layers");
        report.IntegrityWarnings.Add("Layer listesi boştu; varsayılan Kesim layer oluşturuldu.");
    }

    private static void EnsurePartIds(NestLaserProject project, ProjectRecoveryReport report)
    {
        var used = new HashSet<string>();
        foreach (var part in project.Parts)
        {
            if (string.IsNullOrWhiteSpace(part.Id) || !used.Add(part.Id))
            {
                part.Id = Guid.NewGuid().ToString("N")[..8];
                used.Add(part.Id);
                report.IntegrityWarnings.Add("Eksik/tekrarlı Part ID onarıldı.");
            }
        }
    }

    private static void EnsureLayerIds(NestLaserProject project, ProjectRecoveryReport report)
    {
        var used = new HashSet<string>();
        foreach (var layer in project.Layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Id) || !used.Add(layer.Id))
            {
                layer.Id = Guid.NewGuid().ToString("N")[..8];
                used.Add(layer.Id);
                report.IntegrityWarnings.Add("Eksik/tekrarlı Layer ID onarıldı.");
            }
        }
    }

    private static void EnsureOperationIds(NestLaserProject project, ProjectRecoveryReport report)
    {
        var used = new HashSet<string>();
        foreach (var operation in project.Operations)
        {
            if (string.IsNullOrWhiteSpace(operation.Id) || !used.Add(operation.Id))
            {
                operation.Id = Guid.NewGuid().ToString("N")[..8];
                used.Add(operation.Id);
                report.IntegrityWarnings.Add("Eksik/tekrarlı Operation ID onarıldı.");
            }
        }
    }

    private static void RepairPartLayerReferences(NestLaserProject project, ProjectRecoveryReport report)
    {
        var defaultLayer = project.Layers.First();
        var layerIds = project.Layers.Select(l => l.Id).ToHashSet();

        foreach (var part in project.Parts)
        {
            if (string.IsNullOrWhiteSpace(part.LayerId) || !layerIds.Contains(part.LayerId))
            {
                part.LayerId = defaultLayer.Id;
                part.LayerName = defaultLayer.Name;
                report.IntegrityWarnings.Add($"Part layer referansı onarıldı: {part.Name}");
            }
        }
    }

    private static void RepairOperationLayerReferences(NestLaserProject project, ProjectRecoveryReport report)
    {
        var defaultLayer = project.Layers.First();
        var layerIds = project.Layers.Select(l => l.Id).ToHashSet();

        foreach (var operation in project.Operations)
        {
            if (string.IsNullOrWhiteSpace(operation.LayerId) || !layerIds.Contains(operation.LayerId))
            {
                operation.LayerId = defaultLayer.Id;
                operation.LayerName = defaultLayer.Name;
                report.IntegrityWarnings.Add($"Operation layer referansı onarıldı: {operation.Name}");
            }
        }
    }

    private static void RepairProfileReferences(NestLaserProject project, ProjectRecoveryReport report)
    {
        if (string.IsNullOrWhiteSpace(project.SelectedMaterialId) && project.ProfileSnapshot.Material != null)
        {
            project.SelectedMaterialId = project.ProfileSnapshot.Material.Id;
            report.RecoveredSections.Add("SelectedMaterialId");
        }

        if (string.IsNullOrWhiteSpace(project.SelectedMachineId) && project.ProfileSnapshot.Machine != null)
        {
            project.SelectedMachineId = project.ProfileSnapshot.Machine.Id;
            report.RecoveredSections.Add("SelectedMachineId");
        }
    }
}
