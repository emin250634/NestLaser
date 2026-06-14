using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class DxfService
{
    public static int InvalidRemovedCount;
    public static int DuplicatesCleanedCount;
    public static int WindingNormalizedCount;

    public static DxfImportResult Import(string filePath, ImportUnitInfo? unitInfoOverride = null)
    {
        var result = new DxfImportResult { FilePath = filePath, FileName = Path.GetFileName(filePath) };

        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add($"Dosya bulunamadı: {filePath}");
                return result;
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".dxf")
            {
                result.Errors.Add("Sadece .dxf dosyaları destekleniyor.");
                return result;
            }

            if (unitInfoOverride != null)
            {
                result.UnitInfo = unitInfoOverride;
            }
            else
            {
                result.UnitInfo = DxfHeaderParser.DetectUnit(filePath);
            }

            var entities = DxfParser.Parse(filePath);
            string sourceFile = Path.GetFileNameWithoutExtension(filePath);
            int partIndex = 0;
            int invalidRemoved = 0;
            int duplicatesCleaned = 0;
            int windingNormalized = 0;
            double scale = result.UnitInfo.ScaleFactorToMm;

            foreach (var entity in entities)
            {
                try
                {
                    if (entity.Vertices == null || entity.Vertices.Count < 2) continue;

                    var scaledVerts = entity.Vertices
                        .Where(v => !double.IsNaN(v.X) && !double.IsNaN(v.Y)
                                    && !double.IsInfinity(v.X) && !double.IsInfinity(v.Y))
                        .Select(v => new Point2D(v.X * scale, v.Y * scale))
                        .ToList();

                    if (scaledVerts.Count < 2) continue;

                    var polygon = new Polygon
                    {
                        Vertices = scaledVerts
                    };
                    polygon.Calculate();

                    polygon.NormalizeWinding();

                    int dupRemoved = polygon.CleanupVertices();
                    if (dupRemoved > 0) duplicatesCleaned += dupRemoved;

                    if (!polygon.IsValid())
                    {
                        invalidRemoved++;
                        partIndex++;
                        continue;
                    }

                    string prefix = entity.Type switch
                    {
                        DxfEntityType.LwPolyline => "LP",
                        DxfEntityType.Polyline => "P",
                        DxfEntityType.Circle => "C",
                        DxfEntityType.Arc => "A",
                        DxfEntityType.Line => "L",
                        DxfEntityType.Spline => "S",
                        DxfEntityType.Ellipse => "E",
                        _ => "X"
                    };

                    partIndex++;
                    var part = new PartModel
                    {
                        Name = $"{sourceFile}_{prefix}{partIndex}",
                        SourceFile = sourceFile,
                        LayerName = string.IsNullOrWhiteSpace(entity.LayerName) ? "Default Cut" : entity.LayerName,
                        Geometry = polygon,
                        ScaleFactor = scale
                    };
                    result.Parts.Add(part);
                }
                catch
                {
                    partIndex++;
                }
            }

            result.TotalArea = result.Parts.Sum(p => p.Area);
            result.Success = true;

            // Calculate total bounding box
            if (result.Parts.Count > 0)
            {
                double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
                foreach (var p in result.Parts)
                {
                    var b = p.Geometry.Bounds;
                    if (b.MinX < minX) minX = b.MinX;
                    if (b.MinY < minY) minY = b.MinY;
                    if (b.MaxX > maxX) maxX = b.MaxX;
                    if (b.MaxY > maxY) maxY = b.MaxY;
                }
                result.TotalBoundingWidth = maxX - minX;
                result.TotalBoundingHeight = maxY - minY;
            }

            if (!result.UnitInfo.IsUnitDetected && unitInfoOverride == null)
            {
                result.Warnings.Add(result.UnitInfo.WarningMessage);
            }
            if (scale != 1.0)
            {
                result.Warnings.Add($"Ölçü birimi dönüştürüldü: {ImportUnitInfo.GetUnitDisplayName(result.UnitInfo.SourceUnit)} → mm (x{scale:F4})");
                windingNormalized++; // mark as notable
            }

            InvalidRemovedCount += invalidRemoved;
            DuplicatesCleanedCount += duplicatesCleaned;
            WindingNormalizedCount += windingNormalized;

            if (result.Parts.Count == 0)
            {
                result.Warnings.Add("DXF dosyasında okunabilir geometri bulunamadı.");
            }
            if (invalidRemoved > 0)
            {
                result.Warnings.Add($"{invalidRemoved} geçersiz geometri atlandı.");
            }
            if (duplicatesCleaned > 0)
            {
                result.Warnings.Add($"{duplicatesCleaned} yinelenen/kolineer köşe temizlendi.");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"DXF okuma hatası: {ex.Message}");
        }

        return result;
    }

    public static DxfExportReport Export(
        string filePath,
        PlateModel plate,
        NestResult? nestResult,
        IReadOnlyList<PartModel> parts,
        IReadOnlyCollection<PartModel> selectedParts,
        IReadOnlyCollection<LayerModel> layers,
        DxfExportOptions options,
        IReadOnlyList<LaserOperation>? operations = null,
        string? materialName = null,
        string? machineName = null,
        JobCostEstimate? costEstimate = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Dosya yolu boş olamaz.");
        if (plate == null) throw new ArgumentNullException(nameof(plate));
        if (parts == null) throw new ArgumentNullException(nameof(parts));
        if (selectedParts == null) throw new ArgumentNullException(nameof(selectedParts));
        if (layers == null) throw new ArgumentNullException(nameof(layers));
        if (options == null) throw new ArgumentNullException(nameof(options));

        bool useNestResult = options.UseNestResult && nestResult != null;
        var selectedIds = selectedParts.Select(p => p.Id).ToHashSet();
        var exportedEntities = new List<ExportEntity>();
        var warnings = new List<string>();
        var layerLookup = layers.ToDictionary(l => l.Id, l => l);
        
        var platesList = useNestResult ? nestResult!.Plates : new List<PlateModel> { plate };
        var plateLayouts = BuildPlateLayouts(platesList);

        if (options.SelectedOnly && selectedParts.Count == 0)
            throw new InvalidOperationException("Seçili parça yok.");

        if (useNestResult)
        {
            // Yerleşen parçalar
            foreach (var placement in nestResult!.Placed)
            {
                if (options.SelectedOnly && !selectedIds.Contains(placement.PartId))
                    continue;

                if (!TryResolveLayer(placement.Part, layerLookup, out var layer))
                    continue;

                if (!ShouldExportLayer(layer, options))
                    continue;

                exportedEntities.Add(new ExportEntity
                {
                    Layer = layer,
                    Geometry = placement.TransformedGeometry.Clone(),
                    PlateIndex = placement.PlateIndex,
                    OffsetX = plateLayouts.ElementAtOrDefault(placement.PlateIndex)?.OffsetX ?? 0
                });
            }

            // Yerleşmeyen parçalar
            if (options.ExportUnplacedParts && nestResult.Unplaced.Count > 0)
            {
                double unplacedStartX = plateLayouts.Count > 0 
                    ? plateLayouts.Max(p => p.OffsetX + p.Width) + 50 
                    : 0;
                double currentX = unplacedStartX;
                double currentY = 0;
                double maxHeightInRow = 0;
                double rowWidth = 1000;

                foreach (var part in nestResult.Unplaced)
                {
                    if (options.SelectedOnly && !selectedIds.Contains(part.Id))
                        continue;

                    if (!TryResolveLayer(part, layerLookup, out var layer))
                        continue;

                    if (!ShouldExportLayer(layer, options))
                        continue;

                    var geometry = part.Geometry.Clone();
                    var bounds = geometry.Bounds;
                    
                    if (currentX + bounds.Width > unplacedStartX + rowWidth)
                    {
                        currentX = unplacedStartX;
                        currentY += maxHeightInRow + 20;
                        maxHeightInRow = 0;
                    }

                    geometry.Move(currentX - bounds.MinX, currentY - bounds.MinY);
                    
                    exportedEntities.Add(new ExportEntity
                    {
                        Layer = layer,
                        Geometry = geometry,
                        PlateIndex = -1,
                        OffsetX = 0
                    });

                    maxHeightInRow = Math.Max(maxHeightInRow, bounds.Height);
                    currentX += bounds.Width + 20;
                }
            }
        }
        else
        {
            // Orijinal konumlar
            foreach (var part in parts)
            {
                if (options.SelectedOnly && !selectedIds.Contains(part.Id))
                    continue;

                if (!TryResolveLayer(part, layerLookup, out var layer))
                    continue;

                if (!ShouldExportLayer(layer, options))
                    continue;

                exportedEntities.Add(new ExportEntity
                {
                    Layer = layer,
                    Geometry = part.Geometry.Clone(),
                    PlateIndex = 0
                });
            }
        }

        if (exportedEntities.Count == 0)
            throw new InvalidOperationException("Export edilecek parça bulunamadı.");

        var usedLayers = exportedEntities.Select(e => e.Layer).DistinctBy(l => l.Name).ToList();
        var layerNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var l in usedLayers) layerNames.Add(l.Name);

        if (options.ExportPlateBorders)
        {
            foreach (var layout in plateLayouts)
            {
                string borderLayerName = $"Plate_{layout.Index + 1}_Border";
                layerNames.Add(borderLayerName);
                exportedEntities.Add(new ExportEntity
                {
                    Layer = new LayerModel
                    {
                        Name = borderLayerName,
                        Color = "#808080",
                        Type = LayerType.Reference,
                        IsVisible = true
                    },
                    Geometry = CreateRectangleGeometry(layout.Width, layout.Height),
                    OffsetX = layout.OffsetX
                });
            }
        }

        var reportDir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(reportDir))
            reportDir = Directory.GetCurrentDirectory();
        if (!Directory.Exists(reportDir))
            Directory.CreateDirectory(reportDir);
        var reportPath = Path.Combine(reportDir, "export-report.txt");
        var layerSummaries = BuildLayerSummaries(usedLayers);
        
        var partEntities = exportedEntities.Where(e => !e.Layer.Name.StartsWith("Plate_", StringComparison.OrdinalIgnoreCase)).ToList();
        var exportArea = partEntities.Sum(e => e.Geometry.Area);
        var totalPlateArea = plateLayouts.Sum(p => p.Width * p.Height);
        var efficiency = totalPlateArea > 0 ? (exportArea / totalPlateArea) * 100.0 : 0.0;
        var wastePercent = totalPlateArea > 0 ? ((totalPlateArea - exportArea) / totalPlateArea) * 100.0 : 0.0;

        var operationOrder = new List<string>();
        if (options.IncludeOperationOrder && operations != null && operations.Count > 0)
        {
            int opIdx = 1;
            foreach (var op in operations.Where(o => o.Enabled).OrderBy(o => o.Priority))
            {
                string opName = GetOperationDisplayName(op.Type);
                operationOrder.Add($"{opIdx}. {op.Name} | Tip: {opName} | Güç: {op.Power:F0} | Hız: {op.Speed:F0} | Pas: {op.PassCount}");
                opIdx++;
            }
        }

        var report = new DxfExportReport
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            ExportTime = DateTime.Now,
            PartCount = partEntities.Count,
            PlateCount = plateLayouts.Count,
            Efficiency = efficiency,
            WastePercent = wastePercent,
            LayerSummaries = layerSummaries
                .Select(l => $"{l.Name} | Tip: {l.Type} | Renk: {l.Color} | Power: {l.Power:F0} | Speed: {l.Speed:F0} | Pass: {l.PassCount}")
                .ToList(),
            OperationOrder = operationOrder,
            TotalOperationCount = operations?.Count ?? 0,
            ActiveOperationCount = operations?.Count(o => o.Enabled) ?? 0,
            MaterialName = materialName,
            MachineName = machineName,
            TotalCutLengthMm = costEstimate?.TotalCutLengthMm,
            EngravingAreaMm2 = costEstimate?.EngravingAreaMm2,
            EstimatedTimeMinutes = costEstimate?.TotalEstimatedTimeMinutes,
            MaterialCost = costEstimate?.MaterialCost,
            WasteCost = costEstimate?.WasteCost,
            MachineCost = costEstimate?.MachineCost,
            LaborCost = costEstimate?.LaborCost,
            TotalProductionCost = costEstimate?.TotalProductionCost,
            SuggestedPrice = costEstimate?.SuggestedPrice,
            FinalPriceWithVat = costEstimate?.FinalPriceWithVat,
            CostCurrency = costEstimate?.Currency,
            Warnings = warnings,
            ReportPath = reportPath
        };

        WriteDxf(filePath, exportedEntities, layerNames, usedLayers);
        WriteExportReport(reportPath, report, layerSummaries, exportedEntities, useNestResult, options, nestResult, plateLayouts, operations);
        
        return report;
    }

    private static bool TryResolveLayer(PartModel part, IReadOnlyDictionary<string, LayerModel> layerLookup, out LayerModel layer)
    {
        if (!string.IsNullOrWhiteSpace(part.LayerId) &&
            layerLookup.TryGetValue(part.LayerId, out var resolvedLayer) &&
            resolvedLayer != null)
        {
            layer = resolvedLayer;
            return true;
        }

        layer = layerLookup.Values.FirstOrDefault(l => string.Equals(l.Name, part.LayerName, StringComparison.CurrentCultureIgnoreCase))
            ?? new LayerModel
            {
                Name = string.IsNullOrWhiteSpace(part.LayerName) ? "Cut" : part.LayerName,
                Type = LayerType.Cut,
                Color = "#4EC9B0",
                IsVisible = true,
                IsLocked = false
            };
        return true;
    }

    private static bool ShouldExportLayer(LayerModel layer, DxfExportOptions options)
    {
        if (!options.ExportHiddenLayers && !layer.IsVisible) return false;
        if (!options.ExportReferenceLayer && layer.Type == LayerType.Reference) return false;
        return true;
    }

    private static List<LayerSummary> BuildLayerSummaries(IEnumerable<LayerModel> layers)
    {
        return layers
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Name)
            .Select(l => new LayerSummary
            {
                Name = l.Name,
                Color = l.Color,
                Type = l.Type.ToString(),
                Power = l.Power,
                Speed = l.Speed,
                PassCount = l.PassCount
            })
            .ToList();
    }

    private static List<PlateLayout> BuildPlateLayouts(IReadOnlyList<PlateModel> plates)
    {
        var layouts = new List<PlateLayout>();
        double offsetX = 0;
        double gap = 20;

        for (int i = 0; i < plates.Count; i++)
        {
            var plate = plates[i];
            layouts.Add(new PlateLayout(i, plate.Width, plate.Height, offsetX));
            offsetX += plate.Width + gap;
        }

        return layouts;
    }

    private static void WriteDxf(string filePath, IReadOnlyList<ExportEntity> entities, IReadOnlyCollection<string> layerNames, List<LayerModel> usedLayers)
    {
        var lines = new List<string>
        {
            "0", "SECTION", "2", "HEADER", "9", "$ACADVER", "1", "AC1015", "0", "ENDSEC",
            "0", "SECTION", "2", "TABLES"
        };

        WriteLayerTable(lines, layerNames, usedLayers);

        lines.Add("0");
        lines.Add("ENDSEC");
        lines.Add("0");
        lines.Add("SECTION");
        lines.Add("2");
        lines.Add("ENTITIES");

        foreach (var entity in entities)
        {
            if (entity.Geometry?.Vertices == null || entity.Geometry.Vertices.Count < 2) continue;
            WriteLwPolyline(lines, entity.Layer.Name, entity.Geometry.Vertices, true, entity.OffsetX);
        }

        lines.Add("0");
        lines.Add("ENDSEC");
        lines.Add("0");
        lines.Add("EOF");

        File.WriteAllLines(filePath, lines);
    }

    private static void WriteLayerTable(List<string> lines, IReadOnlyCollection<string> layerNames, List<LayerModel> usedLayers)
    {
        lines.Add("0");
        lines.Add("TABLE");
        lines.Add("2");
        lines.Add("LAYER");
        lines.Add("70");
        lines.Add(layerNames.Count.ToString());

        var layerLookup = usedLayers.ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        foreach (var layerName in layerNames.OrderBy(n => n))
        {
            lines.Add("0");
            lines.Add("LAYER");
            lines.Add("2");
            lines.Add(layerName);
            lines.Add("70");
            lines.Add("0");
            lines.Add("62");
            
            int colorIndex = 7;
            if (layerLookup.TryGetValue(layerName, out var layer))
            {
                colorIndex = GetAciFromHex(layer.Color);
            }
            else
            {
                colorIndex = GetDxfColorIndex(layerName);
            }
            
            lines.Add(colorIndex.ToString());
            lines.Add("6");
            lines.Add("CONTINUOUS");
        }

        lines.Add("0");
        lines.Add("ENDTAB");
    }

    private static int GetAciFromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return 7;
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return 7;

        try
        {
            int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            if (r > 200 && g < 50 && b < 50) return 1; // Red
            if (r > 200 && g > 200 && b < 50) return 2; // Yellow
            if (r < 50 && g > 200 && b < 50) return 3; // Green
            if (r < 50 && g > 200 && b > 200) return 4; // Cyan
            if (r < 50 && g < 50 && b > 200) return 5; // Blue
            if (r > 200 && g < 50 && b > 200) return 6; // Magenta
            if (r < 50 && g < 50 && b < 50) return 250; // Black
            if (r > 200 && g > 200 && b > 200) return 7; // White
            
            return hex.ToLowerInvariant() switch
            {
                "4ec9b0" => 3,
                "d7ba7d" => 2,
                "9cdcfe" => 4,
                "808080" => 8,
                _ => 7
            };
        }
        catch { return 7; }
    }

    private static int GetDxfColorIndex(string layerName)
    {
        if (layerName.StartsWith("Plate_", StringComparison.OrdinalIgnoreCase))
            return 8;

        return layerName.ToLowerInvariant() switch
        {
            "kesim" => 3,
            "cut" => 3,
            "gravür" => 2,
            "engrave" => 2,
            "markalama" => 5,
            "mark" => 5,
            "referans" => 8,
            "reference" => 8,
            _ => 7
        };
    }

    private static void WriteLwPolyline(List<string> lines, string layer, List<Point2D> vertices, bool closed, double offsetX = 0)
    {
        lines.Add("0");
        lines.Add("LWPOLYLINE");
        lines.Add("8");
        lines.Add(layer);
        lines.Add("90");
        lines.Add(vertices.Count.ToString());
        lines.Add("70");
        lines.Add(closed ? "1" : "0");

        foreach (var v in vertices)
        {
            lines.Add("10");
            lines.Add((v.X + offsetX).ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            lines.Add("20");
            lines.Add(v.Y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private static Polygon CreateRectangleGeometry(double width, double height)
    {
        var polygon = new Polygon
        {
            Vertices = new List<Point2D>
            {
                new(0, 0),
                new(width, 0),
                new(width, height),
                new(0, height)
            }
        };
        polygon.Calculate();
        return polygon;
    }

    private static string GetOperationDisplayName(OperationType type) => type switch
    {
        OperationType.Engrave => "Gravür",
        OperationType.Mark => "Markalama",
        OperationType.CutInner => "İç Kesim",
        OperationType.CutOuter => "Dış Kesim",
        OperationType.Reference => "Referans",
        _ => type.ToString()
    };

    private static void WriteExportReport(
        string reportPath,
        DxfExportReport report,
        IReadOnlyList<LayerSummary> layerSummaries,
        IReadOnlyList<ExportEntity> entities,
        bool useNestResult,
        DxfExportOptions options,
        NestResult? nestResult,
        IReadOnlyList<PlateLayout> plateLayouts,
        IReadOnlyList<LaserOperation>? operations = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine("              NESTLASER DESKTOP EXPORT REPORT               ");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine($"Export Zamanı      : {report.ExportTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Kaynak DXF         : {report.FileName}");
        sb.AppendLine($"Çıktı DXF          : {Path.GetFileName(report.FilePath)}");
        sb.AppendLine($"Export Modu        : {(useNestResult ? "Yerleşim Sonrası (Production)" : "Orijinal Konumlar")}");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine($"Toplam Parça       : {report.PartCount}");
        sb.AppendLine($"Plaka Sayısı       : {report.PlateCount}");
        sb.AppendLine($"Yerleşen Parça     : {nestResult?.Placed.Count ?? report.PartCount}");
        sb.AppendLine($"Sığmayan Parça     : {nestResult?.Unplaced.Count ?? 0}");
        sb.AppendLine($"Verimlilik         : %{report.Efficiency:F2}");
        sb.AppendLine($"Fire               : %{report.WastePercent:F2}");
        sb.AppendLine($"Algoritma          : {nestResult?.AlgorithmUsed ?? "N/A"}");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine("SEÇENEKLER:");
        sb.AppendLine($"- Seçili Parça Export     : {(options.SelectedOnly ? "Evet" : "Hayır")}");
        sb.AppendLine($"- Gizli Katman Export     : {(options.ExportHiddenLayers ? "Evet" : "Hayır")}");
        sb.AppendLine($"- Referans Katman Export  : {(options.ExportReferenceLayer ? "Evet" : "Hayır")}");
        sb.AppendLine($"- Plaka Sınırı Export     : {(options.ExportPlateBorders ? "Evet" : "Hayır")}");
        sb.AppendLine($"- Sığmayan Parça Export   : {(options.ExportUnplacedParts ? "Evet" : "Hayır")}");
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine("KATMAN LİSTESİ:");

        foreach (var layer in layerSummaries)
        {
            int partCountInLayer = entities.Count(e => e.Layer.Name == layer.Name);
            sb.AppendLine($"- {layer.Name,-15} | Tip: {layer.Type,-10} | Power: {layer.Power,3:F0} | Speed: {layer.Speed,4:F0} | Pass: {layer.PassCount,2} | Parça: {partCountInLayer}");
        }

        if (nestResult != null && nestResult.Warnings.Count > 0)
        {
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine("UYARILAR:");
            foreach (var warning in nestResult.Warnings)
                sb.AppendLine($"! {warning}");
        }

        if (plateLayouts.Count > 0 && options.ExportPlateBorders)
        {
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine("PLAKA DETAYLARI:");
            foreach (var plate in plateLayouts)
                sb.AppendLine($"- Plate_{plate.Index + 1}_Border : {plate.Width:F0} x {plate.Height:F0} mm");
        }

        if (!string.IsNullOrWhiteSpace(report.MaterialName) || !string.IsNullOrWhiteSpace(report.MachineName))
        {
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine("MALZEME & MAKİNE:");
            if (!string.IsNullOrWhiteSpace(report.MaterialName))
                sb.AppendLine($"  Malzeme: {report.MaterialName}");
            if (!string.IsNullOrWhiteSpace(report.MachineName))
                sb.AppendLine($"  Makine: {report.MachineName}");
        }

        if (report.TotalProductionCost.HasValue)
        {
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine("MALİYET & TEKLİF:");
            sb.AppendLine($"  Toplam Kesim Uzunluğu   : {report.TotalCutLengthMm:F0} mm");
            sb.AppendLine($"  Kazıma Alanı            : {report.EngravingAreaMm2:F0} mm²");
            sb.AppendLine($"  Tahmini Süre            : {report.EstimatedTimeMinutes:F1} dk");
            sb.AppendLine($"  Malzeme Maliyeti        : {report.CostCurrency}{report.MaterialCost:F2}");
            sb.AppendLine($"  Fire Maliyeti           : {report.CostCurrency}{report.WasteCost:F2}");
            sb.AppendLine($"  Makine Maliyeti         : {report.CostCurrency}{report.MachineCost:F2}");
            sb.AppendLine($"  İşçilik Maliyeti        : {report.CostCurrency}{report.LaborCost:F2}");
            sb.AppendLine($"  Toplam Üretim Maliyeti  : {report.CostCurrency}{report.TotalProductionCost:F2}");
            sb.AppendLine($"  Önerilen Fiyat          : {report.CostCurrency}{report.SuggestedPrice:F2}");
            sb.AppendLine($"  KDV Dahil Fiyat         : {report.CostCurrency}{report.FinalPriceWithVat:F2}");
        }

        if (operations != null && operations.Count > 0 && options.IncludeOperationOrder)
        {
            int totalOps = operations.Count;
            int activeOps = operations.Count(o => o.Enabled);
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine($"OPERASYON SIRASI (ÜRETİM AKIŞI)  | Toplam: {totalOps} | Aktif: {activeOps}");
            foreach (var opLine in report.OperationOrder)
                sb.AppendLine($"  {opLine}");
        }

        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine("Generated by NestLaser Desktop");

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            try { File.WriteAllText(reportPath, sb.ToString()); }
            catch { /* Rapor yazılamazsa export devam etsin */ }
        }
    }

    private sealed class ExportEntity
    {
        public LayerModel Layer { get; set; } = new();
        public Polygon Geometry { get; set; } = new();
        public double OffsetX { get; set; }
        public int PlateIndex { get; set; }
    }

    private sealed class PlateLayout
    {
        public PlateLayout(int index, double width, double height, double offsetX)
        {
            Index = index;
            Width = width;
            Height = height;
            OffsetX = offsetX;
        }

        public int Index { get; }
        public double Width { get; }
        public double Height { get; }
        public double OffsetX { get; }
    }

    private sealed class LayerSummary
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Power { get; set; }
        public double Speed { get; set; }
        public int PassCount { get; set; }
    }
}

public class DxfImportResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public List<PartModel> Parts { get; set; } = new();
    public double TotalArea { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ImportUnitInfo UnitInfo { get; set; } = ImportUnitInfo.Default;
    public double TotalBoundingWidth { get; set; }
    public double TotalBoundingHeight { get; set; }
}
