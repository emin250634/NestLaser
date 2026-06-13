using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NestLaserDesktop.Models;
using NestLaserDesktop.Services;
using NestLaserDesktop.ViewModels;

namespace NestLaserDesktop.Views;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush GridBrush = FreezeBrush(Color.FromArgb(22, 0x5E, 0x6A, 0x73));
    private static readonly SolidColorBrush MajorGridBrush = FreezeBrush(Color.FromArgb(42, 0x3F, 0xA7, 0xD6));
    private static readonly SolidColorBrush TickBrush = FreezeBrush(Color.FromRgb(0x8C, 0x97, 0xA3));
    private static readonly SolidColorBrush RulerTextBrush = FreezeBrush(Color.FromRgb(0xD7, 0xDC, 0xE2));
    private static readonly SolidColorBrush MajorTickBrush = FreezeBrush(Color.FromRgb(0x3F, 0xA7, 0xD6));
    private static readonly SolidColorBrush HighlightBrush = FreezeBrush(Color.FromArgb(230, 0xFF, 0x55, 0x55));
    private static readonly SolidColorBrush HandleBrush = FreezeBrush(Color.FromArgb(220, 0xFF, 0x55, 0x55));
    private static readonly SolidColorBrush HandleStrokeBrush = FreezeBrush(Colors.White);
    private static readonly SolidColorBrush SnapBrush = FreezeBrush(Color.FromRgb(0xFF, 0xD8, 0x4D));
    private static readonly SolidColorBrush HoverBrush = FreezeBrush(Color.FromArgb(220, 0x9C, 0xDC, 0xFE));
    private static readonly SolidColorBrush PlateFillBrush = FreezeBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
    private static readonly SolidColorBrush PlateStrokeBrush = FreezeBrush(Color.FromRgb(0x56, 0x9C, 0xD6));
    private static readonly SolidColorBrush MarginStrokeBrush = FreezeBrush(Color.FromArgb(40, 0x56, 0x9C, 0xD6));
    private static readonly SolidColorBrush PlateLabelBrush = FreezeBrush(Color.FromRgb(0x80, 0x80, 0x80));
    private static readonly SolidColorBrush MeasureLineBrush = FreezeBrush(Color.FromRgb(0xFF, 0xD8, 0x4D));
    private static readonly SolidColorBrush MeasureTextBrush = FreezeBrush(Color.FromRgb(0xFF, 0xD8, 0x4D));
    private static readonly SolidColorBrush SnapLabelBgBrush = FreezeBrush(Color.FromArgb(210, 0x18, 0x1B, 0x1F));
    private static readonly DoubleCollection SnapDash = new() { 5, 3 };
    private static readonly DoubleCollection MarginDash = new() { 4, 2 };
    private static readonly DoubleCollection SelectionDash = new() { 4, 2 };
    private static readonly SolidColorBrush SelectionFillBrush = FreezeBrush(Color.FromArgb(30, 0x56, 0x9C, 0xD6));
    private static readonly SolidColorBrush SelectionStrokeBrush = FreezeBrush(Color.FromArgb(150, 0x56, 0x9C, 0xD6));

    private static SolidColorBrush FreezeBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private MainViewModel? _vm;
    private bool _isPanning;
    private Point _panStart;
    private double _zoom = 1.0;
    private double _panX = 0;
    private double _panY = 0;
    private bool _isDraggingSelection;
    private bool _selectionLeftToRight;
    private Point _dragStart;
    private System.Windows.Shapes.Rectangle? _selectionRect;
    private bool _isDraggingParts;
    private bool _hasMovedParts;
    private Point _lastDragScreen;
    private SnapVisual? _activeSnapVisual;
    private PartModel? _hoveredPart;
    private bool _spacePanMode;
    private bool _syncingPartListSelection;
    private bool _closingAfterAsyncSave;
    private bool _isMeasuring;
    private Geometry.Point2D? _measurePointA;
    private Geometry.Point2D? _measurePointB;
    private bool _dragRedrawSkipped;
    private double _viewportMinX, _viewportMaxX, _viewportMinY, _viewportMaxY;
    private int _renderedPartCount;
    private int _culledPartCount;
    private long _lastRenderTicks;
    private bool _enableRenderDiagnostics = false;

    private double Sx(double worldX) => worldX * _zoom + _panX;
    private double Sy(double worldY) => worldY * _zoom + _panY;
    private double Wx(double screenX) => (screenX - _panX) / _zoom;
    private double Wy(double screenY) => (screenY - _panY) / _zoom;

    private void UpdateViewportBounds()
    {
        _viewportMinX = Wx(0);
        _viewportMaxX = Wx(NestCanvas.ActualWidth);
        _viewportMinY = Wy(NestCanvas.ActualHeight);
        _viewportMaxY = Wy(0);
    }

    private bool IsVisibleInViewport(double minX, double maxX, double minY, double maxY)
    {
        return maxX >= _viewportMinX && minX <= _viewportMaxX &&
               maxY >= _viewportMinY && minY <= _viewportMaxY;
    }

    private bool IsVisibleInViewport(Geometry.BoundingBox bounds)
    {
        return Math.Max(bounds.MinX, 0) <= _viewportMaxX && Math.Min(bounds.MaxX, double.MaxValue) >= _viewportMinX &&
               Math.Max(bounds.MinY, 0) <= _viewportMaxY && Math.Min(bounds.MaxY, double.MaxValue) >= _viewportMinY;
    }

    private sealed class SnapVisual
    {
        public SnapMode Mode { get; init; }
        public Geometry.Point2D Point { get; init; }
        public bool HasVerticalLine { get; init; }
        public bool HasHorizontalLine { get; init; }
        public double LineX { get; init; }
        public double LineY { get; init; }
    }

    private sealed class SnapDelta
    {
        public double Dx { get; init; }
        public double Dy { get; init; }
        public SnapVisual? Visual { get; init; }
    }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closingAfterAsyncSave) return;
        if (_vm == null || !_vm.HasUnsavedChanges) return;

        var result = MessageBox.Show(
            "Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?",
            "Projeyi Kaydet",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
        }
        else if (result == MessageBoxResult.Yes)
        {
            e.Cancel = true;
            if (await _vm.SaveProjectAsync())
            {
                _closingAfterAsyncSave = true;
                Close();
            }
        }
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
                        (args.PropertyName == nameof(MainViewModel.NestResult) && _vm.NestResult != null) ||
                        args.PropertyName == nameof(MainViewModel.PlateWidthText) ||
                        args.PropertyName == nameof(MainViewModel.PlateHeightText) ||
                        args.PropertyName == nameof(MainViewModel.MarginText) ||
                        args.PropertyName == nameof(MainViewModel.GapText))
                    {
                        FitToScreen();
                    }
                    else if (args.PropertyName == nameof(MainViewModel.SelectedParts))
                    {
                        DrawPreview();
                    }

                    if (args.PropertyName == nameof(MainViewModel.SelectedCount))
                        FocusSelectedPartInList();

                    if (args.PropertyName == nameof(MainViewModel.IsLoading))
                        Cursor = _vm.IsLoading ? Cursors.Wait : Cursors.Arrow;
                }
                catch { }
            };

            FitToScreen();
        }
    }

    private void UpdateStatus()
    {
        if (_vm != null)
            _vm.ZoomPercent = $"{(int)(_zoom * 100)}";
    }

    private void UpdateMouseCoordinates(Point pos)
    {
        if (_vm == null) return;

        _vm.MouseXText = $"{Wx(pos.X):F1}";
        _vm.MouseYText = $"{Wy(pos.Y):F1}";
    }

    private void ClearMouseCoordinates()
    {
        if (_vm == null) return;

        _vm.MouseXText = "--";
        _vm.MouseYText = "--";
    }

    private void MeasureTool_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        _isMeasuring = true;
        _measurePointA = null;
        _measurePointB = null;
        _vm.StatusText = "Ölçüm: İlk noktayı seçin (ESC = iptal)";
        SetToolStyle("Ölç");
        NestCanvas.Cursor = Cursors.Cross;
        DrawPreview();
    }

    private void SetToolStyle(string tool)
    {
        if (_vm == null) return;
        _vm.ActiveTool = tool;
        SelectToolButton.Style = tool == "Seç"
            ? (Style)Resources["ActiveToolbarButton"]
            : (Style)Resources["ToolbarButton"];
        MeasureToolButton.Style = tool == "Ölç"
            ? (Style)Resources["ActiveToolbarButton"]
            : (Style)Resources["ToolbarButton"];
    }

    private void UpdateMeasureStatus()
    {
        if (_vm == null || _measurePointA == null || _measurePointB == null) return;

        var a = _measurePointA.Value;
        var b = _measurePointB.Value;
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;

        _vm.StatusText = $"Mesafe: {distance:F2} mm | ΔX: {dx:F2} | ΔY: {-dy:F2} | Açı: {angle:F1}°";
    }

    private void ZoomToSelection()
    {
        if (_vm == null || _vm.SelectedParts.Count == 0) return;

        double canvasW = NestCanvas.ActualWidth;
        double canvasH = NestCanvas.ActualHeight;
        if (canvasW < 10 || canvasH < 10) return;

        var bounds = _vm.GetSelectionBoundsPublic();
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var plate = _vm.Plate;
        double padding = 70;
        double scaleX = (canvasW - 2 * padding) / bounds.Width;
        double scaleY = (canvasH - 2 * padding) / bounds.Height;
        _zoom = Math.Clamp(Math.Min(scaleX, scaleY), 0.05, 100.0);

        double centerX = (bounds.MinX + bounds.MaxX) / 2.0;
        double centerY = plate.Height - ((bounds.MinY + bounds.MaxY) / 2.0);
        _panX = canvasW / 2.0 - centerX * _zoom;
        _panY = canvasH / 2.0 - centerY * _zoom;

        UpdateStatus();
        DrawPreview();
        _vm.StatusText = $"Seçime odaklanıldı ({_vm.SelectedParts.Count} parça)";
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
        if (_vm == null) return;

        if (e.ChangedButton == MouseButton.Middle ||
            (e.ChangedButton == MouseButton.Left && _spacePanMode) ||
            (e.ChangedButton == MouseButton.Left && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)))
        {
            _isPanning = true;
            _panStart = e.GetPosition(NestCanvas);
            NestCanvas.CaptureMouse();
            NestCanvas.Cursor = Cursors.Hand;
        }
        else if (e.ChangedButton == MouseButton.Left)
        {
            var pos = e.GetPosition(NestCanvas);
            double worldX = Wx(pos.X);
            double worldY = Wy(pos.Y);

            if (_isMeasuring)
            {
                if (_measurePointA == null)
                {
                    _measurePointA = new Geometry.Point2D(worldX, worldY);
                    _vm.StatusText = "Ölçüm: 1. nokta seçildi - 2. noktayı seçin";
                    DrawPreview();
                }
                else
                {
                    _measurePointB = new Geometry.Point2D(worldX, worldY);
                    UpdateMeasureStatus();
                    _measurePointA = null;
                    _measurePointB = null;
                    _isMeasuring = false;
                    SetToolStyle("Seç");
                    DrawPreview();
                }
                e.Handled = true;
                return;
            }

            if (e.ClickCount >= 2)
            {
                var doubleHit = _vm.HitTest(worldX, worldY);
                if (doubleHit != null)
                {
                    _vm.SelectPart(doubleHit);
                    ZoomToPart(doubleHit);
                    _vm.StatusText = $"Odaklandı: {doubleHit.Name}";
                    e.Handled = true;
                }
                return;
            }

            var hit = _vm.HitTest(worldX, worldY);

            if (hit != null)
            {
                bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

                if (!ctrl && !shift)
                {
                    if (!_vm.SelectedParts.Contains(hit))
                        _vm.ToggleSelection(hit, false, false);

                    _isDraggingParts = true;
                    _hasMovedParts = false;
                    _lastDragScreen = pos;
                    NestCanvas.CaptureMouse();
                    NestCanvas.Cursor = Cursors.SizeAll;
                }
                else
                {
                    _vm.ToggleSelection(hit, ctrl, shift);
                }
            }
            else
            {
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    _vm.DeselectAll();

                _isDraggingSelection = true;
                _dragStart = pos;
                _selectionLeftToRight = false;
                NestCanvas.CaptureMouse();
            }
        }
    }

    private void NestCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(NestCanvas);
        UpdateMouseCoordinates(pos);

        if (_isMeasuring && _measurePointA != null)
        {
            _measurePointB = new Geometry.Point2D(Wx(pos.X), Wy(pos.Y));
            DrawPreview();
            return;
        }

        if (_isPanning)
        {
            _panX += pos.X - _panStart.X;
            _panY += pos.Y - _panStart.Y;
            _panStart = pos;
            DrawPreview();
        }
        else if (_isDraggingSelection)
        {
            _selectionLeftToRight = pos.X >= _dragStart.X;
            DrawSelectionRect();
        }
        else if (_isDraggingParts)
        {
            MoveDraggedSelection(pos);
        }
        else
        {
            UpdateHover(pos);
        }
    }

    private void NestCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var vm = _vm;
        if (vm == null) return;

        if (_isDraggingParts)
        {
            _isDraggingParts = false;
            NestCanvas.ReleaseMouseCapture();
            NestCanvas.Cursor = Cursors.Arrow;

            if (!_hasMovedParts)
            {
                var pos = e.GetPosition(NestCanvas);
                double worldX = Wx(pos.X);
                double worldY = Wy(pos.Y);
                var hit = vm.HitTest(worldX, worldY);
                if (hit != null)
                    vm.ToggleSelection(hit, false, false);
            }
            else
            {
                vm.StatusText = $"{vm.SelectedCount} parça taşındı";
            }

            _activeSnapVisual = null;
            vm.SnapStatusText = "Snap: --";
            DrawPreview();
        }
        else if (_isPanning)
        {
            _isPanning = false;
            NestCanvas.ReleaseMouseCapture();
            NestCanvas.Cursor = Cursors.Arrow;
        }
        else if (_isDraggingSelection)
        {
            _isDraggingSelection = false;
            NestCanvas.ReleaseMouseCapture();

            var pos = e.GetPosition(NestCanvas);
            double wx1 = Wx(_dragStart.X);
            double wy1 = Wy(_dragStart.Y);
            double wx2 = Wx(pos.X);
            double wy2 = Wy(pos.Y);

            if (Math.Abs(pos.X - _dragStart.X) > 5 || Math.Abs(pos.Y - _dragStart.Y) > 5)
            {
                bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                if (_selectionLeftToRight)
                {
                    var partsFullyInside = vm.HitTestRectFullyInside(wx1, wy1, wx2, wy2);
                    vm.SelectByRect(partsFullyInside, ctrl);
                }
                else
                {
                    var partsInRect = vm.HitTestRect(wx1, wy1, wx2, wy2);
                    vm.SelectByRect(partsInRect, ctrl);
                }
            }

            RemoveSelectionRect();
            DrawPreview();
        }
    }

    private void NestCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        ClearMouseCoordinates();
        if (_hoveredPart != null)
        {
            _hoveredPart = null;
            DrawPreview();
        }
    }

    private void UpdateHover(Point pos)
    {
        if (_vm == null) return;

        var hit = _vm.HitTest(Wx(pos.X), Wy(pos.Y));
        if (!ReferenceEquals(hit, _hoveredPart))
        {
            _hoveredPart = hit;
            DrawPreview();
        }
    }

    private void MoveDraggedSelection(Point currentScreen)
    {
        if (_vm == null || _vm.SelectedParts.Count == 0) return;

        double dx = Wx(currentScreen.X) - Wx(_lastDragScreen.X);
        double dy = -(Wy(currentScreen.Y) - Wy(_lastDragScreen.Y));

        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9) return;

        if (!_hasMovedParts)
        {
            _vm.BeginMoveSelected();
            _hasMovedParts = true;
        }

        var snap = ResolveSnap(dx, dy);
        double finalDx = dx + snap.Dx;
        double finalDy = dy + snap.Dy;

        if (Math.Abs(finalDx) > 1e-9 || Math.Abs(finalDy) > 1e-9)
            _vm.MoveSelected(finalDx, finalDy, false);

        _activeSnapVisual = snap.Visual;
        _vm.SnapStatusText = snap.Visual != null ? $"Snap: {snap.Visual.Mode}" : "Snap: --";
        _lastDragScreen = currentScreen;
        DrawPreview();
    }

    private SnapDelta ResolveSnap(double baseDx, double baseDy)
    {
        if (_vm == null) return new SnapDelta();

        double dx = 0;
        double dy = 0;
        SnapVisual? visual = null;

        if (_vm.SnapToGrid)
        {
            var grid = ResolveGridSnap(baseDx, baseDy);
            dx += grid.Dx;
            dy += grid.Dy;
            visual = grid.Visual;
        }

        var precise = ResolvePreciseSnap(baseDx + dx, baseDy + dy);
        if (precise.Visual != null)
        {
            dx += precise.Dx;
            dy += precise.Dy;
            visual = precise.Visual;
        }

        return new SnapDelta { Dx = dx, Dy = dy, Visual = visual };
    }

    private SnapDelta ResolveGridSnap(double baseDx, double baseDy)
    {
        if (_vm == null || _vm.SelectedParts.Count == 0 || _vm.GridStepMm <= 0) return new SnapDelta();

        var anchor = _vm.SelectedParts[0].Geometry.Bounds;
        double targetX = Math.Round((anchor.MinX + baseDx) / _vm.GridStepMm) * _vm.GridStepMm;
        double targetY = Math.Round((anchor.MinY + baseDy) / _vm.GridStepMm) * _vm.GridStepMm;
        double dx = targetX - (anchor.MinX + baseDx);
        double dy = targetY - (anchor.MinY + baseDy);

        return new SnapDelta
        {
            Dx = dx,
            Dy = dy,
            Visual = new SnapVisual
            {
                Mode = SnapMode.Grid,
                Point = new Geometry.Point2D(targetX, targetY),
                HasVerticalLine = true,
                HasHorizontalLine = true,
                LineX = targetX,
                LineY = targetY
            }
        };
    }

    private SnapDelta ResolvePreciseSnap(double baseDx, double baseDy)
    {
        if (_vm == null) return new SnapDelta();

        double toleranceMm = 5.0 / Math.Max(_zoom, 0.0001);
        var selected = _vm.SelectedParts.ToHashSet();
        var targets = _vm.Parts.Where(p => !selected.Contains(p) && _vm.IsPartVisible(p)).ToList();
        if (targets.Count == 0) return new SnapDelta();

        if (_vm.SnapToVertex)
        {
            var vertexSnap = ResolveVertexSnap(baseDx, baseDy, targets, toleranceMm);
            if (vertexSnap.Visual != null) return vertexSnap;
        }

        if (_vm.SnapToEdge)
        {
            var edgeSnap = ResolveEdgeSnap(baseDx, baseDy, targets, toleranceMm);
            if (edgeSnap.Visual != null) return edgeSnap;
        }

        if (_vm.SnapToCenter)
        {
            var centerSnap = ResolveCenterSnap(baseDx, baseDy, targets, toleranceMm);
            if (centerSnap.Visual != null) return centerSnap;
        }

        return new SnapDelta();
    }

    private SnapDelta ResolveVertexSnap(double baseDx, double baseDy, List<PartModel> targets, double toleranceMm)
    {
        if (_vm == null) return new SnapDelta();

        double bestDistance = double.MaxValue;
        Geometry.Point2D bestSource = default;
        Geometry.Point2D bestTarget = default;
        bool found = false;

        foreach (var selectedPart in _vm.SelectedParts)
        {
            foreach (var sourceVertex in selectedPart.Geometry.Vertices)
            {
                var moved = new Geometry.Point2D(sourceVertex.X + baseDx, sourceVertex.Y + baseDy);
                foreach (var target in targets)
                {
                    foreach (var targetVertex in target.Geometry.Vertices)
                    {
                        double distance = Distance(moved, targetVertex);
                        if (distance <= toleranceMm && distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestSource = moved;
                            bestTarget = targetVertex;
                            found = true;
                        }
                    }
                }
            }
        }

        if (!found) return new SnapDelta();

        return new SnapDelta
        {
            Dx = bestTarget.X - bestSource.X,
            Dy = bestTarget.Y - bestSource.Y,
            Visual = new SnapVisual
            {
                Mode = SnapMode.Vertex,
                Point = bestTarget,
                HasVerticalLine = true,
                HasHorizontalLine = true,
                LineX = bestTarget.X,
                LineY = bestTarget.Y
            }
        };
    }

    private SnapDelta ResolveEdgeSnap(double baseDx, double baseDy, List<PartModel> targets, double toleranceMm)
    {
        if (_vm == null) return new SnapDelta();

        double bestDistance = double.MaxValue;
        double bestDeltaX = 0;
        double bestDeltaY = 0;
        bool vertical = false;
        bool horizontal = false;
        double lineX = 0;
        double lineY = 0;
        var point = new Geometry.Point2D();

        foreach (var selectedPart in _vm.SelectedParts)
        {
            var b = selectedPart.Geometry.Bounds;
            var movedX = new[] { b.MinX + baseDx, b.MaxX + baseDx };
            var movedY = new[] { b.MinY + baseDy, b.MaxY + baseDy };

            foreach (var target in targets)
            {
                var tb = target.Geometry.Bounds;
                var targetX = new[] { tb.MinX, tb.MaxX };
                var targetY = new[] { tb.MinY, tb.MaxY };

                foreach (double sx in movedX)
                {
                    foreach (double tx in targetX)
                    {
                        double dist = Math.Abs(tx - sx);
                        if (dist <= toleranceMm && dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestDeltaX = tx - sx;
                            bestDeltaY = 0;
                            vertical = true;
                            horizontal = false;
                            lineX = tx;
                            lineY = 0;
                            point = new Geometry.Point2D(tx, (tb.MinY + tb.MaxY) / 2.0);
                        }
                    }
                }

                foreach (double sy in movedY)
                {
                    foreach (double ty in targetY)
                    {
                        double dist = Math.Abs(ty - sy);
                        if (dist <= toleranceMm && dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestDeltaX = 0;
                            bestDeltaY = ty - sy;
                            vertical = false;
                            horizontal = true;
                            lineX = 0;
                            lineY = ty;
                            point = new Geometry.Point2D((tb.MinX + tb.MaxX) / 2.0, ty);
                        }
                    }
                }
            }
        }

        if (bestDistance == double.MaxValue) return new SnapDelta();

        return new SnapDelta
        {
            Dx = bestDeltaX,
            Dy = bestDeltaY,
            Visual = new SnapVisual
            {
                Mode = SnapMode.Edge,
                Point = point,
                HasVerticalLine = vertical,
                HasHorizontalLine = horizontal,
                LineX = lineX,
                LineY = lineY
            }
        };
    }

    private SnapDelta ResolveCenterSnap(double baseDx, double baseDy, List<PartModel> targets, double toleranceMm)
    {
        if (_vm == null) return new SnapDelta();

        double bestDistance = double.MaxValue;
        double bestDeltaX = 0;
        double bestDeltaY = 0;
        bool vertical = false;
        bool horizontal = false;
        double lineX = 0;
        double lineY = 0;
        var point = new Geometry.Point2D();

        foreach (var selectedPart in _vm.SelectedParts)
        {
            var center = selectedPart.Geometry.Bounds.Center;
            double movedCenterX = center.X + baseDx;
            double movedCenterY = center.Y + baseDy;

            foreach (var target in targets)
            {
                var targetCenter = target.Geometry.Bounds.Center;
                double dx = targetCenter.X - movedCenterX;
                double dy = targetCenter.Y - movedCenterY;

                if (Math.Abs(dx) <= toleranceMm && Math.Abs(dx) < bestDistance)
                {
                    bestDistance = Math.Abs(dx);
                    bestDeltaX = dx;
                    bestDeltaY = 0;
                    vertical = true;
                    horizontal = false;
                    lineX = targetCenter.X;
                    lineY = 0;
                    point = new Geometry.Point2D(targetCenter.X, movedCenterY);
                }

                if (Math.Abs(dy) <= toleranceMm && Math.Abs(dy) < bestDistance)
                {
                    bestDistance = Math.Abs(dy);
                    bestDeltaX = 0;
                    bestDeltaY = dy;
                    vertical = false;
                    horizontal = true;
                    lineX = 0;
                    lineY = targetCenter.Y;
                    point = new Geometry.Point2D(movedCenterX, targetCenter.Y);
                }
            }
        }

        if (bestDistance == double.MaxValue) return new SnapDelta();

        return new SnapDelta
        {
            Dx = bestDeltaX,
            Dy = bestDeltaY,
            Visual = new SnapVisual
            {
                Mode = SnapMode.Center,
                Point = point,
                HasVerticalLine = vertical,
                HasHorizontalLine = horizontal,
                LineX = lineX,
                LineY = lineY
            }
        };
    }

    private static double Distance(Geometry.Point2D a, Geometry.Point2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        if (e.Key == Key.Escape)
        {
            if (_isMeasuring)
            {
                _isMeasuring = false;
                _measurePointA = null;
                _measurePointB = null;
                _vm.StatusText = "Ölçüm iptal edildi";
                SetToolStyle("Seç");
                DrawPreview();
                e.Handled = true;
                return;
            }
        }
        else if (e.Key == Key.Delete && _vm.HasSelectedParts)
        {
            _vm.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F)
        {
            FitToScreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (_vm.SelectedParts.Count > 0)
                ZoomToSelection();
            else
                FitToScreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            _spacePanMode = true;
            _vm.ActiveTool = "Pan";
            NestCanvas.Cursor = Cursors.Hand;
            e.Handled = true;
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        if (e.Key == Key.Space)
        {
            _spacePanMode = false;
            _vm.ActiveTool = "Seç";
            if (!_isPanning)
                NestCanvas.Cursor = Cursors.Arrow;
            e.Handled = true;
        }
    }

    private void PartListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm == null || PartListBox.SelectedItem is not PartModel part) return;

        _vm.SelectPart(part);
        ZoomToPart(part);
        _vm.StatusText = $"Odaklandı: {part.Name}";
    }

    private void PartListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPartListSelection || _vm == null || PartListBox.SelectedItem is not PartModel part) return;
        _vm.SelectPart(part);
    }

    private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null || sender is not ListBox listBox || listBox.SelectedItem is not LayerModel layer)
            return;

        if (!ReferenceEquals(_vm.SelectedLayer, layer))
            _vm.SelectedLayer = layer;
    }

    private void FocusSelectedPartInList()
    {
        if (_vm == null || _vm.SelectedParts.Count == 0) return;

        var part = _vm.SelectedParts[0];
        if (!PartListBox.Items.Contains(part)) return;

        _syncingPartListSelection = true;
        try
        {
            PartListBox.SelectedItem = part;
            PartListBox.ScrollIntoView(part);
        }
        finally
        {
            _syncingPartListSelection = false;
        }
    }

    private void DrawSelectionRect()
    {
        RemoveSelectionRect();

        var pos = Mouse.GetPosition(NestCanvas);
        double left = Math.Min(_dragStart.X, pos.X);
        double top = Math.Min(_dragStart.Y, pos.Y);
        double width = Math.Abs(pos.X - _dragStart.X);
        double height = Math.Abs(pos.Y - _dragStart.Y);

        if (width < 2 && height < 2) return;

        _selectionRect = new System.Windows.Shapes.Rectangle
        {
            Width = width,
            Height = height,
            Fill = SelectionFillBrush,
            Stroke = SelectionStrokeBrush,
            StrokeThickness = 1,
            StrokeDashArray = SelectionDash,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_selectionRect, left);
        Canvas.SetTop(_selectionRect, top);
        NestCanvas.Children.Add(_selectionRect);
    }

    private void RemoveSelectionRect()
    {
        if (_selectionRect != null)
        {
            NestCanvas.Children.Remove(_selectionRect);
            _selectionRect = null;
        }
    }

    private void FitToScreen_Click(object sender, RoutedEventArgs e) => FitToScreen();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            Services.AppVersion.GetAboutText(),
            "Hakkında - NestLaser Desktop",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Grid1_Click(object sender, RoutedEventArgs e) => SetGridStep(1);
    private void Grid5_Click(object sender, RoutedEventArgs e) => SetGridStep(5);
    private void Grid10_Click(object sender, RoutedEventArgs e) => SetGridStep(10);
    private void Grid50_Click(object sender, RoutedEventArgs e) => SetGridStep(50);

    private void SetGridStep(double stepMm)
    {
        if (_vm == null) return;

        _vm.GridStepMm = stepMm;
        DrawPreview();
    }

    private void GridStep_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null || sender is not ComboBox combo || combo.SelectedItem is not ComboBoxItem item) return;
        if (double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double step))
            SetGridStep(step);
    }

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

        var content = GetPreviewContentBounds();
        double contentMinX = content.minX;
        double contentMinY = content.minY;
        double contentMaxX = content.maxX;
        double contentMaxY = content.maxY;

        double contentW = Math.Max(1, contentMaxX - contentMinX);
        double contentH = Math.Max(1, contentMaxY - contentMinY);
        double padding = 50;
        double scaleX = (canvasW - 2 * padding) / contentW;
        double scaleY = (canvasH - 2 * padding) / contentH;
        _zoom = Math.Min(scaleX, scaleY);
        _zoom = Math.Clamp(_zoom, 0.01, 100.0);

        _panX = (canvasW - contentW * _zoom) / 2 - contentMinX * _zoom;
        _panY = (canvasH - contentH * _zoom) / 2 - contentMinY * _zoom;

        UpdateStatus();
        DrawPreview();
    }

    private (double minX, double minY, double maxX, double maxY) GetPreviewContentBounds()
    {
        if (_vm == null)
            return (0, 0, 1000, 1000);

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        // Imported parts (visible, non-reference)
        foreach (var part in _vm.Parts)
        {
            if (!_vm.IsPartVisible(part) || part.Geometry.Vertices.Count < 2)
                continue;
            var layer = _vm.GetLayerForPart(part);
            if (layer?.Type == LayerType.Reference)
                continue;

            var b = part.Geometry.Bounds;
            minX = Math.Min(minX, b.MinX);
            maxX = Math.Max(maxX, b.MaxX);
            double displayMinY = _vm.Plate.Height - b.MaxY;
            double displayMaxY = _vm.Plate.Height - b.MinY;
            minY = Math.Min(minY, displayMinY);
            maxY = Math.Max(maxY, displayMaxY);
        }

        // NestResult: placed + unplaced parts + plates
        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            double currentWorldX = 0;
            double gapMm = 20;
            for (int i = 0; i < _vm.NestResult.Plates.Count; i++)
            {
                var plate = _vm.NestResult.Plates[i];
                double px1 = currentWorldX;
                double py1 = 0;
                double px2 = currentWorldX + plate.Width;
                double py2 = plate.Height;
                minX = Math.Min(minX, px1);
                minY = Math.Min(minY, py1);
                maxX = Math.Max(maxX, px2);
                maxY = Math.Max(maxY, py2);

                var placements = _vm.NestResult.Placed
                    .Where(p => p.PlateIndex == i)
                    .ToList();
                foreach (var placement in placements)
                {
                    var b = placement.TransformedGeometry.Bounds;
                    double worldBx1 = currentWorldX + b.MinX;
                    double worldBx2 = currentWorldX + b.MaxX;
                    double worldBy1 = plate.Height - b.MaxY;
                    double worldBy2 = plate.Height - b.MinY;
                    minX = Math.Min(minX, worldBx1);
                    maxX = Math.Max(maxX, worldBx2);
                    minY = Math.Min(minY, worldBy1);
                    maxY = Math.Max(maxY, worldBy2);
                }

                currentWorldX += plate.Width + gapMm;
            }

            // Unplaced parts
            if (_vm.NestResult.Unplaced.Count > 0)
            {
                double upX = currentWorldX + gapMm;
                double upY = 0;
                foreach (var up in _vm.NestResult.Unplaced)
                {
                    if (up.Geometry.Vertices.Count < 2) continue;
                    var b = up.Geometry.Bounds;
                    minX = Math.Min(minX, upX + b.MinX);
                    maxX = Math.Max(maxX, upX + b.MaxX);
                    minY = Math.Min(minY, upY);
                    maxY = Math.Max(maxY, upY + b.Height);
                    upY += b.Height + 10;
                }
            }
        }

        if (minX == double.MaxValue) minX = 0;
        if (minY == double.MaxValue) minY = 0;
        if (maxX == double.MinValue) maxX = Math.Max(1, _vm.Plate.Width);
        if (maxY == double.MinValue) maxY = Math.Max(1, _vm.Plate.Height);

        return (minX, minY, maxX, maxY);
    }

    private void ZoomToPart(PartModel part)
    {
        if (_vm == null) return;

        double canvasW = NestCanvas.ActualWidth;
        double canvasH = NestCanvas.ActualHeight;
        if (canvasW < 10 || canvasH < 10) return;

        var b = part.Geometry.Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;

        double padding = 90;
        double scaleX = (canvasW - 2 * padding) / b.Width;
        double scaleY = (canvasH - 2 * padding) / b.Height;
        _zoom = Math.Clamp(Math.Min(scaleX, scaleY), 0.05, 100.0);

        double centerX = (b.MinX + b.MaxX) / 2.0;
        double centerY = _vm.Plate.Height - ((b.MinY + b.MaxY) / 2.0);
        _panX = canvasW / 2.0 - centerX * _zoom;
        _panY = canvasH / 2.0 - centerY * _zoom;

        UpdateStatus();
        DrawPreview();
    }

    private void DrawPreview()
    {
        if (_vm == null) return;

        double canvasW = NestCanvas.ActualWidth;
        double canvasH = NestCanvas.ActualHeight;
        if (canvasW < 10 || canvasH < 10) return;

        if (_isDraggingParts && _dragRedrawSkipped)
        {
            _dragRedrawSkipped = false;
            return;
        }

        long startTicks = _enableRenderDiagnostics ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        _renderedPartCount = 0;
        _culledPartCount = 0;

        try
        {
            UpdateViewportBounds();
            NestCanvas.Children.Clear();

            DrawGrid(canvasW, canvasH);
            DrawRulers();

            if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
            {
                DrawMultiPlateNesting(_vm.NestResult);
            }
            else if (_vm.Parts.Count > 0)
            {
                DrawSinglePlatePreview(_vm.Plate, _vm.Parts);
            }

            if (_vm.OperationViewMode && _vm.SelectedOperation != null)
                DrawOperationPreview();

            if (_isMeasuring)
                DrawMeasurementOverlay();

            DrawHoverPartHighlight();
            DrawSelectedPartHighlights();
            DrawSnapVisual();

            if (_enableRenderDiagnostics)
            {
                long endTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                _lastRenderTicks = (long)((endTicks - startTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            }
        }
        catch (Exception ex)
        {
            Services.AppLogger.LogError(ex, "DrawPreview render error");
            _vm.StatusText = $"Render hatası: {ex.Message}";
        }
    }

    private void DrawOperationPreview()
    {
        if (_vm == null || _vm.SelectedOperation == null) return;

        var op = _vm.SelectedOperation;
        var layer = _vm.Layers.FirstOrDefault(l => l.Id == op.LayerId);
        Color opColor = layer != null && !string.IsNullOrWhiteSpace(layer.Color)
            ? (ColorConverter.ConvertFromString(layer.Color) as Color? ?? Color.FromRgb(0x56, 0x9C, 0xD6))
            : Color.FromRgb(0x56, 0x9C, 0xD6);

        var strokeBrush = new SolidColorBrush(opColor);
        var fillColor = Color.FromArgb(60, opColor.R, opColor.G, opColor.B);

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            double gapMm = 20;
            double currentWorldX = 0;

            for (int i = 0; i < _vm.NestResult.Plates.Count; i++)
            {
                var plate = _vm.NestResult.Plates[i];
                var platePlacements = _vm.NestResult.Placed.Where(p => p.PlateIndex == i).ToList();

                foreach (var placement in platePlacements)
                {
                    if (placement.TransformedGeometry.Vertices.Count < 2) continue;
                    var partLayer = _vm.GetLayerForPart(placement.Part);
                    if (partLayer == null || partLayer.Id != op.LayerId) continue;

                    var partOpColor = Color.FromArgb(180, opColor.R, opColor.G, opColor.B);
                    var partFillColor = Color.FromArgb(80, opColor.R, opColor.G, opColor.B);

                    DrawWorldPolygon(placement.TransformedGeometry.Vertices, plate.Height, currentWorldX,
                        partFillColor, new SolidColorBrush(partOpColor), 2.5);
                }

                currentWorldX += plate.Width + gapMm;
            }
        }
        else if (_vm.Parts.Count > 0)
        {
            var plate = _vm.Plate;
            foreach (var part in _vm.Parts)
            {
                if (!_vm.IsPartVisible(part)) continue;
                var partLayer = _vm.GetLayerForPart(part);
                if (partLayer == null || partLayer.Id != op.LayerId) continue;
                if (part.Geometry.Vertices.Count < 2) continue;

                var screenVertices = new List<Point>();
                foreach (var v in part.Geometry.Vertices)
                    screenVertices.Add(new Point(Sx(v.X), Sy(plate.Height - v.Y)));

                DrawScreenPolygon(screenVertices, fillColor, strokeBrush, 2.5);
            }
        }
    }

    private void DrawGrid(double canvasW, double canvasH)
    {
        if (_vm == null || !_vm.ShowGrid) return;

        double gridStepMm = GetEffectiveStep(_vm.GridStepMm, 12);
        double gridScreenStep = gridStepMm * _zoom;

        var geometry = new StreamGeometry();
        var majorGeometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        using (var majorCtx = majorGeometry.Open())
        {
            double startX = _panX % gridScreenStep;
            int indexX = (int)Math.Floor((0 - _panX) / gridScreenStep);
            for (double x = startX; x < canvasW; x += gridScreenStep)
            {
                bool major = Math.Abs(indexX % 5) == 0;
                var target = major ? majorCtx : ctx;
                target.BeginFigure(new Point(x, 0), false, false);
                target.LineTo(new Point(x, canvasH), true, false);
                indexX++;
            }

            double startY = _panY % gridScreenStep;
            int indexY = (int)Math.Floor((0 - _panY) / gridScreenStep);
            for (double y = startY; y < canvasH; y += gridScreenStep)
            {
                bool major = Math.Abs(indexY % 5) == 0;
                var target = major ? majorCtx : ctx;
                target.BeginFigure(new Point(0, y), false, false);
                target.LineTo(new Point(canvasW, y), true, false);
                indexY++;
            }
        }

        var gridPath = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = GridBrush,
            StrokeThickness = 0.5,
            IsHitTestVisible = false
        };
        NestCanvas.Children.Add(gridPath);

        var majorPath = new System.Windows.Shapes.Path
        {
            Data = majorGeometry,
            Stroke = MajorGridBrush,
            StrokeThickness = 0.8,
            IsHitTestVisible = false
        };
        NestCanvas.Children.Add(majorPath);
    }

    private double GetEffectiveStep(double requestedStepMm, double minPixels)
    {
        double step = Math.Max(1, requestedStepMm);
        while (step * _zoom < minPixels)
        {
            step = step switch
            {
                < 5 => 5,
                < 10 => 10,
                < 50 => 50,
                _ => step * 2
            };
        }

        return step;
    }

    private void DrawRulers()
    {
        HorizontalRulerCanvas.Children.Clear();
        VerticalRulerCanvas.Children.Clear();

        if (_vm == null || !_vm.ShowRulers) return;

        double width = HorizontalRulerCanvas.ActualWidth;
        double height = VerticalRulerCanvas.ActualHeight;
        if (width < 10 || height < 10) return;

        double stepMm = GetEffectiveStep(_vm.GridStepMm, 45);
        double stepPx = stepMm * _zoom;

        double startWorldX = Math.Floor(Wx(0) / stepMm) * stepMm;
        for (double worldX = startWorldX; Sx(worldX) < width; worldX += stepMm)
        {
            double x = Sx(worldX);
            if (x < 0) continue;

            bool major = IsMajorTick(worldX, stepMm);
            var line = new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = major ? 4 : 12,
                X2 = x,
                Y2 = 24,
                Stroke = major ? MajorTickBrush : TickBrush,
                StrokeThickness = major ? 1.0 : 0.7,
                IsHitTestVisible = false
            };
            HorizontalRulerCanvas.Children.Add(line);

            if (major && stepPx >= 30)
            {
                var label = new TextBlock
                {
                    Text = $"{worldX:F0}",
                    Foreground = RulerTextBrush,
                    FontSize = 9,
                    FontFamily = new FontFamily("Consolas"),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(label, x + 3);
                Canvas.SetTop(label, 2);
                HorizontalRulerCanvas.Children.Add(label);
            }
        }

        double startWorldY = Math.Floor(Wy(0) / stepMm) * stepMm;
        for (double worldY = startWorldY; Sy(worldY) < height; worldY += stepMm)
        {
            double y = Sy(worldY);
            if (y < 0) continue;

            bool major = IsMajorTick(worldY, stepMm);
            var line = new System.Windows.Shapes.Line
            {
                X1 = major ? 4 : 22,
                Y1 = y,
                X2 = 42,
                Y2 = y,
                Stroke = major ? MajorTickBrush : TickBrush,
                StrokeThickness = major ? 1.0 : 0.7,
                IsHitTestVisible = false
            };
            VerticalRulerCanvas.Children.Add(line);

            if (major && stepPx >= 30)
            {
                var label = new TextBlock
                {
                    Text = $"{worldY:F0}",
                    Foreground = RulerTextBrush,
                    FontSize = 9,
                    FontFamily = new FontFamily("Consolas"),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(label, 4);
                Canvas.SetTop(label, y + 2);
                VerticalRulerCanvas.Children.Add(label);
            }
        }
    }

    private static bool IsMajorTick(double value, double step)
    {
        double major = step * 5;
        if (major <= 0) return false;
        return Math.Abs(value / major - Math.Round(value / major)) < 1e-6;
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

            var platePlacements = result.Placed.Where(p => p.PlateIndex == i).ToList();
            double plateUsedArea = platePlacements.Sum(p => p.Width * p.Height);
            double plateEfficiency = plate.TotalArea > 0 ? (plateUsedArea / plate.TotalArea) * 100 : 0;

            DrawPlateShape(plate, $"Plaka {i + 1}  |  Verimlilik %{plateEfficiency:F1}", currentWorldX);

            var greenShades = GetGreenPalette();

            for (int j = 0; j < platePlacements.Count; j++)
            {
                var placement = platePlacements[j];
                if (placement.TransformedGeometry.Vertices.Count < 2) continue;

                var b = placement.TransformedGeometry.Bounds;
                double screenX1 = currentWorldX + b.MinX;
                double screenX2 = currentWorldX + b.MaxX;
                double screenY1 = plate.Height - b.MaxY;
                double screenY2 = plate.Height - b.MinY;

                if (!IsVisibleInViewport(screenX1, screenX2, screenY1, screenY2))
                {
                    _culledPartCount++;
                    continue;
                }

                _renderedPartCount++;
                var color = GetPartLayerColor(placement.Part, greenShades[j % greenShades.Count]);
                var fillColor = Color.FromArgb(70, color.R, color.G, color.B);

                DrawWorldPolygon(placement.TransformedGeometry.Vertices, plate.Height, currentWorldX,
                    fillColor, new SolidColorBrush(color), 1);

                double cx = 0, cy = 0;
                foreach (var v in placement.TransformedGeometry.Vertices) { cx += v.X; cy += v.Y; }
                cx /= placement.TransformedGeometry.Vertices.Count;
                cy /= placement.TransformedGeometry.Vertices.Count;

                if (_zoom > 0.5 && _vm != null)
                {
                    var labels = new List<string>();
                    if (_vm.ShowPartNames) labels.Add(placement.PartName ?? "");
                    if (_vm.ShowLayerNames && !string.IsNullOrWhiteSpace(placement.Part?.LayerName)) labels.Add($"[{placement.Part.LayerName}]");
                    string labelText = string.Join(" ", labels);

                    if (!string.IsNullOrWhiteSpace(labelText))
                    {
                        var label = new TextBlock
                        {
                            Text = labelText,
                            Foreground = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B)),
                            FontSize = 9,
                            FontFamily = new FontFamily("Consolas"),
                            Opacity = 0.75
                        };
                        Canvas.SetLeft(label, Sx(currentWorldX + cx) - 15);
                        Canvas.SetTop(label, Sy(plate.Height - cy) - 6);
                        NestCanvas.Children.Add(label);
                    }
                }
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
            Fill = PlateFillBrush,
            Stroke = PlateStrokeBrush,
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
                Stroke = MarginStrokeBrush,
                StrokeThickness = 1,
                StrokeDashArray = MarginDash,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(marginRect, Math.Min(mx1, mx2));
            Canvas.SetTop(marginRect, Math.Min(my1, my2));
            NestCanvas.Children.Add(marginRect);
        }

        var sizeLabel = new TextBlock
        {
            Text = $"{label} | {plate.Width}x{plate.Height} mm",
            Foreground = PlateLabelBrush,
            FontSize = 11,
            FontFamily = new FontFamily("Consolas")
        };
        Canvas.SetLeft(sizeLabel, Sx(x1) + 4);
        Canvas.SetTop(sizeLabel, Sy(y1) - 18);
        NestCanvas.Children.Add(sizeLabel);
    }

    private void DrawPartsOnPlate(ObservableCollection<PartModel> parts, PlateModel plate)
    {
        int colorIndex = 0;
        var colors = GetColorPalette();

        foreach (var part in parts)
        {
            if (_vm != null && !_vm.IsPartVisible(part)) continue;
            if (part.Geometry.Vertices.Count < 2) continue;

            var bounds = part.Geometry.Bounds;
            double screenY1 = plate.Height - bounds.MaxY;
            double screenY2 = plate.Height - bounds.MinY;

            if (!IsVisibleInViewport(bounds.MinX, bounds.MaxX, screenY1, screenY2))
            {
                _culledPartCount++;
                continue;
            }

            _renderedPartCount++;
            var color = GetPartLayerColor(part, colors[colorIndex % colors.Count]);
            var fillColor = Color.FromArgb(42, color.R, color.G, color.B);

            var screenVertices = new List<Point>();
            foreach (var v in part.Geometry.Vertices)
            {
                screenVertices.Add(new Point(Sx(v.X), Sy(plate.Height - v.Y)));
            }

            DrawScreenPolygon(screenVertices, fillColor, new SolidColorBrush(color), 1);
            colorIndex++;
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

    private Color GetPartLayerColor(PartModel part, Color fallback)
    {
        if (_vm == null) return fallback;
        var layer = _vm.GetLayerForPart(part);
        if (layer == null || string.IsNullOrWhiteSpace(layer.Color)) return fallback;

        try
        {
            var converted = ColorConverter.ConvertFromString(layer.Color);
            return converted is Color color ? color : fallback;
        }
        catch
        {
            return fallback;
        }
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
                if (!_vm.IsPartVisible(p.Part)) continue;
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
                if (!_vm.IsPartVisible(part)) continue;
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

	private void DrawSelectedPartHighlights()
	{
		if (_vm == null || _vm.SelectedParts.Count == 0) return;

		var highlightPen = new Pen(HighlightBrush, 2.0);

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            var plate = _vm.NestResult.Plates.FirstOrDefault();
            if (plate == null) return;

            foreach (var sel in _vm.SelectedParts)
            {
                if (!_vm.IsPartVisible(sel)) continue;
                foreach (var p in _vm.NestResult.Placed)
                {
                    if (p.PartName != sel.Name) continue;
                    if (p.TransformedGeometry.Vertices.Count < 2) continue;

                    DrawVertexBounds(p.TransformedGeometry.Vertices, plate.Height, 0, highlightPen);
                    DrawCornerHandles(p.TransformedGeometry.Vertices, plate.Height, 0);
                    break;
                }
            }
        }
        else if (_vm.Parts.Count > 0)
        {
            var plate = _vm.Plate;
            foreach (var sel in _vm.SelectedParts)
            {
                if (!_vm.IsPartVisible(sel)) continue;
                var screenVertices = new List<Point>();
                foreach (var v in sel.Geometry.Vertices)
                {
                    screenVertices.Add(new Point(Sx(v.X), Sy(plate.Height - v.Y)));
                }

                DrawScreenPolyline(screenVertices, HighlightBrush, 2.0);
                DrawPartBounds(sel, plate, highlightPen);
                DrawCornerHandles(sel.Geometry.Vertices, plate.Height, 0);
            }
        }
    }

    private void DrawHoverPartHighlight()
    {
        if (_vm == null || _hoveredPart == null || _vm.SelectedParts.Contains(_hoveredPart) || !_vm.IsPartVisible(_hoveredPart)) return;

        if (_vm.NestResult != null && _vm.NestResult.Plates.Count > 0)
        {
            var plate = _vm.NestResult.Plates.FirstOrDefault();
            if (plate == null) return;

            foreach (var p in _vm.NestResult.Placed)
            {
                if (p.PartName != _hoveredPart.Name || p.TransformedGeometry.Vertices.Count < 2) continue;
                DrawWorldPolyline(p.TransformedGeometry.Vertices, plate.Height, 0, HoverBrush, 2.0);
                return;
            }
        }
        else if (_vm.Parts.Count > 0)
        {
            var plate = _vm.Plate;
            var screenVertices = new List<Point>();
            foreach (var v in _hoveredPart.Geometry.Vertices)
                screenVertices.Add(new Point(Sx(v.X), Sy(plate.Height - v.Y)));

            DrawScreenPolyline(screenVertices, HoverBrush, 2.0);
        }
    }

    private void DrawMeasurementOverlay()
    {
        if (_vm == null) return;

        var plate = _vm.Plate;
        double plateH = plate.Height;

        if (_measurePointA != null)
        {
            var ptA = _measurePointA.Value;
            double sa = Sx(ptA.X);
            double syA = Sy(plateH - ptA.Y);

            var markerA = new System.Windows.Shapes.Ellipse
            {
                Width = 8,
                Height = 8,
                Stroke = MeasureLineBrush,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(100, 0xFF, 0xD8, 0x4D)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(markerA, sa - 4);
            Canvas.SetTop(markerA, syA - 4);
            NestCanvas.Children.Add(markerA);

            var labelA = new TextBlock
            {
                Text = $"A",
                Foreground = MeasureTextBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(labelA, sa + 6);
            Canvas.SetTop(labelA, syA - 14);
            NestCanvas.Children.Add(labelA);

            if (_measurePointB != null)
            {
                var ptB = _measurePointB.Value;
                double sb = Sx(ptB.X);
                double syB = Sy(plateH - ptB.Y);

                var lineGeo = new StreamGeometry();
                using (var ctx = lineGeo.Open())
                {
                    ctx.BeginFigure(new Point(sa, syA), false, false);
                    ctx.LineTo(new Point(sb, syB), true, false);
                }

                var dashColl = new DoubleCollection { 6, 3 };
                var measurePath = new System.Windows.Shapes.Path
                {
                    Data = lineGeo,
                    Stroke = MeasureLineBrush,
                    StrokeThickness = 1.5,
                    StrokeDashArray = dashColl,
                    IsHitTestVisible = false
                };
                NestCanvas.Children.Add(measurePath);

                var markerB = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Stroke = MeasureLineBrush,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0xFF, 0xD8, 0x4D)),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(markerB, sb - 4);
                Canvas.SetTop(markerB, syB - 4);
                NestCanvas.Children.Add(markerB);

                var labelB = new TextBlock
                {
                    Text = $"B",
                    Foreground = MeasureTextBrush,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(labelB, sb + 6);
                Canvas.SetTop(labelB, syB - 14);
                NestCanvas.Children.Add(labelB);

                double dx = ptB.X - ptA.X;
                double dy = ptB.Y - ptA.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;

                double midX = Sx((ptA.X + ptB.X) / 2.0);
                double midY = Sy(plateH - (ptA.Y + ptB.Y) / 2.0);

                var infoBg = new System.Windows.Shapes.Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(200, 0x18, 0x1B, 0x1F)),
                    IsHitTestVisible = false
                };
                NestCanvas.Children.Add(infoBg);

                var infoText = new TextBlock
                {
                    Foreground = MeasureTextBrush,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    IsHitTestVisible = false
                };
                infoText.Inlines.Add(new Run($"L: {distance:F2} mm") { Foreground = MeasureTextBrush });
                infoText.Inlines.Add(new LineBreak());
                infoText.Inlines.Add(new Run($"ΔX: {dx:F2} mm") { Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xDC, 0xFE)) });
                infoText.Inlines.Add(new LineBreak());
                infoText.Inlines.Add(new Run($"ΔY: {-dy:F2} mm") { Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)) });
                infoText.Inlines.Add(new LineBreak());
                infoText.Inlines.Add(new Run($"θ: {angle:F1}°") { Foreground = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xAA)) });

                var infoSize = new System.Windows.Size();
                infoText.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                infoSize = infoText.DesiredSize;

                double infoW = infoSize.Width + 16;
                double infoH = infoSize.Height + 10;
                double infoX = midX + 10;
                double infoY = midY - infoH / 2;

                infoBg.Width = infoW;
                infoBg.Height = infoH;
                infoBg.RadiusX = 4;
                infoBg.RadiusY = 4;
                Canvas.SetLeft(infoBg, infoX);
                Canvas.SetTop(infoBg, infoY);
                Canvas.SetLeft(infoText, infoX + 8);
                Canvas.SetTop(infoText, infoY + 5);
                NestCanvas.Children.Add(infoText);
            }
        }
    }

    private void DrawWorldPolyline(List<Geometry.Point2D> vertices, double plateHeight, double worldOffsetX,
        Brush strokeBrush, double strokeThickness)
    {
        if (vertices.Count < 2) return;

        var screenVertices = new List<Point>();
        foreach (var v in vertices)
            screenVertices.Add(new Point(Sx(worldOffsetX + v.X), Sy(plateHeight - v.Y)));

        DrawScreenPolyline(screenVertices, strokeBrush, strokeThickness);
    }

    private void DrawScreenPolyline(List<Point> screenVertices, Brush strokeBrush, double strokeThickness)
    {
        if (screenVertices.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(screenVertices[0], false, true);
            for (int i = 1; i < screenVertices.Count; i++)
                ctx.LineTo(screenVertices[i], true, false);
        }

        var path = new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = strokeBrush,
            StrokeThickness = strokeThickness,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        NestCanvas.Children.Add(path);
    }

    private void DrawPartBounds(PartModel part, PlateModel plate, Pen pen)
    {
        var b = part.Geometry.Bounds;
        if (b.Width <= 0 && b.Height <= 0) return;

        DrawScreenBBox(
            Sx(b.MinX), Sy(plate.Height - b.MaxY),
            Sx(b.MaxX), Sy(plate.Height - b.MinY),
            pen);
    }

    private void DrawVertexBounds(List<Geometry.Point2D> vertices, double plateHeight, double worldOffsetX, Pen pen)
    {
        if (vertices.Count < 2) return;

        double minX = vertices.Min(v => v.X);
        double maxX = vertices.Max(v => v.X);
        double minY = vertices.Min(v => v.Y);
        double maxY = vertices.Max(v => v.Y);

        DrawScreenBBox(
            Sx(worldOffsetX + minX), Sy(plateHeight - maxY),
            Sx(worldOffsetX + maxX), Sy(plateHeight - minY),
            pen);
    }

    private void DrawCornerHandles(List<Geometry.Point2D> vertices, double plateHeight, double worldOffsetX)
    {
        if (vertices.Count < 2) return;

        double minX = vertices.Min(v => v.X);
        double maxX = vertices.Max(v => v.X);
        double minY = vertices.Min(v => v.Y);
        double maxY = vertices.Max(v => v.Y);

        double handleSize = 7;

        var corners = new[]
        {
            new Point(Sx(worldOffsetX + minX), Sy(plateHeight - minY)),
            new Point(Sx(worldOffsetX + maxX), Sy(plateHeight - minY)),
            new Point(Sx(worldOffsetX + minX), Sy(plateHeight - maxY)),
            new Point(Sx(worldOffsetX + maxX), Sy(plateHeight - maxY)),
        };

        foreach (var corner in corners)
        {
            var handle = new System.Windows.Shapes.Rectangle
            {
                Width = handleSize,
                Height = handleSize,
                Fill = HandleBrush,
                Stroke = HandleStrokeBrush,
                StrokeThickness = 1.2,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(handle, corner.X - handleSize / 2);
            Canvas.SetTop(handle, corner.Y - handleSize / 2);
            NestCanvas.Children.Add(handle);
        }
    }

    private void DrawSnapVisual()
    {
        if (_vm == null || _activeSnapVisual == null) return;

        double plateHeight = _vm.Plate.Height;
        double screenX = Sx(_activeSnapVisual.Point.X);
        double screenY = Sy(plateHeight - _activeSnapVisual.Point.Y);

        if (_activeSnapVisual.HasVerticalLine)
        {
            double x = Sx(_activeSnapVisual.LineX);
            var line = new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = NestCanvas.ActualHeight,
                Stroke = SnapBrush,
                StrokeThickness = 1,
                StrokeDashArray = SnapDash,
                IsHitTestVisible = false
            };
            NestCanvas.Children.Add(line);
        }

        if (_activeSnapVisual.HasHorizontalLine)
        {
            double y = Sy(plateHeight - _activeSnapVisual.LineY);
            var line = new System.Windows.Shapes.Line
            {
                X1 = 0,
                Y1 = y,
                X2 = NestCanvas.ActualWidth,
                Y2 = y,
                Stroke = SnapBrush,
                StrokeThickness = 1,
                StrokeDashArray = SnapDash,
                IsHitTestVisible = false
            };
            NestCanvas.Children.Add(line);
        }

        double size = 12;
        var marker = new System.Windows.Shapes.Ellipse
        {
            Width = size,
            Height = size,
            Stroke = SnapBrush,
            StrokeThickness = 2.5,
            Fill = new SolidColorBrush(Color.FromArgb(100, 0xFF, 0xD8, 0x4D)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(marker, screenX - size / 2);
        Canvas.SetTop(marker, screenY - size / 2);
        NestCanvas.Children.Add(marker);

        var label = new TextBlock
        {
            Text = $"Snap: {_activeSnapVisual.Mode}",
            Foreground = SnapBrush,
            Background = SnapLabelBgBrush,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Padding = new Thickness(4, 2, 4, 2),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(label, screenX + 8);
        Canvas.SetTop(label, screenY + 8);
        NestCanvas.Children.Add(label);
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

    private List<Color> GetGreenPalette() => new()
    {
        Color.FromRgb(0x4E, 0xC9, 0xB0),
        Color.FromRgb(0x6A, 0xD4, 0x9B),
        Color.FromRgb(0x2D, 0xB5, 0x8E),
        Color.FromRgb(0x81, 0xC7, 0x84),
        Color.FromRgb(0x38, 0x8E, 0x6E),
        Color.FromRgb(0x5C, 0xBC, 0xA0),
        Color.FromRgb(0x26, 0xA6, 0x9A),
        Color.FromRgb(0x4D, 0xB6, 0xAC)
    };

    // --- Operation List Drag & Drop ---
    private Point _operationDragStartPoint;

    private void OperationList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _operationDragStartPoint = e.GetPosition(null);
    }

    private void OperationList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _operationDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(pos.Y - _operationDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (sender is ListBox listBox && listBox.SelectedItem != null)
            {
                DragDrop.DoDragDrop(listBox, listBox.SelectedItem, DragDropEffects.Move);
            }
        }
    }

    private void OperationList_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(LaserOperation)))
            e.Effects = DragDropEffects.Move;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OperationList_Drop(object sender, DragEventArgs e)
    {
        if (_vm == null) return;
        if (e.Data.GetData(typeof(LaserOperation)) is not LaserOperation draggedOp) return;
        if (sender is not ListBox listBox) return;

        var pos = e.GetPosition(listBox);
        var item = listBox.Items.Cast<LaserOperation>()
            .FirstOrDefault(op =>
            {
                var container = listBox.ItemContainerGenerator.ContainerFromItem(op) as FrameworkElement;
                if (container == null) return false;
                var containerPos = e.GetPosition(container);
                return containerPos.Y >= 0 && containerPos.Y <= container.ActualHeight;
            });

        if (item == null || item == draggedOp) return;

        int oldIdx = _vm.Operations.IndexOf(draggedOp);
        int newIdx = _vm.Operations.IndexOf(item);
        if (oldIdx < 0 || newIdx < 0 || oldIdx == newIdx) return;

        _vm.Operations.Move(oldIdx, newIdx);
        _vm.SelectedOperation = draggedOp;
        _vm.RenumberOperations();
        _vm.MarkDirty();

        e.Handled = true;
    }

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
