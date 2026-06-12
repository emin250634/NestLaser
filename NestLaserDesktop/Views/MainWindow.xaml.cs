using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NestLaserDesktop.Models;
using NestLaserDesktop.ViewModels;

namespace NestLaserDesktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => DrawPreview();
    }

    private void OnLoadDxf(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        vm.LoadDxf();
        DrawPreview();
    }

    private void OnRunNesting(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        vm.RunNesting();
        DrawPreview();
    }

    private void OnExportDxf(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        vm.ExportDxf();
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        vm.ClearAll();
        DrawPreview();
    }

    private void DrawPreview()
    {
        NestCanvas.Children.Clear();

        var vm = DataContext as MainViewModel;
        if (vm == null) return;

        var plate = vm.Plate;
        var canvasW = NestCanvas.ActualWidth;
        var canvasH = NestCanvas.ActualHeight;

        if (canvasW < 10 || canvasH < 10) return;

        double margin = 20;
        double drawW = canvasW - 2 * margin;
        double drawH = canvasH - 2 * margin;

        double scaleX = drawW / plate.Width;
        double scaleY = drawH / plate.Height;
        double scale = Math.Min(scaleX, scaleY);

        double offsetX = margin + (drawW - plate.Width * scale) / 2;
        double offsetY = margin + (drawH - plate.Height * scale) / 2;

        var plateBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
        var plateBorder = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));

        var plateRect = new System.Windows.Shapes.Rectangle
        {
            Width = plate.Width * scale,
            Height = plate.Height * scale,
            Fill = plateBrush,
            Stroke = plateBorder,
            StrokeThickness = 2
        };
        Canvas.SetLeft(plateRect, offsetX);
        Canvas.SetTop(plateRect, offsetY);
        NestCanvas.Children.Add(plateRect);

        if (vm.NestResult != null)
        {
            var random = new Random(42);
            var colors = new[] {
                Color.FromRgb(0x4E, 0xC9, 0xB0), Color.FromRgb(0x56, 0x9C, 0xD6),
                Color.FromRgb(0xDC, 0xDC, 0xAA), Color.FromRgb(0xC5, 0x86, 0xC0),
                Color.FromRgb(0xCE, 0x91, 0x78), Color.FromRgb(0xB5, 0xCE, 0xA8),
                Color.FromRgb(0xD7, 0xBA, 0x7D), Color.FromRgb(0x9C, 0xDC, 0xFE)
            };

            foreach (var placement in vm.NestResult.Placed)
            {
                if (placement.TransformedGeometry.Vertices.Count < 3) continue;

                var fillColor = colors[random.Next(colors.Length)];
                var strokeColor = colors[random.Next(colors.Length)];
                var fill = new SolidColorBrush(Color.FromArgb(100, fillColor.R, fillColor.G, fillColor.B));
                var stroke = new SolidColorBrush(strokeColor);

                var pg = new System.Windows.Shapes.Path();
                var geometry = new StreamGeometry();

                using (var ctx = geometry.Open())
                {
                    var first = placement.TransformedGeometry.Vertices[0];
                    ctx.BeginFigure(new System.Windows.Point(first.X * scale + offsetX, (plate.Height - first.Y) * scale + offsetY), true, true);

                    for (int i = 1; i < placement.TransformedGeometry.Vertices.Count; i++)
                    {
                        var v = placement.TransformedGeometry.Vertices[i];
                        ctx.LineTo(new System.Windows.Point(v.X * scale + offsetX, (plate.Height - v.Y) * scale + offsetY), true, false);
                    }
                }

                pg.Data = geometry;
                pg.Fill = fill;
                pg.Stroke = stroke;
                pg.StrokeThickness = 1;

                NestCanvas.Children.Add(pg);

                var tb = new TextBlock
                {
                    Text = placement.Part.Id,
                    Foreground = stroke,
                    FontSize = 9,
                    FontFamily = new FontFamily("Consolas")
                };
                double cx = 0, cy = 0;
                foreach (var v in placement.TransformedGeometry.Vertices) { cx += v.X; cy += v.Y; }
                cx /= placement.TransformedGeometry.Vertices.Count;
                cy /= placement.TransformedGeometry.Vertices.Count;
                Canvas.SetLeft(tb, cx * scale + offsetX - 10);
                Canvas.SetTop(tb, (plate.Height - cy) * scale + offsetY - 6);
                NestCanvas.Children.Add(tb);
            }
        }
        else if (vm.Parts.Count > 0)
        {
            double x = offsetX + 10;
            double y = offsetY + 10;
            double maxH = 0;

            foreach (var part in vm.Parts)
            {
                if (x + part.Width * scale > offsetX + plate.Width * scale - 10)
                {
                    x = offsetX + 10;
                    y += maxH + 10;
                    maxH = 0;
                }

                if (y + part.Height * scale > offsetY + plate.Height * scale - 10) break;

                var pg = new System.Windows.Shapes.Path();
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    var first = part.Geometry.Vertices[0];
                    ctx.BeginFigure(new System.Windows.Point(first.X * scale + x, (part.Height - first.Y) * scale + y), true, true);
                    for (int i = 1; i < part.Geometry.Vertices.Count; i++)
                    {
                        var v = part.Geometry.Vertices[i];
                        ctx.LineTo(new System.Windows.Point(v.X * scale + x, (part.Height - v.Y) * scale + y), true, false);
                    }
                }

                pg.Data = geometry;
                pg.Fill = new SolidColorBrush(Color.FromArgb(50, 0x56, 0x9C, 0xD6));
                pg.Stroke = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
                pg.StrokeThickness = 1;

                NestCanvas.Children.Add(pg);

                if (part.Height > maxH) maxH = part.Height;
                x += part.Width * scale + 10;
            }
        }
    }

    private void NestCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawPreview();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int i && i == 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
