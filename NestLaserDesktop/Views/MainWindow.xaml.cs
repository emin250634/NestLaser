using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NestLaserDesktop.Models;
using NestLaserDesktop.ViewModels;

namespace NestLaserDesktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;
    private bool _isPanning;
    private Point _panStart;
    private double _zoom = 1.0;
    private double _panX = 0;
    private double _panY = 0;

    private double Sx(double worldX) => worldX * _zoom + _panX;
    private double Sy(double worldY) => worldY * _zoom + _panY;
    private double Wx(double screenX) => (screenX - _panX) / _zoom;
    private double Wy(double screenY) => (screenY - _panY) / _zoom;

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
                try
                {
                    if (args.PropertyName == nameof(MainViewModel.Parts) ||
                        args.PropertyName == nameof(MainViewModel.NestResult) ||
                        args.PropertyName == nameof(MainViewModel.SelectedPart))
                    {
                        DrawPreview();
                    }
                }
                catch { }
            };
        }
    }

    private void UpdateStatus()
    {
        if (_vm != null)
            _vm.ZoomPercent = $"{(int)(_zoom * 100)}";
    }

    private void NestCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(NestCanvas);
        double oldZoom = _zoom;
        double factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        _zoom = Math.Clamp(_zoom * factor, 0.01, 100.0);

        _panX = pos.X - (pos.X - _panX) * (_zoom / oldZoom);
        _panY = pos.Y - (pos.Y - _panY) * (_zoom / oldZoom);

        UpdateStatus();
        DrawPreview();
    }

    private void NestCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle ||
            (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
        {
            _isPanning = true;
            _panStart = e.GetPosition(NestCanvas);
            NestCanvas.CaptureMouse();
            NestCanvas.Cursor = Cursors.Hand;
        }
        else if (e.ChangedButton == MouseButton.Left)
        {
            TrySelectPart(e.GetPosition(NestCanvas));
        }
    }

    private void NestCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(NestCanvas);
        _panX += pos.X - _panStart.X;
        _panY += pos.Y - _panStart.Y;
        _panStart = pos;
        DrawPreview();
    }

    private void NestCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            NestCanvas.ReleaseMouseCapture();
            NestCanvas.Cursor = Cursors.Arrow;
        }
    }

    private void TrySelectPart(Point screenPos)
    {
        if (_vm == null) return;

        double worldX = Wx(screenPos.X);
        double worldY = Wy(screenPos.Y);

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            var plate = _vm.NestResult.Plates.FirstOrDefault();
            if (plate == null) return;

            foreach (var p in _vm.NestResult.Placed)
            {
                if (p.TransformedGeometry.Vertices.Count < 2) continue;
                double minX = p.TransformedGeometry.Vertices.Min(v => v.X);
                double maxX = p.TransformedGeometry.Vertices.Max(v => v.X);
                double minY = p.TransformedGeometry.Vertices.Min(v => v.Y);
                double maxY = p.TransformedGeometry.Vertices.Max(v => v.Y);

                if (worldX >= minX && worldX <= maxX && worldY >= (plate.Height - maxY) && worldY <= (plate.Height - minY))
                {
                    _vm.SelectedPart = _vm.Parts.FirstOrDefault(pt => pt.Name == p.PartName);
                    DrawPreview();
                    return;
                }
            }
        }
        else if (_vm.Parts.Count > 0)
        {
            var plate = _vm.Plate;
            foreach (var part in _vm.Parts)
            {
                var b = part.Geometry.Bounds;
                if (b.Width <= 0 && b.Height <= 0) continue;
                if (worldX >= b.MinX && worldX <= b.MaxX && worldY >= (plate.Height - b.MaxY) && worldY <= (plate.Height - b.MinY))
                {
                    _vm.SelectedPart = part;
                    DrawPreview();
                    return;
                }
            }
        }

        _vm.SelectedPart = null;
        DrawPreview();
    }

    private void FitToScreen_Click(object sender, RoutedEventArgs e) => FitToScreen();

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        _zoom = 1.0;
        _panX = 0;
        _panY = 0;
        UpdateStatus();
        DrawPreview();
    }

    private void FitToScreen()
    {
        if (_vm == null) return;

        double canvasW = NestCanvas.ActualWidth;
        double canvasH = NestCanvas.ActualHeight;
        if (canvasW < 10 || canvasH < 10) return;

        double contentW, contentH;

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            contentW = _vm.NestResult.Plates.Sum(p => p.Width) + 20 * (_vm.NestResult.Plates.Count - 1);
            contentH = _vm.NestResult.Plates.Max(p => p.Height);
        }
        else if (_vm.Parts.Count > 0)
        {
            contentW = _vm.Plate.Width > 0 ? _vm.Plate.Width : 1000;
            contentH = _vm.Plate.Height > 0 ? _vm.Plate.Height : 1000;
        }
        else
        {
            _zoom = 1.0;
            _panX = 0;
            _panY = 0;
            UpdateStatus();
            DrawPreview();
            return;
        }

        double padding = 50;
        double scaleX = (canvasW - 2 * padding) / contentW;
        double scaleY = (canvasH - 2 * padding) / contentH;
        _zoom = Math.Min(scaleX, scaleY);
        _zoom = Math.Clamp(_zoom, 0.01, 100.0);

        _panX = (canvasW - contentW * _zoom) / 2;
        _panY = (canvasH - contentH * _zoom) / 2;

        UpdateStatus();
        DrawPreview();
    }

    private void DrawPreview()
    {
        try
        {
            NestCanvas.Children.Clear();

            if (_vm == null) return;

            double canvasW = NestCanvas.ActualWidth;
            double canvasH = NestCanvas.ActualHeight;
            if (canvasW < 10 || canvasH < 10) return;

            DrawGrid(canvasW, canvasH);

            if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
            {
                DrawMultiPlateNesting(_vm.NestResult);
            }
            else if (_vm.Parts.Count > 0)
            {
                DrawSinglePlatePreview(_vm.Plate, _vm.Parts);
            }

            DrawBoundingBoxes();
            DrawSelectedPartHighlight();
        }
        catch { }
    }

    private void DrawGrid(double canvasW, double canvasH)
    {
        double gridStepMm = 50;
        double gridScreenStep = gridStepMm * _zoom;
        if (gridScreenStep < 15) return;

        var gridBrush = new SolidColorBrush(Color.FromArgb(20, 0x56, 0x9C, 0xD6));
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            double startX = _panX % gridScreenStep;
            for (double x = startX; x < canvasW; x += gridScreenStep)
            {
                ctx.BeginFigure(new Point(x, 0), false, false);
                ctx.LineTo(new Point(x, canvasH), true, false);
            }

            double startY = _panY % gridScreenStep;
            for (double y = startY; y < canvasH; y += gridScreenStep)
            {
                ctx.BeginFigure(new Point(0, y), false, false);
                ctx.LineTo(new Point(canvasW, y), true, false);
            }
        }

        var gridPath = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = gridBrush,
            StrokeThickness = 0.5,
            IsHitTestVisible = false
        };
        NestCanvas.Children.Add(gridPath);
    }

    private void DrawSinglePlatePreview(PlateModel plate, ObservableCollection<PartModel> parts)
    {
        DrawPlateShape(plate, "Plaka");
        DrawPartsOnPlate(parts, plate);
    }

    private void DrawMultiPlateNesting(NestResult result)
    {
        if (result.Plates.Count == 0) return;

        double gapMm = 20;
        double currentWorldX = 0;

        for (int i = 0; i < result.Plates.Count; i++)
        {
            var plate = result.Plates[i];

            DrawPlateShape(plate, $"Plaka {i + 1}", currentWorldX);

            var platePlacements = result.Placed.Where(p => p.PlateIndex == i).ToList();
            var colors = GetColorPalette();

            for (int j = 0; j < platePlacements.Count; j++)
            {
                var placement = platePlacements[j];
                if (placement.TransformedGeometry.Vertices.Count < 2) continue;

                var color = colors[j % colors.Count];
                var fillColor = Color.FromArgb(80, color.R, color.G, color.B);

                DrawWorldPolygon(placement.TransformedGeometry.Vertices, plate.Height, currentWorldX,
                    fillColor, new SolidColorBrush(color), 1);

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
                Canvas.SetLeft(label, Sx(currentWorldX + cx) - 15);
                Canvas.SetTop(label, Sy(plate.Height - cy) - 6);
                NestCanvas.Children.Add(label);
            }

            currentWorldX += plate.Width + gapMm;
        }
    }

    private void DrawPlateShape(PlateModel plate, string label, double worldOffsetX = 0)
    {
        double x1 = worldOffsetX;
        double y1 = 0;
        double x2 = worldOffsetX + plate.Width;
        double y2 = plate.Height;

        double sx1 = Sx(x1), sy1 = Sy(y1);
        double sx2 = Sx(x2), sy2 = Sy(y2);
        double screenW = sx2 - sx1;
        double screenH = sy2 - sy1;

        var plateRect = new System.Windows.Shapes.Rectangle
        {
            Width = Math.Abs(screenW),
            Height = Math.Abs(screenH),
            Fill = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Stroke = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),
            StrokeThickness = 2
        };
        Canvas.SetLeft(plateRect, Math.Min(sx1, sx2));
        Canvas.SetTop(plateRect, Math.Min(sy1, sy2));
        NestCanvas.Children.Add(plateRect);

        if (plate.Margin > 0)
        {
            double mx1 = Sx(x1 + plate.Margin);
            double my1 = Sy(y1 - plate.Margin);
            double mx2 = Sx(x2 - plate.Margin);
            double my2 = Sy(y2 + plate.Margin);

            var marginRect = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Abs(mx2 - mx1),
                Height = Math.Abs(my2 - my1),
                Stroke = new SolidColorBrush(Color.FromArgb(40, 0x56, 0x9C, 0xD6)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(marginRect, Math.Min(mx1, mx2));
            Canvas.SetTop(marginRect, Math.Min(my1, my2));
            NestCanvas.Children.Add(marginRect);
        }

        var sizeLabel = new TextBlock
        {
            Text = $"{label} | {plate.Width}x{plate.Height} mm",
            Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),
            FontSize = 11,
            FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(sizeLabel, Sx(x1) + 4);
        Canvas.SetTop(sizeLabel, Sy(y1) - 18);
        NestCanvas.Children.Add(sizeLabel);
    }

    private void DrawPartsOnPlate(ObservableCollection<PartModel> parts, PlateModel plate)
    {
        double worldX = 5;
        double worldY = 5;
        double maxH = 0;
        int colorIndex = 0;
        var colors = GetColorPalette();

        foreach (var part in parts)
        {
            if (part.Geometry.Vertices.Count < 2) continue;

            if (worldX + part.Width > plate.Width - 5)
            {
                worldX = 5;
                worldY += maxH + 8;
                maxH = 0;
                colorIndex = (colorIndex + 1) % colors.Count;
            }

            if (worldY + part.Height > plate.Height - 5) break;

            var color = colors[colorIndex % colors.Count];
            var fillColor = Color.FromArgb(60, color.R, color.G, color.B);

            var screenVertices = new List<Point>();
            foreach (var v in part.Geometry.Vertices)
            {
                screenVertices.Add(new Point(Sx(worldX + v.X), Sy(plate.Height - (worldY + v.Y))));
            }

            DrawScreenPolygon(screenVertices, fillColor, new SolidColorBrush(color), 1);

            if (part.Height > maxH) maxH = part.Height;
            worldX += part.Width + 8;
        }
    }

    private void DrawWorldPolygon(List<Geometry.Point2D> vertices, double plateHeight, double worldOffsetX,
        Color fillColor, Brush strokeBrush, double strokeThickness)
    {
        if (vertices.Count < 2) return;

        var screenVertices = new List<Point>();
        foreach (var v in vertices)
        {
            screenVertices.Add(new Point(Sx(worldOffsetX + v.X), Sy(plateHeight - v.Y)));
        }

        DrawScreenPolygon(screenVertices, fillColor, strokeBrush, strokeThickness);
    }

    private void DrawScreenPolygon(List<Point> screenVertices, Color fillColor, Brush strokeBrush, double strokeThickness)
    {
        if (screenVertices.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(screenVertices[0], true, true);
            for (int i = 1; i < screenVertices.Count; i++)
            {
                ctx.LineTo(screenVertices[i], true, false);
            }
        }

        var path = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Fill = new SolidColorBrush(fillColor),
            Stroke = strokeBrush,
            StrokeThickness = strokeThickness
        };
        NestCanvas.Children.Add(path);
    }

    private void DrawBoundingBoxes()
    {
        if (_vm == null) return;

        var dashArray = new DoubleCollection { 4, 3 };
        var bboxPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xFF, 0x00)), 0.8);
        bboxPen.DashStyle = new DashStyle(dashArray, 0);

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            var plate = _vm.NestResult.Plates.FirstOrDefault();
            if (plate == null) return;
            double plateOffsetX = 0;

            foreach (var p in _vm.NestResult.Placed)
            {
                if (p.TransformedGeometry.Vertices.Count < 2) continue;
                double minX = p.TransformedGeometry.Vertices.Min(v => v.X);
                double maxX = p.TransformedGeometry.Vertices.Max(v => v.X);
                double minY = p.TransformedGeometry.Vertices.Min(v => v.Y);
                double maxY = p.TransformedGeometry.Vertices.Max(v => v.Y);

                DrawScreenBBox(
                    Sx(plateOffsetX + minX), Sy(plate.Height - maxY),
                    Sx(plateOffsetX + maxX), Sy(plate.Height - minY),
                    bboxPen);
            }
        }
        else if (_vm.Parts.Count > 0)
        {
            var plate = _vm.Plate;
            foreach (var part in _vm.Parts)
            {
                var b = part.Geometry.Bounds;
                if (b.Width <= 0 && b.Height <= 0) continue;
                DrawScreenBBox(
                    Sx(b.MinX), Sy(plate.Height - b.MaxY),
                    Sx(b.MaxX), Sy(plate.Height - b.MinY),
                    bboxPen);
            }
        }
    }

    private void DrawScreenBBox(double sx1, double sy1, double sx2, double sy2, Pen pen)
    {
        double left = Math.Min(sx1, sx2);
        double top = Math.Min(sy1, sy2);
        double w = Math.Abs(sx2 - sx1);
        double h = Math.Abs(sy2 - sy1);
        if (w < 1 || h < 1) return;

        var rect = new System.Windows.Shapes.Rectangle
        {
            Width = w,
            Height = h,
            Stroke = pen.Brush,
            StrokeThickness = pen.Thickness,
            StrokeDashArray = pen.DashStyle.Dashes,
            StrokeDashOffset = pen.DashStyle.Offset,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);
        NestCanvas.Children.Add(rect);
    }

    private void DrawSelectedPartHighlight()
    {
        if (_vm?.SelectedPart == null) return;

        var sel = _vm.SelectedPart;
        var highlightBrush = new SolidColorBrush(Color.FromArgb(200, 0xFF, 0x55, 0x55));
        var highlightFill = new SolidColorBrush(Color.FromArgb(40, 0xFF, 0x55, 0x55));

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            var plate = _vm.NestResult.Plates.FirstOrDefault();
            if (plate == null) return;

            foreach (var p in _vm.NestResult.Placed)
            {
                if (p.PartName != sel.Name) continue;
                if (p.TransformedGeometry.Vertices.Count < 2) continue;

                DrawWorldPolygon(p.TransformedGeometry.Vertices, plate.Height, 0,
                    Color.FromArgb(40, 0xFF, 0x55, 0x55), highlightBrush, 2.0);
                return;
            }
        }
        else if (_vm.Parts.Count > 0)
        {
            var plate = _vm.Plate;
            var screenVertices = new List<Point>();
            foreach (var v in sel.Geometry.Vertices)
            {
                screenVertices.Add(new Point(Sx(v.X), Sy(plate.Height - v.Y)));
            }
            DrawScreenPolygon(screenVertices, Color.FromArgb(40, 0xFF, 0x55, 0x55), highlightBrush, 2.0);
        }
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

    private void NestCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if ((_vm?.Parts.Count > 0) == true || (_vm?.NestResult?.Plates.Count > 0) == true)
        {
            FitToScreen();
        }
    }
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
