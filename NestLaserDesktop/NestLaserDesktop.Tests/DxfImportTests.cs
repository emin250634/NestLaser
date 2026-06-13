using NestLaserDesktop.Services;
using Xunit;

namespace NestLaserDesktop.Tests;

public class DxfImportTests
{
    [Fact]
    [Trait("Category", "DxfImportTests")]
    public void UnitlessLwPolylineFixture_ReturnsExpectedScaleAndBounds()
        => AssertFixture("unitless_lwpolyline.dxf", 1, 100, 50, false, 1.0, 1);

    [Fact]
    [Trait("Category", "DxfImportTests")]
    public void MillimeterLwPolylineFixture_ReturnsExpectedScaleAndBounds()
        => AssertFixture("millimeter_lwpolyline.dxf", 1, 120, 60, true, 1.0, 0);

    [Fact]
    [Trait("Category", "DxfImportTests")]
    public void InchLwPolylineFixture_ReturnsExpectedScaleAndBounds()
        => AssertFixture("inch_lwpolyline.dxf", 1, 25.4, 50.8, true, 25.4, 1);

    [Fact]
    [Trait("Category", "DxfImportTests")]
    public void PolylineFixture_ReturnsExpectedScaleAndBounds()
        => AssertFixture("polyline_rectangle.dxf", 1, 80, 40, true, 1.0, 0);

    [Fact]
    [Trait("Category", "DxfImportTests")]
    public void CircleFixture_ReturnsExpectedScaleAndBounds()
        => AssertFixture("circle.dxf", 1, 20, 20, true, 1.0, 0);

    [Theory]
    [Trait("Category", "DxfImportTests")]
    [InlineData("arc.dxf")]
    [InlineData("spline.dxf")]
    [InlineData("ellipse.dxf")]
    public void CurvedEntityFixtures_ImportAsValidParts(string fileName)
    {
        var result = DxfService.Import(TestPaths.Fixture(fileName));

        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Empty(result.Errors);
        Assert.Single(result.Parts);
        Assert.True(result.TotalBoundingWidth > 0);
        Assert.True(result.TotalBoundingHeight > 0);
        Assert.True(result.Parts[0].Geometry.IsValid());
    }

    private static void AssertFixture(
        string fileName,
        int partCount,
        double expectedWidth,
        double expectedHeight,
        bool unitDetected,
        double scaleFactor,
        int minWarningCount)
    {
        var result = DxfService.Import(TestPaths.Fixture(fileName));

        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Empty(result.Errors);
        Assert.Equal(partCount, result.Parts.Count);
        Assert.Equal(expectedWidth, result.TotalBoundingWidth, 1);
        Assert.Equal(expectedHeight, result.TotalBoundingHeight, 1);
        Assert.Equal(unitDetected, result.UnitInfo.IsUnitDetected);
        Assert.Equal(scaleFactor, result.UnitInfo.ScaleFactorToMm, 4);
        Assert.True(result.Warnings.Count >= minWarningCount);
    }
}
