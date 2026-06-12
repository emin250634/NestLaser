using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Services;

internal static class DxfParser
{
    public static List<DxfEntity> Parse(string filePath)
    {
        var entities = new List<DxfEntity>();

        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return entities;

            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
                return entities;

            int i = 0;
            while (i < lines.Length - 1)
            {
                try
                {
                    string code = lines[i]?.Trim() ?? string.Empty;
                    string value = lines[i + 1]?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(value))
                    {
                        i += 2;
                        continue;
                    }

                    if (code == "0" && value == "LWPOLYLINE")
                    {
                        var entity = ParseLwPolyline(lines, ref i);
                        if (entity != null) entities.Add(entity);
                    }
                    else if (code == "0" && value == "POLYLINE")
                    {
                        var entity = ParsePolyline(lines, ref i);
                        if (entity != null) entities.Add(entity);
                    }
                    else if (code == "0" && value == "CIRCLE")
                    {
                        var entity = ParseCircle(lines, ref i);
                        if (entity != null) entities.Add(entity);
                    }
                    else if (code == "0" && value == "ARC")
                    {
                        var entity = ParseArc(lines, ref i);
                        if (entity != null) entities.Add(entity);
                    }
                    else if (code == "0" && value == "LINE")
                    {
                        var entity = ParseLine(lines, ref i);
                        if (entity != null) entities.Add(entity);
                    }
                    else
                    {
                        i += 2;
                    }
                }
                catch
                {
                    i += 2;
                }
            }
        }
        catch
        {
            // Dosya okuma hatası - boş liste dön
        }

        return entities;
    }

    private static DxfEntity? ParseLwPolyline(string[] lines, ref int startIndex)
    {
        try
        {
            var vertices = new List<Point2D>();
            bool isClosed = false;
            double x = 0, y = 0;
            bool readingVertex = false;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "70")
                {
                    if (int.TryParse(value, out int flags))
                        isClosed = (flags & 1) == 1;
                }
                else if (code == "10")
                {
                    if (readingVertex)
                        vertices.Add(new Point2D(x, y));
                    x = ParseDouble(value);
                    y = 0;
                    readingVertex = true;
                }
                else if (code == "20")
                {
                    y = ParseDouble(value);
                }

                i += 2;
            }

            if (readingVertex)
                vertices.Add(new Point2D(x, y));

            startIndex = i;

            if (vertices.Count < 2) return null;

            return new DxfEntity
            {
                Type = DxfEntityType.LwPolyline,
                Vertices = vertices,
                IsClosed = isClosed
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static DxfEntity? ParsePolyline(string[] lines, ref int startIndex)
    {
        try
        {
            var vertices = new List<Point2D>();
            bool isClosed = false;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "70")
                {
                    if (int.TryParse(value, out int flags))
                        isClosed = (flags & 1) == 1;
                }
                else if (code == "0" && value == "VERTEX")
                {
                    i += 2;
                    double vx = 0, vy = 0;
                    while (i < lines.Length - 1)
                    {
                        string vcode = lines[i]?.Trim() ?? string.Empty;
                        string vvalue = lines[i + 1]?.Trim() ?? string.Empty;

                        if (vcode == "0") break;

                        if (vcode == "10") vx = ParseDouble(vvalue);
                        else if (vcode == "20") vy = ParseDouble(vvalue);

                        i += 2;
                    }
                    vertices.Add(new Point2D(vx, vy));
                }
                else if (code == "0" && value == "SEQEND")
                {
                    i += 2;
                    break;
                }
                else
                {
                    i += 2;
                }
            }

            startIndex = i;

            if (vertices.Count < 2) return null;

            return new DxfEntity
            {
                Type = DxfEntityType.Polyline,
                Vertices = vertices,
                IsClosed = isClosed
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static DxfEntity? ParseCircle(string[] lines, ref int startIndex)
    {
        try
        {
            double cx = 0, cy = 0, radius = 0;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "10") cx = ParseDouble(value);
                else if (code == "20") cy = ParseDouble(value);
                else if (code == "40") radius = ParseDouble(value);

                i += 2;
            }

            startIndex = i;

            if (radius <= 0) return null;

            var vertices = new List<Point2D>();
            int segments = 36;
            for (int j = 0; j < segments; j++)
            {
                double angle = 2 * Math.PI * j / segments;
                vertices.Add(new Point2D(
                    cx + radius * Math.Cos(angle),
                    cy + radius * Math.Sin(angle)));
            }

            return new DxfEntity
            {
                Type = DxfEntityType.Circle,
                Vertices = vertices,
                IsClosed = true
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static DxfEntity? ParseArc(string[] lines, ref int startIndex)
    {
        try
        {
            double cx = 0, cy = 0, radius = 0, startAngle = 0, endAngle = 0;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "10") cx = ParseDouble(value);
                else if (code == "20") cy = ParseDouble(value);
                else if (code == "40") radius = ParseDouble(value);
                else if (code == "50") startAngle = ParseDouble(value);
                else if (code == "51") endAngle = ParseDouble(value);

                i += 2;
            }

            startIndex = i;

            if (radius <= 0) return null;

            double startRad = startAngle * Math.PI / 180.0;
            double endRad = endAngle * Math.PI / 180.0;

            if (endRad < startRad)
                endRad += 2 * Math.PI;

            double angleSpan = endRad - startRad;
            if (angleSpan <= 0 || double.IsNaN(angleSpan) || double.IsInfinity(angleSpan))
                return null;

            int segments = Math.Max(8, (int)(angleSpan / (Math.PI / 18)));
            segments = Math.Min(segments, 360);

            var vertices = new List<Point2D>();
            for (int j = 0; j <= segments; j++)
            {
                double t = startRad + angleSpan * j / segments;
                vertices.Add(new Point2D(
                    cx + radius * Math.Cos(t),
                    cy + radius * Math.Sin(t)));
            }

            return new DxfEntity
            {
                Type = DxfEntityType.Arc,
                Vertices = vertices,
                IsClosed = false
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static DxfEntity? ParseLine(string[] lines, ref int startIndex)
    {
        try
        {
            double x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "10") x1 = ParseDouble(value);
                else if (code == "20") y1 = ParseDouble(value);
                else if (code == "11") x2 = ParseDouble(value);
                else if (code == "21") y2 = ParseDouble(value);

                i += 2;
            }

            startIndex = i;

            double dx = x2 - x1;
            double dy = y2 - y1;
            if (dx * dx + dy * dy < 0.0001) return null;

            return new DxfEntity
            {
                Type = DxfEntityType.Line,
                Vertices = new List<Point2D> { new Point2D(x1, y1), new Point2D(x2, y2) },
                IsClosed = false
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static double ParseDouble(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;
        return 0;
    }
}

internal enum DxfEntityType
{
    LwPolyline,
    Polyline,
    Circle,
    Arc,
    Line
}

internal class DxfEntity
{
    public DxfEntityType Type { get; set; }
    public List<Point2D> Vertices { get; set; } = new();
    public bool IsClosed { get; set; }
}
