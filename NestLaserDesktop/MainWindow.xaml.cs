using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NestLaserDesktop.Models;

namespace NestLaserDesktop;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => DrawPreview();
    }

    private void OnLoadDxf(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModels.MainViewModel)DataContext;
        vm.LoadDxf();
        DrawPreview();
    }

    private void OnRunNesting(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModels.MainViewModel)DataContext;
        vm.RunNesting();
        DrawPreview();
    }

    private void OnExportDxf(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModels.MainViewModel)DataContext;
        vm.ExportDxf();
    }

    private void DrawPreview()
    {
        NestCanvas.Children.Clear();

        var vm = DataContext as ViewModels.MainViewModel;
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

        var plateBrush = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x30));
        var plateBorder = new SolidColorBrush(Color.FromRgb(0x56, 0x9c, 0xd6));
        var partBrush = new SolidColorBrush(Color.FromArgb(80, 0x4e, 0xc9, 0xb0));
        var partBorder = new SolidColorBrush(Color.FromRgb(0x4e, 0xc9, 0xb0));

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
                Color.FromRgb(0x4e, 0xc9, 0xb0), Color.FromRgb(0x56, 0x9c, 0xd6),
                Color.FromRgb(0xdc, 0xdc, 0xaa), Color.FromRgb(0xc5, 0x86, 0xc0),
                Color.FromRgb(0xce, 0x91, 0x78), Color.FromRgb(0xb5, 0xce, 0xa8),
                Color.FromRgb(0xd7, 0xba, 0x7d), Color.FromRgb(0x9c, 0xdc, 0xfe)
            };

            foreach (var placement in vm.NestResult.Placed)
            {
                if (placement.TransformedVertices.Count < 3) continue;

                var fill = new SolidColorBrush(Color.FromArgb(100, colors[random.Next(colors.Length)].R, colors[random.Next(colors.Length)].G, colors[random.Next(colors.Length)].B));
                var stroke = new SolidColorBrush(colors[random.Next(colors.Length)]);

                var pg = new System.Windows.Shapes.Path();
                var geometry = new StreamGeometry();

                using (var ctx = geometry.Open())
                {
                    ctx.BeginTransformTo(GetIdentityMatrix());
                    var first = placement.TransformedVertices[0];
                    ctx.BeginFigure(new System.Windows.Point(first.X * scale + offsetX, (plate.Height - first.Y) * scale + offsetY), true, true);

                    for (int i = 1; i < placement.TransformedVertices.Count; i++)
                    {
                        var v = placement.TransformedVertices[i];
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
                foreach (var v in placement.TransformedVertices) { cx += v.X; cy += v.Y; }
                cx /= placement.TransformedVertices.Count;
                cy /= placement.TransformedVertices.Count;
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
            int cols = 0;

            foreach (var part in vm.Parts)
            {
                if (x + part.Width * scale > offsetX + plate.Width * scale - 10)
                {
                    x = offsetX + 10;
                    y += maxH + 10;
                    maxH = 0;
                    cols = 0;
                }

                if (y + part.Height * scale > offsetY + plate.Height * scale - 10) break;

                var pg = new System.Windows.Shapes.Path();
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginTransformTo(GetIdentityMatrix());
                    var first = part.Vertices[0];
                    ctx.BeginFigure(new System.Windows.Point(first.X * scale + x, (part.Height - first.Y) * scale + y), true, true);
                    for (int i = 1; i < part.Vertices.Count; i++)
                    {
                        var v = part.Vertices[i];
                        ctx.LineTo(new System.Windows.Point(v.X * scale + x, (part.Height - v.Y) * scale + y), true, false);
                    }
                }

                pg.Data = geometry;
                pg.Fill = new SolidColorBrush(Color.FromArgb(50, 0x56, 0x9c, 0xd6));
                pg.Stroke = new SolidColorBrush(Color.FromRgb(0x56, 0x9c, 0xd6));
                pg.StrokeThickness = 1;

                NestCanvas.Children.Add(pg);

                if (part.Height > maxH) maxH = part.Height;
                x += part.Width * scale + 10;
                cols++;
            }
        }
    }

    private static Matrix GetIdentityMatrix() => Matrix.Identity;

    private void NestCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawPreview();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is int i && i == 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
