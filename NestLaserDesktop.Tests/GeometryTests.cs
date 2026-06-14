using System;
using System.Linq;
using NestLaserDesktop.Geometry;
using Xunit;

namespace NestLaserDesktop.Tests;

public class GeometryTests
{
    [Fact]
    [Trait("Category", "GeometryTests")]
    public void PolygonAreaAndPerimeter_AreStable()
    {
        var polygon = GeometryUtils.CreateRectangle(100, 50);

        Assert.Equal(5000, polygon.Area, 6);
        Assert.Equal(300, Perimeter(polygon), 6);
        Assert.Equal(100, polygon.Bounds.Width, 6);
        Assert.Equal(50, polygon.Bounds.Height, 6);
    }

    [Fact]
    [Trait("Category", "GeometryTests")]
    public void NormalizeWinding_ForClockwisePolygon_MakesSignedAreaPositive()
    {
        var polygon = GeometryUtils.CreateRectangle(100, 50);
        polygon.Vertices.Reverse();

        Assert.True(SignedArea(polygon) < 0);

        polygon.NormalizeWinding();
        polygon.Calculate();

        Assert.True(SignedArea(polygon) > 0);
        Assert.True(polygon.IsValid());
    }

    [Fact]
    [Trait("Category", "GeometryTests")]
    public void CleanupVertices_RemovesDuplicatesAndCollinearPoints_WithoutInvalidatingPolygon()
    {
        var polygon = new Polygon
        {
            Vertices =
            [
                new Point2D(0, 0),
                new Point2D(50, 0),
                new Point2D(100, 0),
                new Point2D(100, 50),
                new Point2D(100, 50),
                new Point2D(0, 50),
                new Point2D(0, 0)
            ]
        };
        polygon.Calculate();

        var removed = polygon.CleanupVertices();

        Assert.True(removed > 0);
        Assert.True(polygon.IsValid());
        Assert.Equal(4, polygon.Vertices.Count);
        Assert.Equal(5000, polygon.Area, 6);
    }

    [Fact]
    [Trait("Category", "GeometryTests")]
    public void IsValid_RejectsDegeneratePolygon()
    {
        var polygon = new Polygon
        {
            Vertices = [new Point2D(0, 0), new Point2D(10, 0)]
        };
        polygon.Calculate();

        Assert.False(polygon.IsValid());
    }

    [Fact]
    [Trait("Category", "GeometryTests")]
    public void MoveScaleRotateAndMirror_PreserveExpectedGeometry()
    {
        var polygon = GeometryUtils.CreateRectangle(100, 50);

        polygon.Move(10, 20);
        Assert.Equal(10, polygon.Bounds.MinX, 6);
        Assert.Equal(20, polygon.Bounds.MinY, 6);

        polygon.Scale(2);
        Assert.Equal(200, polygon.Bounds.Width, 6);
        Assert.Equal(100, polygon.Bounds.Height, 6);
        Assert.Equal(20000, polygon.Area, 6);

        polygon.RotateAroundCenter(90);
        Assert.Equal(100, Math.Round(polygon.Bounds.Width, 6), 6);
        Assert.Equal(200, Math.Round(polygon.Bounds.Height, 6), 6);

        polygon.MirrorX();
        Assert.True(SignedArea(polygon) > 0);
        Assert.True(polygon.IsValid());

        polygon.MirrorY();
        Assert.True(SignedArea(polygon) > 0);
        Assert.True(polygon.IsValid());
    }

    private static double Perimeter(Polygon polygon)
    {
        var vertices = polygon.Vertices;
        return vertices.Select((point, index) => point.DistanceTo(vertices[(index + 1) % vertices.Count])).Sum();
    }

    private static double SignedArea(Polygon polygon)
    {
        double area = 0;
        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var current = polygon.Vertices[i];
            var next = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
            area += current.X * next.Y - next.X * current.Y;
        }

        return area / 2.0;
    }
}
