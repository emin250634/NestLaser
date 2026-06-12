using System;
using System.Collections.Generic;
using System.Linq;
using NestLaserDesktop.Geometry;
using NestLaserDesktop.Models;

namespace NestLaserDesktop.Nesting;

public class NestingEngine
{
    public NestResult Run(List<PartModel> parts, PlateModel plate, NestSettings settings)
    {
        var result = new NestResult { PlateArea = plate.TotalArea };
        var sorted = parts
            .OrderByDescending(p => p.Area)
            .ToList();

        double[] skylineHeights = new double[1];

        foreach (var part in sorted)
        {
            bool placed = false;

            if (settings.AllowRotation0)
            {
                placed = TryPlace(part, plate, settings, result, skylineHeights, false);
            }

            if (!placed && settings.AllowRotation90)
            {
                placed = TryPlace(part, plate, settings, result, skylineHeights, true);
            }

            if (!placed)
            {
                result.Unplaced.Add(part);
            }
        }

        return result;
    }

    private bool TryPlace(PartModel part, PlateModel plate, NestSettings settings, NestResult result, double[] skylineHeights, bool rotated)
    {
        double pw = rotated ? part.Height : part.Width;
        double ph = rotated ? part.Width : part.Height;
        double gap = settings.GapBetweenParts;
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
                TransformedGeometry = part.Geometry.Transform(x, y, rotated)
            };

            result.Placed.Add(placement);
            result.UsedArea += part.Area;

            UpdateSkyline(skylineHeights, position.Value.X, effectiveW, position.Value.Y + effectiveH, plate.UsableWidth);

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

    private void UpdateSkyline(double[] skylineHeights, double x, double w, double newY, double plateW)
    {
        int resolution = skylineHeights.Length;
        if (resolution == 0) return;

        int startIdx = Math.Max(0, (int)(x / plateW * resolution));
        int endIdx = Math.Min(resolution - 1, (int)((x + w) / plateW * resolution));

        for (int i = startIdx; i <= endIdx; i++)
        {
            skylineHeights[i] = newY;
        }
    }
}
