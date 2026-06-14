using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class ProjectMigrationService
{
    public const string CurrentProjectVersion = "1.0.0";

    public static NestLaserProject Migrate(NestLaserProject project, ProjectRecoveryReport? report = null)
    {
        report ??= new ProjectRecoveryReport();

        string sourceVersion = FirstNonEmpty(project.ProjectVersion, project.Version, project.AppVersion, CurrentProjectVersion);
        if (string.IsNullOrWhiteSpace(project.ProjectVersion))
        {
            project.ProjectVersion = sourceVersion;
            report.MigrationNotes.Add("ProjectVersion eksikti; mevcut sürümle dolduruldu.");
        }

        if (string.IsNullOrWhiteSpace(project.CreatedWithVersion))
        {
            project.CreatedWithVersion = sourceVersion;
            report.MigrationNotes.Add("CreatedWithVersion eksikti; proje sürümüyle dolduruldu.");
        }

        project.LastSavedWithVersion = CurrentProjectVersion;
        project.Version = CurrentProjectVersion;
        project.AppVersion = CurrentProjectVersion;

        project.Plate ??= new PlateModel();
        project.Settings ??= new NestSettings();
        project.Parts ??= new List<PartModel>();
        project.Layers ??= new List<LayerModel>();
        project.Operations ??= new List<LaserOperation>();
        project.ProfileSnapshot ??= new ProfileSnapshot();
        project.CompanyProfile ??= new CompanyProfile();
        project.PdfReportSettings ??= new PdfReportSettings();

        if (project.ProfileSnapshot.CostSettings == null)
        {
            project.ProfileSnapshot.CostSettings = new CostSettings
            {
                Currency = project.CostCurrency,
                DefaultProfitMarginPercent = project.CostProfitMarginPercent,
                VatPercent = project.CostVatPercent
            };
            report.MigrationNotes.Add("CostSettings snapshot eklendi.");
        }

        return project;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return CurrentProjectVersion;
    }
}
