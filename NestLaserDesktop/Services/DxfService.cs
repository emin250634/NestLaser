using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;
using NestLaserDesktop.Utilities;

namespace NestLaserDesktop.Services;

public static class DxfService
{
    public static List<PartModel> Import(string filePath)
    {
        var doc = DxfDocument.Load(filePath);
        if (doc == null)
            throw new InvalidOperationException($"DXF dosyası yüklenemedi: {filePath}");

        var parts = new List<PartModel>();
        string sourceFile = Path.GetFileNameWithoutExtension(filePath);

        foreach (var polyline in doc.Entities.LwPolylines)
        {
            if (!polyline.IsClosed) continue;

            var polygon = new Polygon();
            polygon.Vertices = polyline.Vertexes
                .Select(v => new Point2D(v.Position.X, v.Position.Y))
                .ToList();

            if (polygon.Vertices.Count < 3) continue;

            polygon.NormalizeWinding();
            polygon.Calculate();

            parts.Add(new PartModel
            {
                Name = $"{sourceFile}_P{parts.Count + 1}",
                SourceFile = sourceFile,
                LayerName = polyline.Layer?.Name ?? "0",
                Geometry = polygon
            });
        }

        foreach (var polyline in doc.Entities.Polylines)
        {
            if (!polyline.IsClosed) continue;

            var polygon = new Polygon();
            polygon.Vertices = polyline.Vertexes
                .Select(v => new Point2D(v.Position.X, v.Position.Y))
                .ToList();

            if (polygon.Vertices.Count < 3) continue;

            polygon.NormalizeWinding();
            polygon.Calculate();

            parts.Add(new PartModel
            {
                Name = $"{sourceFile}_P{parts.Count + 1}",
                SourceFile = sourceFile,
                LayerName = polyline.Layer?.Name ?? "0",
                Geometry = polygon
            });
        }

        foreach (var circle in doc.Entities.Circles)
        {
            var center = new Point2D(circle.Center.X, circle.Center.Y);
            var vertices = GeometryUtils.CircleToPolygon(center, circle.Radius, AppConstants.CircleSegments);

            var polygon = new Polygon();
            polygon.Vertices = vertices;
            polygon.Calculate();

            parts.Add(new PartModel
            {
                Name = $"{sourceFile}_C{parts.Count + 1}",
                SourceFile = sourceFile,
                LayerName = circle.Layer?.Name ?? "0",
                Geometry = polygon
            });
        }

        return parts;
    }

    public static void Export(string filePath, PlateModel plate, List<NestPlacement> placements)
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

            foreach (var v in p.TransformedGeometry.Vertices)
            {
                poly.Vertexes.Add(new LwPolylineVertex(v.X, v.Y));
            }

            doc.AddEntity(poly);
        }

        doc.Save(filePath);
    }
}
