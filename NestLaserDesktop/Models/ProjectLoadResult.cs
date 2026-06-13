using System.Collections.Generic;

namespace NestLaserDesktop.Models;

public class ProjectLoadResult
{
    public NestLaserProject? Project { get; set; }
    public ProjectRecoveryReport RecoveryReport { get; set; } = new();
    public bool Success => Project != null;
}

public class ProjectRecoveryReport
{
    public bool UsedBackup { get; set; }
    public List<string> CorruptedSections { get; set; } = new();
    public List<string> RecoveredSections { get; set; } = new();
    public List<string> LostSections { get; set; } = new();
    public List<string> IntegrityWarnings { get; set; } = new();
    public List<string> MigrationNotes { get; set; } = new();

    public bool HasIssues =>
        UsedBackup ||
        CorruptedSections.Count > 0 ||
        RecoveredSections.Count > 0 ||
        LostSections.Count > 0 ||
        IntegrityWarnings.Count > 0 ||
        MigrationNotes.Count > 0;

    public string ToStatusText()
    {
        var parts = new List<string>();
        if (UsedBackup) parts.Add("backup kullanıldı");
        if (RecoveredSections.Count > 0) parts.Add($"kurtarılan: {string.Join(", ", RecoveredSections)}");
        if (LostSections.Count > 0) parts.Add($"kaybedilen: {string.Join(", ", LostSections)}");
        if (IntegrityWarnings.Count > 0) parts.Add($"uyarı: {IntegrityWarnings.Count}");
        if (MigrationNotes.Count > 0) parts.Add($"migration: {MigrationNotes.Count}");
        return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
    }
}
