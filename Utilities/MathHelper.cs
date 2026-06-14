using System;
using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Utilities;

public static class MathHelper
{
    public const double DegToRad = Math.PI / 180.0;
    public const double RadToDeg = 180.0 / Math.PI;

    public static Point2D RotatePoint(Point2D point, Point2D center, double angleDeg)
    {
        double rad = angleDeg * DegToRad;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double dx = point.X - center.X;
        double dy = point.Y - center.Y;
        return new Point2D(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos);
    }

    public static double Clamp(double value, double min, double max)
        => Math.Max(min, Math.Min(max, value));

    public static double RoundTo(double value, double precision)
        => Math.Round(value / precision) * precision;
}
