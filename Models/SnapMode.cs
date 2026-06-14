namespace NestLaserDesktop.Models;

[Flags]
public enum SnapMode
{
    None = 0,
    Grid = 1,
    Vertex = 2,
    Edge = 4,
    Center = 8
}
