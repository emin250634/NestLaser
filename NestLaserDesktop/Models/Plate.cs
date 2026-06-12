namespace NestLaserDesktop.Models;

public class Plate
{
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 2000;
    public double Margin { get; set; } = 5;
    public double Gap { get; set; } = 2;
    public double UsableWidth => Width - 2 * Margin;
    public double UsableHeight => Height - 2 * Margin;
    public double TotalArea => Width * Height;
}
