using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public class PdfExportValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
}

public static class ExportWorkflowService
{
    public static Task<DxfExportReport> ExportDxfAsync(
        string filePath,
        PlateModel plate,
        NestResult? nestResult,
        IReadOnlyList<PartModel> parts,
        IReadOnlyCollection<PartModel> selectedParts,
        IReadOnlyCollection<LayerModel> layers,
        DxfExportOptions options,
        IReadOnlyList<LaserOperation> operations,
        string? materialName,
        string? machineName,
        JobCostEstimate? costEstimate,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("DXF export hazırlanıyor...", 25));
            var report = DxfService.Export(
                filePath,
                plate,
                nestResult,
                parts,
                selectedParts,
                layers,
                options,
                operations,
                materialName,
                machineName,
                costEstimate);

            progress?.Report(new WorkflowProgress("Export raporu oluşturuldu.", 90));
            return report;
        }, cancellationToken);
    }

    public static PdfExportValidationResult ValidatePdfInputs(
        IReadOnlyCollection<PartModel> parts,
        NestResult? nestResult,
        MaterialProfile? material,
        MachineProfile? machine,
        JobCostEstimate? estimate)
    {
        if (parts.Count == 0)
            return Invalid("PDF oluşturmak için önce bir proje/DXF açın.", "PDF: Proje yok.");

        if (nestResult == null || nestResult.Placed.Count == 0)
            return Invalid("PDF içinde yerleşim önizlemesi için önce Yerleştir işlemini çalıştırın.", "PDF: Yerleşim bulunamadı.");

        if (material == null)
            return Invalid("PDF oluşturmak için önce malzeme seçin.", "PDF: Malzeme seçilmedi.");

        if (machine == null)
            return Invalid("PDF oluşturmak için önce makine seçin.", "PDF: Makine seçilmedi.");

        if (estimate == null)
            return Invalid("PDF oluşturmak için önce maliyeti hesaplayın.", "PDF: Maliyet hesaplanmadı.");

        return new PdfExportValidationResult { IsValid = true };
    }

    public static Task CreatePdfAsync(
        string filePath,
        PdfReportType reportType,
        string projectName,
        CompanyProfile company,
        MaterialProfile material,
        MachineProfile machine,
        PlateModel plate,
        NestResult nestResult,
        JobCostEstimate estimate,
        IReadOnlyList<LaserOperation> operations,
        IProgress<WorkflowProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new WorkflowProgress("PDF raporu çiziliyor...", 45));

            if (reportType == PdfReportType.Quotation)
            {
                PdfReportService.CreateQuotationPdf(filePath, projectName, company, material, machine, plate, nestResult, estimate, operations);
            }
            else
            {
                PdfReportService.CreateProductionReportPdf(filePath, projectName, company, material, machine, plate, nestResult, estimate, operations);
            }

            progress?.Report(new WorkflowProgress("PDF dosyası yazıldı.", 95));
        }, cancellationToken);
    }

    private static PdfExportValidationResult Invalid(string message, string statusText)
        => new() { Message = message, StatusText = statusText };
}
