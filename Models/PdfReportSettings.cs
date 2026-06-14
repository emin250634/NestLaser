namespace NestLaserDesktop.Models;

public class PdfReportSettings
{
    public string LastQuotationPdfPath { get; set; } = string.Empty;
    public string LastProductionReportPdfPath { get; set; } = string.Empty;
    public string LastExportDirectory { get; set; } = string.Empty;
    public bool IncludeNestingPreview { get; set; } = true;
    public string LastReportType { get; set; } = string.Empty;

    public PdfReportSettings Clone() => new()
    {
        LastQuotationPdfPath = LastQuotationPdfPath,
        LastProductionReportPdfPath = LastProductionReportPdfPath,
        LastExportDirectory = LastExportDirectory,
        IncludeNestingPreview = IncludeNestingPreview,
        LastReportType = LastReportType
    };
}
