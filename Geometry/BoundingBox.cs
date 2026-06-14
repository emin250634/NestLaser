namespace NestLaserDesktop.Geometry;

public struct BoundingBox
{
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }

    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double Area => Width * Height;

    public Point2D Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2);

    public BoundingBox(double minX, double minY, double maxX, double maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    public bool Contains(Point2D point)
        => point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;

    public bool Intersects(BoundingBox other)
        => MinX <= other.MaxX && MaxX >= other.MinX && MinY <= other.MaxY && MaxY >= other.MinY;

    public bool Contains(BoundingBox other)
        => MinX <= other.MinX && MaxX >= other.MaxX && MinY <= other.MinY && MaxY >= other.MaxY;
}
