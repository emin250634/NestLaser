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
        var result = new NestResult();
        var sorted = parts.OrderByDescending(p => p.Area).ToList();

        var currentPlate = ClonePlate(plate);
        result.Plates.Add(currentPlate);
        var skylineHeights = new double[1];

        foreach (var part in sorted)
        {
            bool placed = false;

            if (settings.AllowRotation0)
            {
                placed = TryPlace(part, currentPlate, settings, result, skylineHeights, false);
            }

            if (!placed && settings.AllowRotation90)
            {
                placed = TryPlace(part, currentPlate, settings, result, skylineHeights, true);
            }

            if (!placed)
            {
                var newPlate = ClonePlate(plate);
                result.Plates.Add(newPlate);
                currentPlate = newPlate;
                skylineHeights = new double[1];

                if (settings.AllowRotation0)
                {
                    placed = TryPlace(part, currentPlate, settings, result, skylineHeights, false);
                }

                if (!placed && settings.AllowRotation90)
                {
                    placed = TryPlace(part, currentPlate, settings, result, skylineHeights, true);
                }
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

            if (HasOverlap(x, y, effectiveW, effectiveH, result, plate))
                return false;

            var placement = new NestPlacement
            {
                PartId = part.Id,
                PartName = part.Name,
                Part = part,
                X = x,
                Y = y,
                RotationDeg = rotated ? 90 : 0,
                PlateIndex = result.Plates.IndexOf(plate),
                Width = pw,
                Height = ph,
                TransformedGeometry = part.Geometry.Transform(x, y, rotated)
            };

            result.Placed.Add(placement);
            result.UsedArea += part.Area;

            UpdateSkyline(skylineHeights, position.Value.X, effectiveW, position.Value.Y + effectiveH, plate.UsableWidth);

            return true;
        }

        return false;
    }

    private bool HasOverlap(double x, double y, double w, double h, NestResult result, PlateModel currentPlate)
    {
        int currentPlateIndex = result.Plates.IndexOf(currentPlate);

        foreach (var placed in result.Placed)
        {
            if (placed.PlateIndex != currentPlateIndex) continue;

            if (x < placed.X + placed.Width &&
                x + w > placed.X &&
                y < placed.Y + placed.Height &&
                y + h > placed.Y)
            {
                return true;
            }
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

    private PlateModel ClonePlate(PlateModel source) => new()
    {
        Id = source.Id,
        Width = source.Width,
        Height = source.Height,
        Margin = source.Margin,
        Gap = source.Gap,
        MaterialThickness = source.MaterialThickness
    };
}
