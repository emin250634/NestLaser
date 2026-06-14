namespace NestLaserDesktop.Nesting;

public class FreeRect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }

    public FreeRect(double x, double y, double w, double h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public bool Contains(FreeRect other)
        => X <= other.X + 1e-6 && Y <= other.Y + 1e-6 &&
           X + W >= other.X + other.W - 1e-6 && Y + H >= other.Y + other.H - 1e-6;
}