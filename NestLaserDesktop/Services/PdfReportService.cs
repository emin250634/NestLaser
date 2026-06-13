using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public enum PdfReportType
{
    Quotation,
    Production
}

public static class PdfReportService
{
    private const double PageWidth = 595.0;
    private const double PageHeight = 842.0;
    private const double Margin = 42.0;

    public static void CreateQuotationPdf(
        string filePath,
        string projectName,
        CompanyProfile company,
        MaterialProfile material,
        MachineProfile machine,
        PlateModel plate,
        NestResult nestResult,
        JobCostEstimate estimate,
        IReadOnlyList<LaserOperation> operations)
    {
        CreateReport(filePath, PdfReportType.Quotation, projectName, company, material, machine, plate, nestResult, estimate, operations);
    }

    public static void CreateProductionReportPdf(
        string filePath,
        string projectName,
        CompanyProfile company,
        MaterialProfile material,
        MachineProfile machine,
        PlateModel plate,
        NestResult nestResult,
        JobCostEstimate estimate,
        IReadOnlyList<LaserOperation> operations)
    {
        CreateReport(filePath, PdfReportType.Production, projectName, company, material, machine, plate, nestResult, estimate, operations);
    }

    public static void CreatePreviewPdf(
        string filePath,
        string projectName,
        CompanyProfile company,
        PlateModel plate,
        NestResult nestResult)
    {
        var content = new PdfCanvas();
        var logo = LoadLogo(company.LogoPath);
        DrawHeader(content, "NestLaser Nesting Preview", projectName, company, logo != null);
        DrawPreview(content, plate, nestResult, Margin, 190, PageWidth - Margin * 2, 480);
        DrawFooter(content);
        WritePdf(filePath, new[] { content.ToString() }, logo);
    }

    private static void CreateReport(
        string filePath,
        PdfReportType reportType,
        string projectName,
        CompanyProfile company,
        MaterialProfile material,
        MachineProfile machine,
        PlateModel plate,
        NestResult nestResult,
        JobCostEstimate estimate,
        IReadOnlyList<LaserOperation> operations)
    {
        var content = new PdfCanvas();
        var logo = LoadLogo(company.LogoPath);
        string title = reportType == PdfReportType.Quotation ? "NestLaser Quotation" : "NestLaser Production Report";

        DrawHeader(content, title, projectName, company, logo != null);
        DrawQuotationFields(content, projectName, material, machine, nestResult);
        DrawPreview(content, plate, nestResult, Margin, 455, PageWidth - Margin * 2, 205);

        double left = Margin;
        double right = PageWidth / 2.0 + 10;
        double y = 425;

        DrawSection(content, "Material Summary", left, y);
        DrawRows(content, left, y - 20, 230, new[]
        {
            ("Malzeme", material.DisplayName),
            ("Kalinlik", $"{material.ThicknessMm:F1} mm"),
            ("Plaka Olcusu", $"{plate.Width:F0} x {plate.Height:F0} mm"),
            ("Kullanilan Plaka", $"{estimate.PlateCount}"),
            ("Verimlilik", $"{estimate.EfficiencyPercent:F1}%"),
            ("Fire", $"{estimate.WastePercent:F1}%")
        });

        DrawSection(content, "Production Summary", right, y);
        DrawRows(content, right, y - 20, 220, new[]
        {
            ("Kesim Uzunlugu", $"{estimate.TotalCutLengthMm:F0} mm"),
            ("Gravur Alani", $"{estimate.EngravingAreaMm2:F0} mm2"),
            ("Tahmini Sure", $"{estimate.TotalEstimatedTimeMinutes:F1} dk"),
            ("Operasyon Sayisi", $"{operations.Count(o => o.Enabled)} / {operations.Count}")
        });

        y = 262;
        DrawSection(content, "Cost Summary", left, y);
        DrawRows(content, left, y - 20, 230, new[]
        {
            ("Malzeme Maliyeti", Money(estimate.MaterialCost, estimate.Currency)),
            ("Fire Maliyeti", Money(estimate.WasteCost, estimate.Currency)),
            ("Makine Maliyeti", Money(estimate.MachineCost, estimate.Currency)),
            ("Iscilik", Money(estimate.LaborCost, estimate.Currency)),
            ("Elektrik", Money(estimate.ElectricityCost, estimate.Currency)),
            ("Sarf", Money(estimate.ConsumableCost, estimate.Currency)),
            ("Uretim Maliyeti", Money(estimate.TotalProductionCost, estimate.Currency))
        });

        DrawSection(content, "Sales Summary", right, y);
        DrawRows(content, right, y - 20, 220, new[]
        {
            ("Kar Marji", $"{estimate.ProfitMarginPercent:F1}%"),
            ("Satis Fiyati", Money(estimate.SuggestedPrice, estimate.Currency)),
            ("KDV", $"{estimate.VatPercent:F1}%"),
            ("KDV Dahil Fiyat", Money(estimate.FinalPriceWithVat, estimate.Currency))
        });

        DrawFooter(content);
        WritePdf(filePath, new[] { content.ToString() }, logo);
    }

    private static void DrawHeader(PdfCanvas c, string title, string projectName, CompanyProfile company, bool hasLogo)
    {
        c.FillRgb(0.08, 0.10, 0.12);
        c.Rect(0, PageHeight - 92, PageWidth, 92, fill: true, stroke: false);
        if (hasLogo)
            c.Image("Logo", PageWidth - Margin - 52, PageHeight - 76, 52, 52);
        c.Text(title, Margin, PageHeight - 46, 20, bold: true, r: 1, g: 1, b: 1);
        c.Text(company.CompanyName, Margin, PageHeight - 68, 10, r: 0.75, g: 0.82, b: 0.86);

        var lines = new[] { company.Address, company.Phone, company.Email, company.Website }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Take(4)
            .ToList();

        double y = PageHeight - 34;
        double rightX = hasLogo ? PageWidth - Margin - 250 : PageWidth - Margin - 190;
        foreach (string line in lines)
        {
            c.Text(line, rightX, y, 8, r: 0.75, g: 0.82, b: 0.86);
            y -= 11;
        }

        c.Text($"Project: {projectName}", Margin, PageHeight - 116, 11, bold: true, r: 0.08, g: 0.10, b: 0.12);
    }

    private static void DrawQuotationFields(PdfCanvas c, string projectName, MaterialProfile material, MachineProfile machine, NestResult nestResult)
    {
        DrawRows(c, Margin, PageHeight - 142, PageWidth - Margin * 2, new[]
        {
            ("Firma Adi", ""),
            ("Tarih", DateTime.Now.ToString("yyyy-MM-dd HH:mm")),
            ("Proje Adi", projectName),
            ("Malzeme", material.DisplayName),
            ("Makine", machine.Name),
            ("Parca Sayisi", $"{nestResult.PlacedCount} / {nestResult.TotalParts}")
        }, columns: 3);
    }

    private static void DrawPreview(PdfCanvas c, PlateModel plate, NestResult nestResult, double x, double y, double width, double height)
    {
        DrawSection(c, "Nesting Preview", x, y + height + 18);
        c.StrokeRgb(0.78, 0.82, 0.86);
        c.FillRgb(0.97, 0.98, 0.98);
        c.Rect(x, y, width, height, fill: true, stroke: true);

        if (nestResult.Placed.Count == 0 || plate.Width <= 0 || plate.Height <= 0)
        {
            c.Text("Yerlesim onizlemesi yok.", x + 16, y + height / 2, 11, r: 0.4, g: 0.45, b: 0.5);
            return;
        }

        var plateIndexes = nestResult.Placed.Select(p => p.PlateIndex).Distinct().OrderBy(i => i).ToList();
        int plateCount = Math.Max(1, plateIndexes.Count);
        double gap = 14;
        double totalPlateWidth = plate.Width * plateCount + plate.Width * 0.06 * (plateCount - 1);
        double scale = Math.Min((width - 24) / totalPlateWidth, (height - 34) / plate.Height);
        scale = Math.Max(0.01, scale);
        double renderedWidth = totalPlateWidth * scale;
        double startX = x + (width - renderedWidth) / 2.0;
        double startY = y + 20;

        for (int i = 0; i < plateIndexes.Count; i++)
        {
            int plateIndex = plateIndexes[i];
            double px = startX + i * (plate.Width * scale + gap);
            double py = startY;
            double pw = plate.Width * scale;
            double ph = plate.Height * scale;

            c.FillRgb(1, 1, 1);
            c.StrokeRgb(0.18, 0.22, 0.26);
            c.Rect(px, py, pw, ph, fill: true, stroke: true);

            foreach (var placement in nestResult.Placed.Where(p => p.PlateIndex == plateIndex))
            {
                DrawPlacement(c, placement, px, py, ph, scale);
            }

            c.Text($"Plate {plateIndex + 1}", px, py - 10, 7, r: 0.35, g: 0.39, b: 0.43);
        }

        c.Text($"Efficiency: {nestResult.Efficiency:F1}%   Waste: {nestResult.WastePercent:F1}%   Placed: {nestResult.PlacedCount}/{nestResult.TotalParts}", x + 10, y + 8, 8, r: 0.2, g: 0.25, b: 0.30);
    }

    private static void DrawPlacement(PdfCanvas c, NestPlacement placement, double plateX, double plateY, double plateHeight, double scale)
    {
        var vertices = placement.TransformedGeometry?.Vertices;
        if (vertices == null || vertices.Count < 3) return;

        c.FillRgb(0.30, 0.73, 0.64);
        c.StrokeRgb(0.11, 0.44, 0.38);
        var mapped = vertices.Select(v => (X: plateX + v.X * scale, Y: plateY + plateHeight - v.Y * scale)).ToList();
        c.Polygon(mapped, fill: true, stroke: true);
    }

    private static void DrawSection(PdfCanvas c, string title, double x, double y)
    {
        c.Text(title, x, y, 12, bold: true, r: 0.08, g: 0.10, b: 0.12);
        c.StrokeRgb(0.30, 0.73, 0.64);
        c.Line(x, y - 5, x + 190, y - 5);
    }

    private static void DrawRows(PdfCanvas c, double x, double y, double width, IEnumerable<(string Label, string Value)> rows, int columns = 1)
    {
        var list = rows.ToList();
        int rowsPerColumn = (int)Math.Ceiling(list.Count / (double)columns);
        double columnWidth = width / columns;

        for (int i = 0; i < list.Count; i++)
        {
            int column = i / rowsPerColumn;
            int row = i % rowsPerColumn;
            double rx = x + column * columnWidth;
            double ry = y - row * 16;
            c.Text(list[i].Label, rx, ry, 8, r: 0.45, g: 0.49, b: 0.54);
            c.Text(list[i].Value, rx + 88, ry, 8, bold: true, r: 0.08, g: 0.10, b: 0.12);
        }
    }

    private static void DrawFooter(PdfCanvas c)
    {
        c.StrokeRgb(0.86, 0.88, 0.90);
        c.Line(Margin, 34, PageWidth - Margin, 34);
        c.Text("Generated by NestLaser Desktop", Margin, 22, 8, r: 0.45, g: 0.49, b: 0.54);
        c.Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), PageWidth - Margin - 92, 22, 8, r: 0.45, g: 0.49, b: 0.54);
    }

    private static string Money(double value, string currency) => $"{currency} {value:F2}";

    private static LogoImage? LoadLogo(string logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath) || !File.Exists(logoPath))
            return null;

        try
        {
            var frame = BitmapFrame.Create(new Uri(logoPath, UriKind.Absolute), BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
            encoder.Frames.Add(frame);
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return new LogoImage(frame.PixelWidth, frame.PixelHeight, ms.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static void WritePdf(string path, IReadOnlyList<string> pageStreams, LogoImage? logo)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var objects = new List<PdfObject>();
        int catalogId = 1;
        int pagesId = 2;
        int fontId = 3;
        int imageId = logo != null ? 4 : 0;
        int firstPageId = logo != null ? 5 : 4;
        int firstContentId = firstPageId + pageStreams.Count;

        var pageKids = new List<int>();
        for (int i = 0; i < pageStreams.Count; i++)
            pageKids.Add(firstPageId + i);

        objects.Add(PdfObject.Text($"<< /Type /Catalog /Pages {pagesId} 0 R >>"));
        objects.Add(PdfObject.Text($"<< /Type /Pages /Kids [{string.Join(" ", pageKids.Select(id => $"{id} 0 R"))}] /Count {pageStreams.Count} >>"));
        objects.Add(PdfObject.Text("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        if (logo != null)
            objects.Add(PdfObject.Binary($"<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {logo.Bytes.Length} >>\nstream\n", logo.Bytes, "\nendstream"));

        for (int i = 0; i < pageStreams.Count; i++)
        {
            int contentId = firstContentId + i;
            string xObject = logo != null ? $" /XObject << /Logo {imageId} 0 R >>" : string.Empty;
            objects.Add(PdfObject.Text($"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {PageWidth.ToString(CultureInfo.InvariantCulture)} {PageHeight.ToString(CultureInfo.InvariantCulture)}] /Resources << /Font << /F1 {fontId} 0 R >>{xObject} >> /Contents {contentId} 0 R >>"));
        }

        foreach (string stream in pageStreams)
        {
            int length = Encoding.ASCII.GetByteCount(stream);
            objects.Add(PdfObject.Text($"<< /Length {length} >>\nstream\n{stream}\nendstream"));
        }

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add((int)output.Position);
            WriteAscii(output, $"{i + 1} 0 obj\n");
            objects[i].WriteTo(output);
            WriteAscii(output, "\nendobj\n");
        }

        int xrefOffset = (int)output.Position;
        var sb = new StringBuilder();
        sb.Append("xref\n");
        sb.Append("0 ").Append(objects.Count + 1).Append('\n');
        sb.Append("0000000000 65535 f \n");
        for (int i = 1; i < offsets.Count; i++)
            sb.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");

        sb.Append("trailer\n");
        sb.Append($"<< /Size {objects.Count + 1} /Root {catalogId} 0 R >>\n");
        sb.Append("startxref\n");
        sb.Append(xrefOffset).Append('\n');
        sb.Append("%%EOF");

        WriteAscii(output, sb.ToString());
        File.WriteAllBytes(path, output.ToArray());
    }

    private static void WriteAscii(Stream stream, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private sealed class PdfCanvas
    {
        private readonly StringBuilder _sb = new();

        public void Text(string value, double x, double y, double size, bool bold = false, double r = 0, double g = 0, double b = 0)
        {
            FillRgb(r, g, b);
            _sb.Append("BT /F1 ").Append(N(size)).Append(" Tf ");
            if (bold)
                _sb.Append("0.2 Tc ");
            _sb.Append(N(x)).Append(' ').Append(N(y)).Append(" Td (").Append(Escape(ToPdfText(value))).Append(") Tj ET\n");
            if (bold)
                _sb.Append("0 Tc\n");
        }

        public void Line(double x1, double y1, double x2, double y2)
        {
            _sb.Append(N(x1)).Append(' ').Append(N(y1)).Append(" m ")
               .Append(N(x2)).Append(' ').Append(N(y2)).Append(" l S\n");
        }

        public void Rect(double x, double y, double width, double height, bool fill, bool stroke)
        {
            _sb.Append(N(x)).Append(' ').Append(N(y)).Append(' ').Append(N(width)).Append(' ').Append(N(height)).Append(" re ");
            _sb.Append(fill && stroke ? "B\n" : fill ? "f\n" : "S\n");
        }

        public void Polygon(IReadOnlyList<(double X, double Y)> points, bool fill, bool stroke)
        {
            if (points.Count == 0) return;
            _sb.Append(N(points[0].X)).Append(' ').Append(N(points[0].Y)).Append(" m ");
            for (int i = 1; i < points.Count; i++)
                _sb.Append(N(points[i].X)).Append(' ').Append(N(points[i].Y)).Append(" l ");
            _sb.Append("h ");
            _sb.Append(fill && stroke ? "B\n" : fill ? "f\n" : "S\n");
        }

        public void Image(string name, double x, double y, double width, double height)
        {
            _sb.Append("q ")
                .Append(N(width)).Append(" 0 0 ").Append(N(height)).Append(' ')
                .Append(N(x)).Append(' ').Append(N(y)).Append(" cm /")
                .Append(name).Append(" Do Q\n");
        }

        public void FillRgb(double r, double g, double b)
        {
            _sb.Append(N(r)).Append(' ').Append(N(g)).Append(' ').Append(N(b)).Append(" rg\n");
        }

        public void StrokeRgb(double r, double g, double b)
        {
            _sb.Append(N(r)).Append(' ').Append(N(g)).Append(' ').Append(N(b)).Append(" RG\n");
        }

        public override string ToString() => _sb.ToString();

        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Escape(string value) => value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");

        private static string ToPdfText(string value)
        {
            return value
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace('ş', 's').Replace('Ş', 'S')
                .Replace('ğ', 'g').Replace('Ğ', 'G')
                .Replace('ü', 'u').Replace('Ü', 'U')
                .Replace('ö', 'o').Replace('Ö', 'O')
                .Replace('ç', 'c').Replace('Ç', 'C')
                .Replace('²', '2')
                .Replace('–', '-').Replace('—', '-');
        }
    }

    private sealed record LogoImage(int Width, int Height, byte[] Bytes);

    private sealed class PdfObject
    {
        private readonly string _prefix;
        private readonly byte[]? _bytes;
        private readonly string _suffix;

        private PdfObject(string prefix, byte[]? bytes = null, string suffix = "")
        {
            _prefix = prefix;
            _bytes = bytes;
            _suffix = suffix;
        }

        public static PdfObject Text(string value) => new(value);
        public static PdfObject Binary(string prefix, byte[] bytes, string suffix) => new(prefix, bytes, suffix);

        public void WriteTo(Stream stream)
        {
            WriteAscii(stream, _prefix);
            if (_bytes != null)
                stream.Write(_bytes, 0, _bytes.Length);
            WriteAscii(stream, _suffix);
        }
    }
}
