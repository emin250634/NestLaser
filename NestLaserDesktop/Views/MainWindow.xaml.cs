using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NestLaserDesktop.Models;
using NestLaserDesktop.ViewModels;

namespace NestLaserDesktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as MainViewModel;
        if (_vm != null)
        {
            _vm.RequestDrawPreview += DrawPreview;
            _vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.Parts) ||
                    args.PropertyName == nameof(MainViewModel.NestResult))
                {
                    DrawPreview();
                }
            };
        }
    }

    private void DrawPreview()
    {
        NestCanvas.Children.Clear();

        if (_vm == null) return;

        var canvasW = NestCanvas.ActualWidth;
        var canvasH = NestCanvas.ActualHeight;

        if (canvasW < 10 || canvasH < 10) return;

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            DrawMultiPlateNesting(_vm.NestResult, canvasW, canvasH);
        }
        else if (_vm.Parts.Count > 0)
        {
            DrawSinglePlatePreview(_vm.Plate, _vm.Parts, canvasW, canvasH);
        }
    }

    private void DrawSinglePlatePreview(PlateModel plate, ObservableCollection<PartModel> parts, double canvasW, double canvasH)
    {
        double padding = 30;
        double drawW = canvasW - 2 * padding;
        double drawH = canvasH - 2 * padding;

        double scaleX = drawW / plate.Width;
        double scaleY = drawH / plate.Height;
        double scale = Math.Min(scaleX, scaleY);

        double offsetX = padding + (drawW - plate.Width * scale) / 2;
        double offsetY = padding + (drawH - plate.Height * scale) / 2;

        DrawPlate(plate, scale, offsetX, offsetY, "Plaka");
        DrawPartsOnPlate(parts, plate, scale, offsetX, offsetY);
    }

    private void DrawMultiPlateNesting(NestResult result, double canvasW, double canvasH)
    {
        if (result.Plates.Count == 0) return;

        double padding = 30;
        double gapBetweenPlates = 20;

        double totalWidth = result.Plates.Sum(p => p.Width) + gapBetweenPlates * (result.Plates.Count - 1);
        double maxHeight = result.Plates.Max(p => p.Height);

        double drawAreaW = canvasW - 2 * padding;
        double drawAreaH = canvasH - 2 * padding;

        double multiScaleX = drawAreaW / totalWidth;
        double multiScaleY = drawAreaH / maxHeight;
        double scale = Math.Min(multiScaleX, multiScaleY);

        if (result.Plates.Count == 1)
        {
            var plate = result.Plates[0];
            double singleDrawW = canvasW - 2 * padding;
            double singleDrawH = canvasH - 2 * padding;
            double singleScaleX = singleDrawW / plate.Width;
            double singleScaleY = singleDrawH / plate.Height;
            scale = Math.Min(singleScaleX, singleScaleY);
            double offsetX = padding + (singleDrawW - plate.Width * scale) / 2;
            double offsetY = padding + (singleDrawH - plate.Height * scale) / 2;

            DrawPlate(plate, scale, offsetX, offsetY, "Plaka 1");
            DrawPlacementsOnPlate(result, 0, plate, scale, offsetX, offsetY);
            return;
        }

        double currentX = padding + (drawAreaW - totalWidth * scale) / 2;
        double baseY = padding + (drawAreaH - maxHeight * scale) / 2;

        for (int i = 0; i < result.Plates.Count; i++)
        {
            var plate = result.Plates[i];
            double plateW = plate.Width * scale;
            double plateH = plate.Height * scale;
            double offsetY = baseY + (maxHeight * scale - plateH) / 2;

            DrawPlate(plate, scale, currentX, offsetY, $"Plaka {i + 1}");
            DrawPlacementsOnPlate(result, i, plate, scale, currentX, offsetY);

            currentX += plateW + gapBetweenPlates * scale;
        }
    }

    private void DrawPlate(PlateModel plate, double scale, double offsetX, double offsetY, string label)
    {
        var plateRect = new System.Windows.Shapes.Rectangle
        {
            Width = plate.Width * scale,
            Height = plate.Height * scale,
            Fill = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Stroke = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),
            StrokeThickness = 2
        };
        Canvas.SetLeft(plateRect, offsetX);
        Canvas.SetTop(plateRect, offsetY);
        NestCanvas.Children.Add(plateRect);

        if (plate.Margin > 0)
        {
            double marginScale = plate.Margin * scale;
            var marginRect = new System.Windows.Shapes.Rectangle
            {
                Width = (plate.Width - 2 * plate.Margin) * scale,
                Height = (plate.Height - 2 * plate.Margin) * scale,
                Stroke = new SolidColorBrush(Color.FromArgb(40, 0x56, 0x9C, 0xD6)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(marginRect, offsetX + marginScale);
            Canvas.SetTop(marginRect, offsetY + marginScale);
            NestCanvas.Children.Add(marginRect);
        }

        var sizeLabel = new TextBlock
        {
            Text = $"{label} | {plate.Width}x{plate.Height} mm",
            Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),
            FontSize = 11,
            FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(sizeLabel, offsetX + 4);
        Canvas.SetTop(sizeLabel, offsetY - 18);
        NestCanvas.Children.Add(sizeLabel);
    }

    private void DrawPlacementsOnPlate(NestResult result, int plateIndex, PlateModel plate, double scale, double offsetX, double offsetY)
    {
        var platePlacements = result.Placed.Where(p => p.PlateIndex == plateIndex).ToList();
        var colors = GetColorPalette();

        for (int i = 0; i < platePlacements.Count; i++)
        {
            var placement = platePlacements[i];
            if (placement.TransformedGeometry.Vertices.Count < 2) continue;

            var color = colors[i % colors.Count];
            var fillColor = Color.FromArgb(80, color.R, color.G, color.B);

            DrawPolygonOnCanvas(placement.TransformedGeometry.Vertices, plate.Height,
                fillColor, new SolidColorBrush(color), 1, scale, offsetX, offsetY);

            double cx = 0, cy = 0;
            foreach (var v in placement.TransformedGeometry.Vertices) { cx += v.X; cy += v.Y; }
            cx /= placement.TransformedGeometry.Vertices.Count;
            cy /= placement.TransformedGeometry.Vertices.Count;

            var label = new TextBlock
            {
                Text = placement.PartName,
                Foreground = new SolidColorBrush(color),
                FontSize = 9,
                FontFamily = new FontFamily("Consolas")
            };
            Canvas.SetLeft(label, cx * scale + offsetX - 15);
            Canvas.SetTop(label, (plate.Height - cy) * scale + offsetY - 6);
            NestCanvas.Children.Add(label);
        }
    }

    private void DrawPartsOnPlate(ObservableCollection<PartModel> parts, PlateModel plate, double scale, double offsetX, double offsetY)
    {
        double x = offsetX + 5;
        double y = offsetY + 5;
        double maxH = 0;
        int colorIndex = 0;
        var colors = GetColorPalette();

        foreach (var part in parts)
        {
            if (part.Geometry.Vertices.Count < 2) continue;

            double partW = part.Width * scale;
            double partH = part.Height * scale;

            if (x + partW > offsetX + plate.Width * scale - 5)
            {
                x = offsetX + 5;
                y += maxH + 8;
                maxH = 0;
                colorIndex = (colorIndex + 1) % colors.Count;
            }

            if (y + partH > offsetY + plate.Height * scale - 5) break;

            var color = colors[colorIndex % colors.Count];
            var fillColor = Color.FromArgb(60, color.R, color.G, color.B);

            var offsetVertices = new List<Geometry.Point2D>();
            foreach (var v in part.Geometry.Vertices)
            {
                offsetVertices.Add(new Geometry.Point2D(v.X * scale + x, (part.Height - v.Y) * scale + y));
            }

            DrawPolygonDirect(offsetVertices, fillColor, new SolidColorBrush(color), 1);

            if (part.Height > maxH) maxH = part.Height;
            x += partW + 8;
        }
    }

    private void DrawPolygonOnCanvas(List<Geometry.Point2D> vertices, double plateHeight,
        Color fillColor, Brush strokeBrush, double strokeThickness, double scale, double offsetX, double offsetY)
    {
        if (vertices.Count < 2) return;

        var pg = new System.Windows.Shapes.Path();
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            var first = vertices[0];
            ctx.BeginFigure(
                new System.Windows.Point(first.X * scale + offsetX, (plateHeight - first.Y) * scale + offsetY),
                true, true);

            for (int i = 1; i < vertices.Count; i++)
            {
                var v = vertices[i];
                ctx.LineTo(
                    new System.Windows.Point(v.X * scale + offsetX, (plateHeight - v.Y) * scale + offsetY),
                    true, false);
            }
        }

        pg.Data = geometry;
        pg.Fill = new SolidColorBrush(fillColor);
        pg.Stroke = strokeBrush;
        pg.StrokeThickness = strokeThickness;

        NestCanvas.Children.Add(pg);
    }

    private void DrawPolygonDirect(List<Geometry.Point2D> vertices,
        Color fillColor, Brush strokeBrush, double strokeThickness)
    {
        if (vertices.Count < 2) return;

        var pg = new System.Windows.Shapes.Path();
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            var first = vertices[0];
            ctx.BeginFigure(new System.Windows.Point(first.X, first.Y), true, true);

            for (int i = 1; i < vertices.Count; i++)
            {
                var v = vertices[i];
                ctx.LineTo(new System.Windows.Point(v.X, v.Y), true, false);
            }
        }

        pg.Data = geometry;
        pg.Fill = new SolidColorBrush(fillColor);
        pg.Stroke = strokeBrush;
        pg.StrokeThickness = strokeThickness;

        NestCanvas.Children.Add(pg);
    }

    private List<Color> GetColorPalette() => new()
    {
        Color.FromRgb(0x4E, 0xC9, 0xB0),
        Color.FromRgb(0x56, 0x9C, 0xD6),
        Color.FromRgb(0xDC, 0xDC, 0xAA),
        Color.FromRgb(0xC5, 0x86, 0xC0),
        Color.FromRgb(0xCE, 0x91, 0x78),
        Color.FromRgb(0xB5, 0xCE, 0xA8),
        Color.FromRgb(0xD7, 0xBA, 0x7D),
        Color.FromRgb(0x9C, 0xDC, 0xFE)
    };

    private void NestCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawPreview();
}

public class IntToZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int i && i == 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int i && i == 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
