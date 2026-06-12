using System;
using System.Collections.Generic;
using System.Linq;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Engine;

public class NestingEngine
{
    public NestResult Run(List<Part> parts, Plate plate, bool allowRotation = true)
    {
        var result = new NestResult { PlateArea = plate.TotalArea };
        var sorted = parts.OrderByDescending(p => p.Area).ToList();

        double[] skylineHeights = new double[1];

        foreach (var part in sorted)
        {
            bool placed = false;

            if (allowRotation)
            {
                placed = TryPlace(part, plate, result, skylineHeights, false);
                if (!placed)
                    placed = TryPlace(part, plate, result, skylineHeights, true);
            }
            else
            {
                placed = TryPlace(part, plate, result, skylineHeights, false);
            }

            if (!placed)
            {
                result.Unplaced.Add(part);
            }
        }

        return result;
    }

    private bool TryPlace(Part part, Plate plate, NestResult result, double[] skylineHeights, bool rotated)
    {
        double pw = rotated ? part.Height : part.Width;
        double ph = rotated ? part.Width : part.Height;
        double gap = 2;
        double effectiveW = pw + gap;
        double effectiveH = ph + gap;

        if (effectiveW > plate.UsableWidth || effectiveH > plate.UsableHeight)
            return false;

        var position = FindSkylinePosition(skylineHeights, effectiveW, effectiveH, plate.UsableWidth, plate.UsableHeight);

        if (position.HasValue)
        {
            double x = position.Value.X + plate.Margin;
            double y = position.Value.Y + plate.Margin;

            var placement = new NestPlacement
            {
                Part = part,
                X = x,
                Y = y,
                RotationDeg = rotated ? 90 : 0,
                TransformedVertices = TransformVertices(part.Vertices, x, y, rotated)
            };

            result.Placed.Add(placement);
            result.UsedArea += part.Area;

            UpdateSkyline(skylineHeights, position.Value.X, effectiveW, position.Value.Y + effectiveH);

            return true;
        }

        return false;
    }

    private Point2D? FindSkylinePosition(double[] skylineHeights, double partW, double partH, double plateW, double plateH)
    {
        int resolution = Math.Max(1, (int)(plateW / 1));
        if (skylineHeights.Length < resolution)
            Array.Resize(ref skylineHeights, resolution);

        double bestX = 0, bestY = double.MaxValue;
        bool found = false;

        for (int ix = 0; ix <= resolution; ix++)
        {
            double x = (ix / (double)resolution) * (plateW - partW);
            if (x < 0) continue;

            double maxH = 0;
            int startIdx = (int)(x / plateW * resolution);
            int endIdx = (int)((x + partW) / plateW * resolution);
            endIdx = Math.Min(endIdx, resolution - 1);

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (i < skylineHeights.Length && skylineHeights[i] > maxH)
                    maxH = skylineHeights[i];
            }

            double y = maxH;
            if (y + partH <= plateH && y < bestY)
            {
                bestY = y;
                bestX = x;
                found = true;
            }
        }

        if (!found) return null;
        return new Point2D(bestX, bestY);
    }

    private void UpdateSkyline(double[] skylineHeights, double x, double w, double newY)
    {
        int resolution = skylineHeights.Length;
        if (resolution == 0) return;

        int startIdx = (int)(x / (resolution) * resolution);
        int endIdx = (int)((x + w) / (resolution) * resolution);
        startIdx = Math.Max(0, Math.Min(startIdx, resolution - 1));
        endIdx = Math.Max(0, Math.Min(endIdx, resolution - 1));

        for (int i = startIdx; i <= endIdx; i++)
        {
            skylineHeights[i] = newY;
        }
    }

    private List<Point2D> TransformVertices(List<Point2D> vertices, double tx, double ty, bool rotate90)
    {
        var result = new List<Point2D>();
        foreach (var v in vertices)
        {
            if (rotate90)
                result.Add(new Point2D(tx + v.Y, ty - v.X + v.Y));
            else
                result.Add(new Point2D(tx + v.X, ty + v.Y));
        }
        return result;
    }
}
