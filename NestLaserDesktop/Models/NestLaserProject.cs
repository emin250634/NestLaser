using System;
using System.Collections.Generic;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Models;

public class NestLaserProject
{
    public string ProjectName { get; set; } = "Yeni Proje";
    public string Version { get; set; } = "1.0.0";
    public string ProjectVersion { get; set; } = "1.0.0";
    public string CreatedWithVersion { get; set; } = "1.0.0";
    public string LastSavedWithVersion { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string SourceDxfPath { get; set; } = string.Empty;
    public string OutputDxfPath { get; set; } = string.Empty;
    public PlateModel Plate { get; set; } = new();
    public List<PartModel> Parts { get; set; } = new();
    public List<LayerModel> Layers { get; set; } = new();
    public List<LaserOperation> Operations { get; set; } = new();
    public NestResult? NestResult { get; set; }
    public NestSettings Settings { get; set; } = new();
    public string? SelectedMaterialId { get; set; }
    public string? SelectedMachineId { get; set; }
    public ProfileSnapshot ProfileSnapshot { get; set; } = new();
    public double CostProfitMarginPercent { get; set; } = 30;
    public double CostVatPercent { get; set; } = 20;
    public string CostCurrency { get; set; } = "TRY";
    public JobCostEstimate? LastCostEstimate { get; set; }
    public CompanyProfile CompanyProfile { get; set; } = new();
    public PdfReportSettings PdfReportSettings { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public string AppVersion { get; set; } = "1.0.0";
}
