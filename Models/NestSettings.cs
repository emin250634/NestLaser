namespace NestLaserDesktop.Models;

public class NestSettings
{
    public bool AllowRotation0 { get; set; } = true;
    public bool AllowRotation90 { get; set; } = true;
    public bool AllowAdvancedRotation { get; set; }
    public double GapBetweenParts { get; set; } = 2;
    public double PlateMargin { get; set; } = 5;
    public bool OptimizeByArea { get; set; } = true;
    public int MaxIterations { get; set; } = 1000;
    public NestAlgorithm Algorithm { get; set; } = NestAlgorithm.FreeRectangle;
}

public enum NestAlgorithm
{
    FreeRectangle,
    PolygonCollision,
    IrregularExperimental,
    ShapeAwarePolygon,
    TrueShapeNesting
}
