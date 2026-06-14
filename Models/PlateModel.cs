using NestLaserDesktop.Geometry;

namespace NestLaserDesktop.Models;

public class PlateModel
{
    public string Id { get; set; } = "Plate";
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 2000;
    public double Margin { get; set; } = 5;
    public double Gap { get; set; } = 2;
    public double MaterialThickness { get; set; } = 0;

    public double UsableWidth => Width - 2 * Margin;
    public double UsableHeight => Height - 2 * Margin;
    public double TotalArea => Width * Height;
    public double UsableArea => UsableWidth * UsableHeight;

    public Polygon ToPolygon()
    {
        var poly = GeometryUtils.CreateRectangle(Width, Height);
        poly.Calculate();
        return poly;
    }
}
