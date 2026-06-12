using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using NestLaserDesktop.Models;
using NestLaserDesktop.Nesting;
using NestLaserDesktop.Services;
using NestLaserDesktop.Utilities;

namespace NestLaserDesktop.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private List<PartModel> _parts = new();
    private NestResult? _nestResult;
    private PlateModel _plate = new();
    private NestSettings _settings = new();
    private string _statusText = "Hazır";

    public List<PartModel> Parts
    {
        get => _parts;
        set { _parts = value; OnPropertyChanged(); OnPropertyChanged(nameof(PartCount)); }
    }

    public int PartCount => Parts.Count;

    public NestResult? NestResult
    {
        get => _nestResult;
        set
        {
            _nestResult = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EfficiencyText));
            OnPropertyChanged(nameof(WasteText));
            OnPropertyChanged(nameof(UnplacedCount));
            OnPropertyChanged(nameof(PlacedCount));
        }
    }

    public PlateModel Plate
    {
        get => _plate;
        set { _plate = value; OnPropertyChanged(); }
    }

    public NestSettings Settings
    {
        get => _settings;
        set { _settings = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string EfficiencyText => NestResult != null ? $"%{NestResult.Efficiency:F1}" : "--";
    public string WasteText => NestResult != null ? $"%{NestResult.WasteRate:F1}" : "--";
    public int UnplacedCount => NestResult?.Unplaced.Count ?? 0;
    public int PlacedCount => NestResult?.Placed.Count ?? 0;

    public string PlateWidthText
    {
        get => Plate.Width.ToString();
        set { if (double.TryParse(value, out double v) && v > 0) { Plate.Width = v; OnPropertyChanged(); } }
    }

    public string PlateHeightText
    {
        get => Plate.Height.ToString();
        set { if (double.TryParse(value, out double v) && v > 0) { Plate.Height = v; OnPropertyChanged(); } }
    }

    public string MarginText
    {
        get => Plate.Margin.ToString();
        set { if (double.TryParse(value, out double v) && v >= 0) { Plate.Margin = v; OnPropertyChanged(); } }
    }

    public string GapText
    {
        get => Plate.Gap.ToString();
        set { if (double.TryParse(value, out double v) && v >= 0) { Plate.Gap = v; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void LoadDxf()
    {
        var dlg = new OpenFileDialog { Filter = AppConstants.DxfFilter };
        if (dlg.ShowDialog() != true) return;

        try
        {
            Parts = DxfService.Import(dlg.FileName);
            StatusText = $"{Parts.Count} parça yüklendi: {Path.GetFileName(dlg.FileName)}";
            NestResult = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "DXF Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void RunNesting()
    {
        if (Parts.Count == 0)
        {
            MessageBox.Show("Önce DXF dosyası yükleyin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var engine = new NestingEngine();
        NestResult = engine.Run(Parts, Plate, Settings);
        StatusText = $"Nesting tamamlandı: {NestResult.Placed.Count} yerleştirildi, {NestResult.Unplaced.Count} yerleşemedi";
    }

    public void ExportDxf()
    {
        if (NestResult == null || NestResult.Placed.Count == 0)
        {
            MessageBox.Show("Önce nesting çalıştırın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new SaveFileDialog { Filter = AppConstants.DxfSaveFilter, FileName = "nested_result.dxf" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            DxfService.Export(dlg.FileName, Plate, NestResult.Placed);
            StatusText = $"Dışa aktarıldı: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "DXF Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ClearAll()
    {
        Parts = new List<PartModel>();
        NestResult = null;
        StatusText = "Temizlendi";
    }
}
