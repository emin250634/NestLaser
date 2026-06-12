using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
    private ObservableCollection<PartModel> _parts = new();
    private PartModel? _selectedPart;
    private NestResult? _nestResult;
    private PlateModel _plate = new();
    private NestSettings _settings = new();
    private string _statusText = "Hazır";
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private bool _isLoading;
    private double _totalPartsArea;

    public ObservableCollection<PartModel> Parts
    {
        get => _parts;
        set { _parts = value; OnPropertyChanged(); OnPropertyChanged(nameof(PartCount)); OnPropertyChanged(nameof(TotalPartsAreaText)); }
    }

    public PartModel? SelectedPart
    {
        get => _selectedPart;
        set { _selectedPart = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedPartInfo)); }
    }

    public int PartCount => Parts.Count;

    public double TotalPartsArea
    {
        get => _totalPartsArea;
        set { _totalPartsArea = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPartsAreaText)); }
    }

    public string TotalPartsAreaText => TotalPartsArea > 0 ? $"{TotalPartsArea:F0} mm²" : "--";

    public string SelectedPartInfo =>
        SelectedPart != null
            ? $"{SelectedPart.Name} | {SelectedPart.Width:F1}x{SelectedPart.Height:F1} mm | {SelectedPart.Area:F0} mm²"
            : "Parça seçin";

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
            OnPropertyChanged(nameof(PlateCount));
            OnPropertyChanged(nameof(UsedAreaText));
            OnPropertyChanged(nameof(TotalPlateAreaText));
            OnPropertyChanged(nameof(HasNestResult));
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

    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilePathDisplay)); }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public string FilePathDisplay => string.IsNullOrEmpty(FilePath) ? "Dosya yüklenmedi" : FilePath;

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool HasNestResult => NestResult != null;

    public string EfficiencyText => NestResult != null ? $"%{NestResult.Efficiency:F1}" : "--";
    public string WasteText => NestResult != null ? $"%{NestResult.WasteRate:F1}" : "--";
    public int UnplacedCount => NestResult?.Unplaced.Count ?? 0;
    public int PlacedCount => NestResult?.Placed.Count ?? 0;
    public int PlateCount => NestResult?.PlateCount ?? 0;
    public string UsedAreaText => NestResult?.UsedAreaText ?? "--";
    public string TotalPlateAreaText => NestResult?.TotalPlateAreaText ?? "--";

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

    public RelayCommand OpenDxfCommand { get; }
    public RelayCommand RunNestingCommand { get; }
    public RelayCommand ExportDxfCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand DeletePartCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestDrawPreview;

    public MainViewModel()
    {
        OpenDxfCommand = new RelayCommand(_ => OpenDxf());
        RunNestingCommand = new RelayCommand(_ => RunNesting());
        ExportDxfCommand = new RelayCommand(_ => ExportDxf());
        ClearCommand = new RelayCommand(_ => ClearAll());
        DeletePartCommand = new RelayCommand(_ => DeletePart(), _ => SelectedPart != null);
    }

    private void OpenDxf()
    {
        var dlg = new OpenFileDialog { Filter = AppConstants.DxfFilter };
        if (dlg.ShowDialog() != true) return;

        IsLoading = true;
        StatusText = "DXF yükleniyor...";

        try
        {
            var result = DxfService.Import(dlg.FileName);

            if (!result.Success)
            {
                StatusText = $"Hata: {string.Join(" | ", result.Errors)}";
                IsLoading = false;
                return;
            }

            FilePath = result.FilePath;
            FileName = result.FileName;
            Parts = new ObservableCollection<PartModel>(result.Parts);
            TotalPartsArea = result.TotalArea;
            NestResult = null;

            if (Parts.Count == 0)
            {
                StatusText = "Parça bulunamadı";
            }
            else
            {
                StatusText = $"{Parts.Count} parça yüklendi: {result.FileName} ({result.TotalArea:F0} mm²)";
            }

            RequestDrawPreview?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RunNesting()
    {
        if (Parts.Count == 0)
        {
            MessageBox.Show("Önce DXF dosyası yükleyin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Plate.Width <= 0 || Plate.Height <= 0)
        {
            MessageBox.Show("Plaka genişliği ve yüksekliği 0'dan büyük olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Plate.Margin * 2 >= Plate.Width || Plate.Margin * 2 >= Plate.Height)
        {
            MessageBox.Show("Kenar boşluğu plaka boyutundan büyük olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var oversizedParts = Parts.Where(p =>
            (p.Width + Settings.GapBetweenParts > Plate.UsableWidth && p.Height + Settings.GapBetweenParts > Plate.UsableHeight) &&
            (p.Height + Settings.GapBetweenParts > Plate.UsableWidth && p.Width + Settings.GapBetweenParts > Plate.UsableHeight)
        ).ToList();

        if (oversizedParts.Count > 0)
        {
            string names = string.Join(", ", oversizedParts.Select(p => p.Name));
            MessageBox.Show(
                $"Bu parçalar plaka boyutuna sığmıyor:\n{names}\n\nBu parçalar yerleşemeyenlere eklenecek.",
                "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        StatusText = "Nesting çalıştırılıyor...";

        var engine = new NestingEngine();
        NestResult = engine.Run(Parts.ToList(), Plate, Settings);

        string msg = $"Nesting tamamlandı: {NestResult.PlacedCount} yerleştirildi, {NestResult.UnplacedCount} yerleşemedi, {NestResult.PlateCount} plaka kullanıldı";
        StatusText = msg;

        RequestDrawPreview?.Invoke();
    }

    private void ExportDxf()
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

    private void ClearAll()
    {
        Parts = new ObservableCollection<PartModel>();
        SelectedPart = null;
        NestResult = null;
        FilePath = string.Empty;
        FileName = string.Empty;
        TotalPartsArea = 0;
        StatusText = "Temizlendi";
        RequestDrawPreview?.Invoke();
    }

    private void DeletePart()
    {
        if (SelectedPart == null) return;
        Parts.Remove(SelectedPart);
        TotalPartsArea = Parts.Sum(p => p.Area);
        SelectedPart = Parts.FirstOrDefault();
        StatusText = $"{Parts.Count} parça kaldı";
        RequestDrawPreview?.Invoke();
    }

    public void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
