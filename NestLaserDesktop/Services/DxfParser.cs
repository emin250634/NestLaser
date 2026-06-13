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

                    value = value.ToUpperInvariant();

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
                    else if (code == "0" && value == "SPLINE")
                    {
                        var entity = ParseSpline(lines, ref i);
                        if (entity != null) entities.Add(entity);
                    }
                    else if (code == "0" && value == "ELLIPSE")
                    {
                        var entity = ParseEllipse(lines, ref i);
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
            string layerName = string.Empty;
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
                else if (code == "8")
                {
                    layerName = value;
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
                IsClosed = isClosed,
                LayerName = layerName
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
            string layerName = string.Empty;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "70")
                {
                    if (int.TryParse(value, out int flags))
                        isClosed = (flags & 1) == 1;
                    i += 2;
                }
                else if (code == "8")
                {
                    layerName = value;
                    i += 2;
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
                IsClosed = isClosed,
                LayerName = layerName
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
            string layerName = string.Empty;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "10") cx = ParseDouble(value);
                else if (code == "8") layerName = value;
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

            startIndex = i;

            return new DxfEntity
            {
                Type = DxfEntityType.Circle,
                Vertices = vertices,
                IsClosed = true,
                LayerName = layerName
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
            string layerName = string.Empty;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "10") cx = ParseDouble(value);
                else if (code == "8") layerName = value;
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
                IsClosed = false,
                LayerName = layerName
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
            string layerName = string.Empty;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "10") x1 = ParseDouble(value);
                else if (code == "8") layerName = value;
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
                IsClosed = false,
                LayerName = layerName
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static DxfEntity? ParseSpline(string[] lines, ref int startIndex)
    {
        try
        {
            var vertices = new List<Point2D>();
            string layerName = string.Empty;
            double x = 0, y = 0;
            bool readingPoint = false;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "8")
                {
                    layerName = value;
                }
                else if (code == "10" || code == "11")
                {
                    if (readingPoint)
                        vertices.Add(new Point2D(x, y));
                    x = ParseDouble(value);
                    y = 0;
                    readingPoint = true;
                }
                else if (code == "20" || code == "21")
                {
                    y = ParseDouble(value);
                }

                i += 2;
            }

            if (readingPoint)
                vertices.Add(new Point2D(x, y));

            startIndex = i;

            if (vertices.Count < 2) return null;

            return new DxfEntity
            {
                Type = DxfEntityType.Spline,
                Vertices = vertices,
                IsClosed = false,
                LayerName = layerName
            };
        }
        catch
        {
            startIndex = Math.Min(startIndex + 2, lines.Length);
            return null;
        }
    }

    private static DxfEntity? ParseEllipse(string[] lines, ref int startIndex)
    {
        try
        {
            double cx = 0, cy = 0;
            double majorX = 0, majorY = 0;
            double ratio = 1;
            double startParam = 0;
            double endParam = 2 * Math.PI;
            string layerName = string.Empty;

            int i = startIndex + 2;
            while (i < lines.Length - 1)
            {
                string code = lines[i]?.Trim() ?? string.Empty;
                string value = lines[i + 1]?.Trim() ?? string.Empty;

                if (code == "0") break;

                if (code == "8") layerName = value;
                else if (code == "10") cx = ParseDouble(value);
                else if (code == "20") cy = ParseDouble(value);
                else if (code == "11") majorX = ParseDouble(value);
                else if (code == "21") majorY = ParseDouble(value);
                else if (code == "40") ratio = ParseDouble(value);
                else if (code == "41") startParam = ParseDouble(value);
                else if (code == "42") endParam = ParseDouble(value);

                i += 2;
            }

            startIndex = i;

            double majorLength = Math.Sqrt(majorX * majorX + majorY * majorY);
            if (majorLength <= 0 || ratio <= 0) return null;

            if (endParam < startParam)
                endParam += 2 * Math.PI;

            double span = endParam - startParam;
            if (span <= 0 || double.IsNaN(span) || double.IsInfinity(span))
                span = 2 * Math.PI;

            double ux = majorX / majorLength;
            double uy = majorY / majorLength;
            double minorLength = majorLength * ratio;
            double vx = -uy;
            double vy = ux;
            int segments = Math.Clamp((int)(span / (Math.PI / 36)), 24, 144);

            var vertices = new List<Point2D>();
            for (int j = 0; j <= segments; j++)
            {
                double t = startParam + span * j / segments;
                double x = cx + majorLength * Math.Cos(t) * ux + minorLength * Math.Sin(t) * vx;
                double y = cy + majorLength * Math.Cos(t) * uy + minorLength * Math.Sin(t) * vy;
                vertices.Add(new Point2D(x, y));
            }

            startIndex = i;

            return new DxfEntity
            {
                Type = DxfEntityType.Ellipse,
                Vertices = vertices,
                IsClosed = Math.Abs(span - 2 * Math.PI) < 1e-4,
                LayerName = layerName
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
    Line,
    Spline,
    Ellipse
}

internal class DxfEntity
{
    public DxfEntityType Type { get; set; }
    public List<Point2D> Vertices { get; set; } = new();
    public bool IsClosed { get; set; }
    public string LayerName { get; set; } = string.Empty;
}
