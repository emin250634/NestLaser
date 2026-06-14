using System;
using System.Globalization;
using System.IO;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

internal static class DxfHeaderParser
{
    public static ImportUnitInfo DetectUnit(string filePath)
    {
        var info = ImportUnitInfo.Default;

        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return info;

            using var reader = new StreamReader(filePath);
            string? line;
            bool inHeader = false;
            int insUnits = -1;
            int measurement = -1;
            int lunits = -1;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                if (trimmed == "HEADER")
                {
                    inHeader = true;
                    continue;
                }

                if (inHeader && trimmed == "CLASSES")
                {
                    inHeader = false;
                    break;
                }

                if (inHeader && trimmed == "ENDSEC")
                {
                    inHeader = false;
                    break;
                }

                if (!inHeader) continue;

                string code = trimmed;
                string? valueLine = reader.ReadLine();
                if (valueLine == null) break;
                string value = valueLine.Trim();

                if (code == "9")
                {
                    if (value == "$INSUNITS")
                    {
                        string? nextCode = reader.ReadLine();
                        string? nextValue = reader.ReadLine();
                        if (nextCode?.Trim() == "70" && int.TryParse(nextValue?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int val))
                            insUnits = val;
                    }
                    else if (value == "$MEASUREMENT")
                    {
                        string? nextCode = reader.ReadLine();
                        string? nextValue = reader.ReadLine();
                        if (nextCode?.Trim() == "70" && int.TryParse(nextValue?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int val))
                            measurement = val;
                    }
                    else if (value == "$LUNITS")
                    {
                        string? nextCode = reader.ReadLine();
                        string? nextValue = reader.ReadLine();
                        if (nextCode?.Trim() == "70" && int.TryParse(nextValue?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int val))
                            lunits = val;
                    }
                }
            }

            if (insUnits >= 0)
            {
                info.SourceUnit = ImportUnitInfo.FromInsUnits(insUnits);
                info.IsUnitDetected = true;
                info.DetectionSource = "$INSUNITS";
            }
            else if (measurement >= 0)
            {
                info.SourceUnit = measurement == 0 ? DxfUnit.Inches : DxfUnit.Millimeters;
                info.IsUnitDetected = true;
                info.DetectionSource = "$MEASUREMENT";
            }
            else if (lunits >= 0)
            {
                info.SourceUnit = lunits switch { 1 or 2 => DxfUnit.Inches, 3 => DxfUnit.Millimeters, 4 => DxfUnit.Millimeters, _ => DxfUnit.Unitless };
                info.IsUnitDetected = true;
                info.DetectionSource = "$LUNITS";
            }
            else
            {
                info.WarningMessage = "DXF başlığında birim bilgisi bulunamadı, varsayılan mm kullanılıyor.";
            }

            info.ScaleFactorToMm = ImportUnitInfo.GetScaleToMm(info.SourceUnit);
        }
        catch
        {
            info.WarningMessage = "DXF başlık okuma hatası, varsayılan mm kullanılıyor.";
        }

        return info;
    }
}
