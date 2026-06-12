using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class DxfService
{
    public static DxfImportResult Import(string filePath)
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

            var entities = DxfParser.Parse(filePath);
            string sourceFile = Path.GetFileNameWithoutExtension(filePath);
            int partIndex = 0;

            foreach (var entity in entities)
            {
                try
                {
                    if (entity.Vertices == null || entity.Vertices.Count < 2) continue;

                    var validVertices = entity.Vertices
                        .Where(v => !double.IsNaN(v.X) && !double.IsNaN(v.Y)
                                    && !double.IsInfinity(v.X) && !double.IsInfinity(v.Y))
                        .ToList();

                    if (validVertices.Count < 2) continue;

                    var polygon = new Polygon();
                    polygon.Vertices = validVertices;

                    if (polygon.Vertices.Count >= 3)
                    {
                        polygon.NormalizeWinding();
                    }
                    polygon.Calculate();

                    if (polygon.Area < 0) continue;

                    string prefix = entity.Type switch
                    {
                        DxfEntityType.LwPolyline => "LP",
                        DxfEntityType.Polyline => "P",
                        DxfEntityType.Circle => "C",
                        DxfEntityType.Arc => "A",
                        DxfEntityType.Line => "L",
                        _ => "X"
                    };

                    partIndex++;
                    result.Parts.Add(new PartModel
                    {
                        Name = $"{sourceFile}_{prefix}{partIndex}",
                        SourceFile = sourceFile,
                        LayerName = "0",
                        Geometry = polygon
                    });
                }
                catch
                {
                    partIndex++;
                }
            }

            result.TotalArea = result.Parts.Sum(p => p.Area);
            result.Success = true;

            if (result.Parts.Count == 0)
            {
                result.Warnings.Add("DXF dosyasında kapalı şekil bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"DXF okuma hatası: {ex.Message}");
        }

        return result;
    }

    public static void Export(string filePath, PlateModel plate, List<NestPlacement> placements)
    {
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("Dosya yolu boş olamaz.");
        if (plate == null) throw new ArgumentNullException(nameof(plate));
        if (placements == null) throw new ArgumentNullException(nameof(placements));

        var lines = new List<string>();

        lines.Add("0");
        lines.Add("SECTION");
        lines.Add("2");
        lines.Add("ENTITIES");

        WriteLwPolyline(lines, "Plate", new List<Geometry.Point2D>
        {
            new(0, 0),
            new(plate.Width, 0),
            new(plate.Width, plate.Height),
            new(0, plate.Height)
        }, true);

        foreach (var p in placements)
        {
            if (p?.TransformedGeometry?.Vertices == null || p.TransformedGeometry.Vertices.Count < 2) continue;
            WriteLwPolyline(lines, "Parts", p.TransformedGeometry.Vertices, true);
        }

        lines.Add("0");
        lines.Add("ENDSEC");
        lines.Add("0");
        lines.Add("EOF");

        File.WriteAllLines(filePath, lines);
    }

    private static void WriteLwPolyline(List<string> lines, string layer, List<Geometry.Point2D> vertices, bool closed)
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
            lines.Add(v.X.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            lines.Add("20");
            lines.Add(v.Y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
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
}
