using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Services;

public static class DxfService
{
    public static List<Part> Import(string filePath)
    {
        var doc = DxfDocument.Load(filePath);
        if (doc == null)
            throw new InvalidOperationException($"DXF dosyası yüklenemedi: {filePath}");

        var parts = new List<Part>();

        foreach (var polyline in doc.Entities.LwPolylines)
        {
            if (!polyline.IsClosed) continue;

            var part = new Part { Name = Path.GetFileNameWithoutExtension(filePath) + "_P" + (parts.Count + 1) };
            var vertices = polyline.Vertexes.Select(v => new Point2D(v.Position.X, v.Position.Y)).ToList();

            if (vertices.Count < 3) continue;

            NormalizeWinding(vertices);
            part.Vertices = vertices;
            part.CalculateBounds();
            parts.Add(part);
        }

        foreach (var polyline in doc.Entities.Polylines)
        {
            if (!polyline.IsClosed) continue;

            var part = new Part { Name = Path.GetFileNameWithoutExtension(filePath) + "_P" + (parts.Count + 1) };
            var vertices = new List<Point2D>();

            foreach (var v in polyline.Vertexes)
            {
                vertices.Add(new Point2D(v.Position.X, v.Position.Y));
            }

            if (vertices.Count < 3) continue;

            NormalizeWinding(vertices);
            part.Vertices = vertices;
            part.CalculateBounds();
            parts.Add(part);
        }

        foreach (var circle in doc.Entities.Circles)
        {
            var part = new Part { Name = Path.GetFileNameWithoutExtension(filePath) + "_C" + (parts.Count + 1) };
            var verts = new List<Point2D>();
            int segments = 36;
            double r = circle.Radius;
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                verts.Add(new Point2D(
                    circle.Center.X + r * Math.Cos(angle),
                    circle.Center.Y + r * Math.Sin(angle)));
            }
            part.Vertices = verts;
            part.CalculateBounds();
            parts.Add(part);
        }

        return parts;
    }

    public static void Export(string filePath, Plate plate, List<NestPlacement> placements)
    {
        var doc = new DxfDocument(DxfVersion.R2010);

        var platePoly = new LwPolyline();
        platePoly.IsClosed = true;
        platePoly.Vertexes.Add(new LwPolylineVertex(0, 0));
        platePoly.Vertexes.Add(new LwPolylineVertex(plate.Width, 0));
        platePoly.Vertexes.Add(new LwPolylineVertex(plate.Width, plate.Height));
        platePoly.Vertexes.Add(new LwPolylineVertex(0, plate.Height));
        doc.Entities.Add(platePoly);

        foreach (var p in placements)
        {
            var poly = new LwPolyline();
            poly.IsClosed = true;

            foreach (var v in p.TransformedVertices)
            {
                poly.Vertexes.Add(new LwPolylineVertex(v.X, v.Y));
            }

            doc.AddEntity(poly);
        }

        doc.Save(filePath);
    }

    private static void NormalizeWinding(List<Point2D> vertices)
    {
        double area = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            int j = (i + 1) % vertices.Count;
            area += vertices[i].X * vertices[j].Y;
            area -= vertices[j].X * vertices[i].Y;
        }

        if (area < 0)
        {
            vertices.Reverse();
        }
    }
}
