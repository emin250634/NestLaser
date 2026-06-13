using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NestLaserDesktop.Geometry;
using Microsoft.Win32;
using NestLaserDesktop.Models;
using NestLaserDesktop.Nesting;
using NestLaserDesktop.Services;
using NestLaserDesktop.Utilities;

namespace NestLaserDesktop.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<PartModel> _parts = new();
    private ObservableCollection<PartModel> _filteredParts = new();
    private ObservableCollection<LayerModel> _layers = new();
    private ObservableCollection<LaserOperation> _operations = new();
    private readonly ObservableCollection<PartModel> _selectedParts = new();
    private LayerModel? _selectedLayer;
    private LaserOperation? _selectedOperation;
    private NestResult? _nestResult;
    private bool _operationViewMode;
    private PlateModel _plate = new();
    private NestSettings _settings = new();
    private string _statusText = "Hazır";
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private bool _isLoading;
    private string _progressOverlayText = "Hazırlanıyor...";
    private double _progressPercent;
    private bool _isProgressIndeterminate = true;
    private double _totalPartsArea;
    private string _zoomPercent = "100";
    private string _scaleFactorText = "10";
    private string _lastOperation = "";
    private string _activeTool = "Seç";
    private bool _showGrid = true;
    private bool _showRulers = true;
    private bool _showPartNames;
    private bool _showLayerNames;
    private bool _showOperationNames;
    private double _gridStepMm = 50;
    private string _mouseXText = "--";
    private string _mouseYText = "--";
    private bool _snapToGrid = true;
    private bool _snapToVertex = true;
    private bool _snapToEdge = true;
    private bool _snapToCenter = true;
    private string _snapStatusText = "Snap: --";
    private string _partSearchText = string.Empty;
    private string _partSortMode = "Ad";
    private string _propertyXText = "0";
    private string _propertyYText = "0";
    private string _propertyWidthText = "0";
    private string _propertyHeightText = "0";
    private string _propertyRotationText = "0";
    private string _layerNameText = string.Empty;
    private string _layerColorText = "#4EC9B0";
    private string _layerPowerText = "80";
    private string _layerSpeedText = "20";
    private string _layerPassCountText = "1";
    private LayerType _selectedLayerType = LayerType.Cut;
    // Operation Properties
    private string _operationNameText = string.Empty;
    private string _operationPowerText = "80";
    private string _operationSpeedText = "20";
    private string _operationPassCountText = "1";
    private string _operationPriorityText = "0";
    private OperationType _selectedOperationType = OperationType.CutOuter;

    // Material / Machine
    private List<MaterialProfile> _materials = new();
    private List<MachineProfile> _machines = new();
    private List<MaterialOperationSetting> _materialSettings = new();
    private MaterialProfile? _selectedMaterial;
    private MachineProfile? _selectedMachine;
    private string _materialFilterText = string.Empty;

    // Cost Estimation
    private CostSettings _costSettings = new();
    private JobCostEstimate? _lastCostEstimate;
    private double _costProfitMarginPercent = 30;
    private double _costVatPercent = 20;
    private string _costCurrency = "TRY";
    private CompanyProfile _companyProfile = new();
    private PdfReportSettings _pdfReportSettings = new();

    // Import Options
    private ImportUnitInfo _lastImportUnitInfo = ImportUnitInfo.Default;
    private DxfUnit _importSourceUnit = DxfUnit.Millimeters;
    private double _importManualScale = 1.0;
    private bool _importAutoDetect = true;
    private string _importVerificationText = string.Empty;
    private string _importReferenceDimText = string.Empty;
    private double _importTotalBoundingWidth;
    private double _importTotalBoundingHeight;
    private int _importPartCount;

    // Benchmark
    private NestResult? _benchmarkFreeRectResult;
    private NestResult? _benchmarkPolygonResult;
    private NestResult? _benchmarkIrregularResult;
    private string _benchmarkSummaryText = string.Empty;

    private bool _maintainAspectRatio = true;
    private bool _exportHiddenLayers;
    private bool _exportReferenceLayer;
    private bool _exportSelectedOnly;
    private bool _exportPlateBorders;

    // Project System
    private string _projectName = "Yeni Proje";
    private bool _hasUnsavedChanges;
    private string? _lastSavedPath;
    private ObservableCollection<string> _recentProjects = new();
    private string _lastProjectLoadWarning = string.Empty;

    private const int MaxUndoDepth = 50;
    private readonly Stack<UndoSnapshot> _undoStack = new();
    private readonly Stack<UndoSnapshot> _redoStack = new();

    public string ProjectName
    {
        get => _projectName;
        set { if (_projectName == value) return; _projectName = value; OnPropertyChanged(); OnPropertyChanged(nameof(WindowTitle)); }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set { if (_hasUnsavedChanges == value) return; _hasUnsavedChanges = value; OnPropertyChanged(); OnPropertyChanged(nameof(WindowTitle)); }
    }

    public string? LastSavedPath
    {
        get => _lastSavedPath;
        set { _lastSavedPath = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> RecentProjects
    {
        get => _recentProjects;
        set { _recentProjects = value; OnPropertyChanged(); }
    }

    public string WindowTitle => $"NestLaser Desktop - {ProjectName}{(HasUnsavedChanges ? " *" : "")}";

    public ObservableCollection<PartModel> Parts
    {
        get => _parts;
        set
        {
            _parts = value;
            RefreshFilteredParts();
            OnPropertyChanged();
            OnPropertyChanged(nameof(PartCount));
            OnPropertyChanged(nameof(TotalPartsAreaText));
            OnPropertyChanged(nameof(CanExportDxf));
            SetDirty();
        }
    }

    public ObservableCollection<PartModel> FilteredParts
    {
        get => _filteredParts;
        private set { _filteredParts = value; OnPropertyChanged(); }
    }

    public ObservableCollection<LayerModel> Layers
    {
        get => _layers;
        set
        {
            _layers = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LayerCount));
            RequestDrawPreview?.Invoke();
            SetDirty();
        }
    }

    public ObservableCollection<LaserOperation> Operations
    {
        get => _operations;
        set
        {
            _operations = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OperationCount));
            SetDirty();
        }
    }

    public int OperationCount => Operations.Count;

    public LayerModel? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (_selectedLayer == value) return;
            _selectedLayer = value;
            OnPropertyChanged();
            SyncLayerFieldsFromSelection();
        }
    }

    public LaserOperation? SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (_selectedOperation == value) return;
            _selectedOperation = value;
            OnPropertyChanged();
            SyncOperationFieldsFromSelection();
        }
    }

    public bool OperationViewMode
    {
        get => _operationViewMode;
        set
        {
            if (_operationViewMode == value) return;
            _operationViewMode = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public LayerType[] LayerTypes { get; } = Enum.GetValues<LayerType>();

    public OperationType[] OperationTypes { get; } = Enum.GetValues<OperationType>();

    // --- Material / Machine ---

    public List<MaterialProfile> Materials
    {
        get => _materials;
        set { _materials = value; OnPropertyChanged(); OnPropertyChanged(nameof(MaterialCount)); }
    }

    public int MaterialCount => _materials.Count;

    public List<MachineProfile> Machines
    {
        get => _machines;
        set { _machines = value; OnPropertyChanged(); OnPropertyChanged(nameof(MachineCount)); }
    }

    public int MachineCount => _machines.Count;

    public List<MaterialOperationSetting> MaterialSettings
    {
        get => _materialSettings;
        set { _materialSettings = value; OnPropertyChanged(); }
    }

    public MaterialProfile? SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            if (_selectedMaterial == value) return;
            _selectedMaterial = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CostMaterialName));
            ApplyMaterialSettingsToCurrentOperation();
            SetDirty();
        }
    }

    public MachineProfile? SelectedMachine
    {
        get => _selectedMachine;
        set
        {
            if (_selectedMachine == value) return;
            _selectedMachine = value;
            OnPropertyChanged();
            ApplyMaterialSettingsToCurrentOperation();
            SetDirty();
        }
    }

    public string MaterialFilterText
    {
        get => _materialFilterText;
        set
        {
            _materialFilterText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredMaterials));
        }
    }

    public List<MaterialProfile> FilteredMaterials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MaterialFilterText))
                return Materials;
            string f = MaterialFilterText.Trim().ToLowerInvariant();
            return Materials.Where(m =>
                m.Name.ToLowerInvariant().Contains(f) ||
                m.Category.ToLowerInvariant().Contains(f) ||
                m.DisplayName.ToLowerInvariant().Contains(f)).ToList();
        }
    }

    // --- Cost Estimation ---

    public CostSettings CostSettings
    {
        get => _costSettings;
        set { _costSettings = value; OnPropertyChanged(); }
    }

    public JobCostEstimate? LastCostEstimate
    {
        get => _lastCostEstimate;
        set
        {
            _lastCostEstimate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCostEstimate));
            OnPropertyChanged(nameof(CostMaterialName));
            OnPropertyChanged(nameof(CostMachineName));
            OnPropertyChanged(nameof(CostPlateInfo));
            OnPropertyChanged(nameof(CostEfficiencyText));
            OnPropertyChanged(nameof(CostWasteText));
            OnPropertyChanged(nameof(CostCutLengthText));
            OnPropertyChanged(nameof(CostEngraveAreaText));
            OnPropertyChanged(nameof(CostEstimatedTimeText));
            OnPropertyChanged(nameof(CostMaterialCostText));
            OnPropertyChanged(nameof(CostWasteCostText));
            OnPropertyChanged(nameof(CostMachineCostText));
            OnPropertyChanged(nameof(CostLaborCostText));
            OnPropertyChanged(nameof(CostElectricityText));
            OnPropertyChanged(nameof(CostConsumableText));
            OnPropertyChanged(nameof(CostTotalProductionText));
            OnPropertyChanged(nameof(CostSuggestedPriceText));
            OnPropertyChanged(nameof(CostFinalPriceText));
            OnPropertyChanged(nameof(HasCostEstimate));
        }
    }

    public bool HasCostEstimate => _lastCostEstimate != null;

    public string CostMaterialName => _lastCostEstimate?.MaterialName ?? "--";
    public string CostMachineName => _lastCostEstimate?.MachineName ?? "--";
    public string CostPlateInfo => _lastCostEstimate != null ? $"{_lastCostEstimate.PlateWidth:F0} x {_lastCostEstimate.PlateHeight:F0} mm" : "--";
    public string CostEfficiencyText => _lastCostEstimate != null ? $"%{_lastCostEstimate.EfficiencyPercent:F1}" : "--";
    public string CostWasteText => _lastCostEstimate != null ? $"%{_lastCostEstimate.WastePercent:F1}" : "--";
    public string CostCutLengthText => _lastCostEstimate != null ? $"{_lastCostEstimate.TotalCutLengthMm:F0} mm" : "--";
    public string CostEngraveAreaText => _lastCostEstimate != null ? $"{_lastCostEstimate.EngravingAreaMm2:F0} mm²" : "--";
    public string CostEstimatedTimeText => _lastCostEstimate != null ? $"{_lastCostEstimate.TotalEstimatedTimeMinutes:F1} dk" : "--";
    public string CostMaterialCostText => FormatCost(_lastCostEstimate?.MaterialCost);
    public string CostWasteCostText => FormatCost(_lastCostEstimate?.WasteCost);
    public string CostMachineCostText => FormatCost(_lastCostEstimate?.MachineCost);
    public string CostLaborCostText => FormatCost(_lastCostEstimate?.LaborCost);
    public string CostElectricityText => FormatCost(_lastCostEstimate?.ElectricityCost);
    public string CostConsumableText => FormatCost(_lastCostEstimate?.ConsumableCost);
    public string CostTotalProductionText => FormatCost(_lastCostEstimate?.TotalProductionCost);
    public string CostSuggestedPriceText => FormatCost(_lastCostEstimate?.SuggestedPrice);
    public string CostFinalPriceText => FormatCost(_lastCostEstimate?.FinalPriceWithVat);

    public double CostProfitMarginPercent
    {
        get => _costProfitMarginPercent;
        set { _costProfitMarginPercent = value; OnPropertyChanged(); }
    }

    public double CostVatPercent
    {
        get => _costVatPercent;
        set { _costVatPercent = value; OnPropertyChanged(); }
    }

    public string CostCurrency
    {
        get => _costCurrency;
        set { _costCurrency = value; OnPropertyChanged(); }
    }

    public CurrencyType[] CurrencyTypes { get; } = Enum.GetValues<CurrencyType>();
    public UnitType[] UnitTypeValues { get; } = Enum.GetValues<UnitType>();

    // Convenience property for XAML binding (avoids name collision with enum type)
    public UnitType[] UnitTypeItems => UnitTypeValues;

    public CompanyProfile CompanyProfile
    {
        get => _companyProfile;
        set { _companyProfile = value ?? new CompanyProfile(); OnPropertyChanged(); NotifyCompanyProfileChanged(); }
    }

    public PdfReportSettings PdfReportSettings
    {
        get => _pdfReportSettings;
        set { _pdfReportSettings = value ?? new PdfReportSettings(); OnPropertyChanged(); OnPropertyChanged(nameof(IncludePdfPreview)); }
    }

    public string CompanyName
    {
        get => CompanyProfile.CompanyName;
        set { if (CompanyProfile.CompanyName == value) return; CompanyProfile.CompanyName = value; OnPropertyChanged(); SetDirty(); }
    }

    public string CompanyAddress
    {
        get => CompanyProfile.Address;
        set { if (CompanyProfile.Address == value) return; CompanyProfile.Address = value; OnPropertyChanged(); SetDirty(); }
    }

    public string CompanyPhone
    {
        get => CompanyProfile.Phone;
        set { if (CompanyProfile.Phone == value) return; CompanyProfile.Phone = value; OnPropertyChanged(); SetDirty(); }
    }

    public string CompanyEmail
    {
        get => CompanyProfile.Email;
        set { if (CompanyProfile.Email == value) return; CompanyProfile.Email = value; OnPropertyChanged(); SetDirty(); }
    }

    public string CompanyWebsite
    {
        get => CompanyProfile.Website;
        set { if (CompanyProfile.Website == value) return; CompanyProfile.Website = value; OnPropertyChanged(); SetDirty(); }
    }

    public string CompanyLogoPath
    {
        get => CompanyProfile.LogoPath;
        set { if (CompanyProfile.LogoPath == value) return; CompanyProfile.LogoPath = value; OnPropertyChanged(); SetDirty(); }
    }

    public bool IncludePdfPreview
    {
        get => PdfReportSettings.IncludeNestingPreview;
        set { if (PdfReportSettings.IncludeNestingPreview == value) return; PdfReportSettings.IncludeNestingPreview = value; OnPropertyChanged(); SetDirty(); }
    }

    private string FormatCost(double? value)
    {
        if (value == null) return "--";
        return $"{_costCurrency}{value.Value:F2}";
    }

    private void NotifyCompanyProfileChanged()
    {
        OnPropertyChanged(nameof(CompanyName));
        OnPropertyChanged(nameof(CompanyAddress));
        OnPropertyChanged(nameof(CompanyPhone));
        OnPropertyChanged(nameof(CompanyEmail));
        OnPropertyChanged(nameof(CompanyWebsite));
        OnPropertyChanged(nameof(CompanyLogoPath));
    }

    // --- Import Verification ---

    public ImportUnitInfo LastImportUnitInfo
    {
        get => _lastImportUnitInfo;
        set { _lastImportUnitInfo = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImportSourceUnitText)); OnPropertyChanged(nameof(ImportScaleFactorText)); OnPropertyChanged(nameof(ImportDetectionSourceText)); }
    }

    public string ImportSourceUnitText => ImportUnitInfo.GetUnitDisplayName(_lastImportUnitInfo.SourceUnit);
    public string ImportScaleFactorText => $"{_lastImportUnitInfo.ScaleFactorToMm:F4}";
    public string ImportDetectionSourceText => _lastImportUnitInfo.IsUnitDetected ? _lastImportUnitInfo.DetectionSource : "Algılanamadı (varsayılan mm)";

    public double ImportTotalBoundingWidth
    {
        get => _importTotalBoundingWidth;
        set { _importTotalBoundingWidth = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImportBoundingBoxText)); }
    }

    public double ImportTotalBoundingHeight
    {
        get => _importTotalBoundingHeight;
        set { _importTotalBoundingHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(ImportBoundingBoxText)); }
    }

    public string ImportBoundingBoxText => $"{ImportTotalBoundingWidth:F1} x {ImportTotalBoundingHeight:F1} mm";

    public int ImportPartCount
    {
        get => _importPartCount;
        set { _importPartCount = value; OnPropertyChanged(); }
    }

    public string ImportVerificationText
    {
        get => _importVerificationText;
        set { _importVerificationText = value; OnPropertyChanged(); }
    }

    public DxfUnit ImportSourceUnit
    {
        get => _importSourceUnit;
        set { _importSourceUnit = value; OnPropertyChanged(); }
    }

    public DxfUnit[] ImportUnitOptions { get; } = Enum.GetValues<DxfUnit>();

    public double ImportManualScale
    {
        get => _importManualScale;
        set { _importManualScale = value; OnPropertyChanged(); }
    }

    public bool ImportAutoDetect
    {
        get => _importAutoDetect;
        set { _importAutoDetect = value; OnPropertyChanged(); }
    }

    public string ImportReferenceDimText
    {
        get => _importReferenceDimText;
        set
        {
            _importReferenceDimText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ImportSuggestedScaleText));
        }
    }

    public string ImportSuggestedScaleText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_importReferenceDimText) || Parts.Count == 0)
                return "--";
            if (!double.TryParse(_importReferenceDimText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double refMm))
                return "--";
            double currentWidth = ImportTotalBoundingWidth;
            if (currentWidth <= 0) return "--";
            double suggested = refMm / currentWidth;
            return suggested > 0 ? $"Önerilen scale: {suggested:F4} (referans genişlik: {refMm} mm)" : "--";
        }
    }

    // --- Benchmark ---

    public NestResult? BenchmarkFreeRectResult
    {
        get => _benchmarkFreeRectResult;
        set { _benchmarkFreeRectResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBenchmarkResults)); }
    }

    public NestResult? BenchmarkPolygonResult
    {
        get => _benchmarkPolygonResult;
        set { _benchmarkPolygonResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBenchmarkResults)); }
    }

    public NestResult? BenchmarkIrregularResult
    {
        get => _benchmarkIrregularResult;
        set { _benchmarkIrregularResult = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBenchmarkResults)); }
    }

    public bool HasBenchmarkResults => _benchmarkFreeRectResult != null || _benchmarkPolygonResult != null || _benchmarkIrregularResult != null;

    public string BenchmarkSummaryText
    {
        get => _benchmarkSummaryText;
        set { _benchmarkSummaryText = value; OnPropertyChanged(); }
    }

    public int LayerCount => Layers.Count;

    public ObservableCollection<PartModel> SelectedParts => _selectedParts;

    public int SelectedCount => _selectedParts.Count;

    public double SelectedArea => _selectedParts.Sum(p => p.Area);

    public string SelectedAreaText => SelectedArea > 0 ? $"{SelectedArea:F0} mm²" : "--";

    public string SelectedPartInfo =>
        _selectedParts.Count switch
        {
            0 => "Parça seçin",
            1 => $"{_selectedParts[0].Name} | {_selectedParts[0].Width:F1}x{_selectedParts[0].Height:F1} mm | {_selectedParts[0].Area:F0} mm²",
            _ => $"{_selectedParts.Count} parça seçili | {SelectedAreaText}"
        };

    public bool HasSingleSelection => _selectedParts.Count == 1;

    public string SelectionWidthText => _selectedParts.Count > 0 ? $"{GetSelectionBounds().Width:F1} mm" : "--";

    public string SelectionHeightText => _selectedParts.Count > 0 ? $"{GetSelectionBounds().Height:F1} mm" : "--";

    public string SelectionAreaText => _selectedParts.Count > 0 ? $"{GetSelectionBounds().Area:F0} mm²" : "--";

    public string SelectionPerimeterText
    {
        get
        {
            if (_selectedParts.Count == 0) return "--";
            double perimeter = CalcSelectionPerimeter();
            return $"{perimeter:F1} mm";
        }
    }

    public string SelectionTotalAreaText
    {
        get
        {
            if (_selectedParts.Count == 0) return "--";
            double total = _selectedParts.Sum(p => p.Area);
            return $"{total:F0} mm²";
        }
    }

    private double CalcSelectionPerimeter()
    {
        if (_selectedParts.Count == 0) return 0;
        if (_selectedParts.Count == 1)
        {
            var poly = _selectedParts[0].Geometry;
            double p = 0;
            for (int i = 0; i < poly.Vertices.Count; i++)
            {
                var v1 = poly.Vertices[i];
                var v2 = poly.Vertices[(i + 1) % poly.Vertices.Count];
                p += v1.DistanceTo(v2);
            }
            return p;
        }
        var b = GetSelectionBounds();
        return 2.0 * (b.Width + b.Height);
    }

    public int PartCount => Parts.Count;

    public double TotalPartsArea
    {
        get => _totalPartsArea;
        set { _totalPartsArea = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPartsAreaText)); }
    }

    public string TotalPartsAreaText => TotalPartsArea > 0 ? $"{TotalPartsArea:F0} mm²" : "--";

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
            OnPropertyChanged(nameof(NestingTimeText));
            OnPropertyChanged(nameof(AlgorithmNameText));
            OnPropertyChanged(nameof(AlgorithmUsedText));
            OnPropertyChanged(nameof(FallbackStatusText));
            OnPropertyChanged(nameof(HasFallback));
            OnPropertyChanged(nameof(AverageGapText));
            OnPropertyChanged(nameof(LargestEmptyAreaText));
            OnPropertyChanged(nameof(PlacementAttempts));
            OnPropertyChanged(nameof(CandidatePositionsTested));
            OnPropertyChanged(nameof(CollisionCacheHits));
            OnPropertyChanged(nameof(CollisionCheckCount));
            OnPropertyChanged(nameof(BoundingBoxRejectCount));
            OnPropertyChanged(nameof(AveragePlacementScoreText));
            OnPropertyChanged(nameof(NestingWarningText));
            OnPropertyChanged(nameof(HasNestingWarnings));

            foreach (var part in Parts)
                part.IsPlaced = _nestResult?.Placed.Any(p => p.PartName == part.Name) ?? false;
        }
    }

    public PlateModel Plate
    {
        get => _plate;
        set { _plate = value; OnPropertyChanged(); SetDirty(); }
    }

    public NestSettings Settings
    {
        get => _settings;
        set { _settings = value; OnPropertyChanged(); SetDirty(); }
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

    public string ProgressOverlayText
    {
        get => _progressOverlayText;
        set { _progressOverlayText = value; OnPropertyChanged(); }
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        set { _progressPercent = value; OnPropertyChanged(); }
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        set { _isProgressIndeterminate = value; OnPropertyChanged(); }
    }

    public bool HasNestResult => NestResult != null;

    public string EfficiencyText => NestResult != null ? $"%{NestResult.Efficiency:F1}" : "--";
    public string WasteText => NestResult != null ? $"%{NestResult.WasteRate:F1}" : "--";
    public int UnplacedCount => NestResult?.Unplaced.Count ?? 0;
    public int PlacedCount => NestResult?.Placed.Count ?? 0;
    public int PlateCount => NestResult?.PlateCount ?? 0;
    public string UsedAreaText => NestResult?.UsedAreaText ?? "--";
    public string TotalPlateAreaText => NestResult?.TotalPlateAreaText ?? "--";
    public string NestingTimeText => NestResult?.NestingTimeText ?? "--";
    public string AlgorithmNameText => NestResult?.AlgorithmName ?? "--";
    public string AlgorithmUsedText => NestResult?.AlgorithmUsed ?? "--";
    public bool HasFallback => NestResult?.FallbackUsed ?? false;
    public string FallbackStatusText => HasFallback ? "Fallback Used (Timeout/Efficiency)" : "Normal";
    public string AverageGapText => NestResult?.AverageGapText ?? "--";
    public string LargestEmptyAreaText => NestResult?.LargestEmptyAreaText ?? "--";

    public NestAlgorithm[] NestAlgorithms { get; } = Enum.GetValues<NestAlgorithm>();

    public NestAlgorithm SelectedAlgorithm
    {
        get => _settings.Algorithm;
        set { _settings.Algorithm = value; OnPropertyChanged(); SetDirty(); }
    }
    public int PlacementAttempts => NestResult?.PlacementAttempts ?? 0;
    public int CandidatePositionsTested => NestResult?.CandidatePositionsTested ?? 0;
    public int CollisionCacheHits => NestResult?.CollisionCacheHits ?? 0;
    public int CollisionCheckCount => NestResult?.CollisionCheckCount ?? 0;
    public int BoundingBoxRejectCount => NestResult?.BoundingBoxRejectCount ?? 0;
    public string AveragePlacementScoreText => NestResult != null ? $"{NestResult.AveragePlacementScore:F1}" : "--";
    public bool HasNestingWarnings => NestResult?.Warnings.Count > 0;
    public string NestingWarningText => HasNestingWarnings ? string.Join(" | ", NestResult!.Warnings) : "";

    public string PlateWidthText
    {
        get => _plate.Width.ToString();
        set => UpdatePlateValue(value, v => _plate.Width = v, v => v > 0, "Plaka genişliği 0'dan büyük olmalıdır.", nameof(PlateWidthText));
    }

    public string PlateHeightText
    {
        get => _plate.Height.ToString();
        set => UpdatePlateValue(value, v => _plate.Height = v, v => v > 0, "Plaka yüksekliği 0'dan büyük olmalıdır.", nameof(PlateHeightText));
    }

    public string MarginText
    {
        get => _plate.Margin.ToString();
        set => UpdatePlateValue(value, v => { _plate.Margin = v; _settings.PlateMargin = v; }, IsValidMargin, "Kenar boşluğu plaka boyutundan büyük olamaz.", nameof(MarginText));
    }

    public string GapText
    {
        get => _plate.Gap.ToString();
        set => UpdatePlateValue(value, v => { _plate.Gap = v; _settings.GapBetweenParts = v; }, v => v >= 0, "Parça arası boşluk 0 veya daha büyük olmalıdır.", nameof(GapText));
    }

    public string ZoomPercent
    {
        get => _zoomPercent;
        set { _zoomPercent = value; OnPropertyChanged(); }
    }

    public string ScaleFactorText
    {
        get => _scaleFactorText;
        set { _scaleFactorText = value; OnPropertyChanged(); }
    }

    public string LastOperation
    {
        get => _lastOperation;
        set { _lastOperation = value; OnPropertyChanged(); }
    }

    public string ActiveTool
    {
        get => _activeTool;
        set { _activeTool = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveToolText)); }
    }

    public string ActiveToolText => $"Araç: {_activeTool}";

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            if (_showGrid == value) return;
            _showGrid = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public bool ShowRulers
    {
        get => _showRulers;
        set
        {
            if (_showRulers == value) return;
            _showRulers = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public bool ShowPartNames
    {
        get => _showPartNames;
        set
        {
            if (_showPartNames == value) return;
            _showPartNames = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public bool ShowLayerNames
    {
        get => _showLayerNames;
        set
        {
            if (_showLayerNames == value) return;
            _showLayerNames = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public bool ShowOperationNames
    {
        get => _showOperationNames;
        set
        {
            if (_showOperationNames == value) return;
            _showOperationNames = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public double GridStepMm
    {
        get => _gridStepMm;
        set
        {
            if (Math.Abs(_gridStepMm - value) < 1e-9) return;
            _gridStepMm = value;
            OnPropertyChanged();
            RequestDrawPreview?.Invoke();
        }
    }

    public string MouseXText
    {
        get => _mouseXText;
        set { _mouseXText = value; OnPropertyChanged(); }
    }

    public string MouseYText
    {
        get => _mouseYText;
        set { _mouseYText = value; OnPropertyChanged(); }
    }

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set { _snapToGrid = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveSnapMode)); }
    }

    public bool SnapToVertex
    {
        get => _snapToVertex;
        set { _snapToVertex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveSnapMode)); }
    }

    public bool SnapToEdge
    {
        get => _snapToEdge;
        set { _snapToEdge = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveSnapMode)); }
    }

    public bool SnapToCenter
    {
        get => _snapToCenter;
        set { _snapToCenter = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveSnapMode)); }
    }

    public SnapMode ActiveSnapMode
    {
        get
        {
            var mode = SnapMode.None;
            if (SnapToGrid) mode |= SnapMode.Grid;
            if (SnapToVertex) mode |= SnapMode.Vertex;
            if (SnapToEdge) mode |= SnapMode.Edge;
            if (SnapToCenter) mode |= SnapMode.Center;
            return mode;
        }
    }

    public string SnapStatusText
    {
        get => _snapStatusText;
        set { _snapStatusText = value; OnPropertyChanged(); }
    }

    public string PartSearchText
    {
        get => _partSearchText;
        set
        {
            if (_partSearchText == value) return;
            _partSearchText = value;
            OnPropertyChanged();
            RefreshFilteredParts();
        }
    }

    public string PartSortMode
    {
        get => _partSortMode;
        set
        {
            if (_partSortMode == value) return;
            _partSortMode = value;
            OnPropertyChanged();
            RefreshFilteredParts();
        }
    }

    public string PropertyXText
    {
        get => _propertyXText;
        set { _propertyXText = value; OnPropertyChanged(); }
    }

    public string PropertyYText
    {
        get => _propertyYText;
        set { _propertyYText = value; OnPropertyChanged(); }
    }

    public string PropertyWidthText
    {
        get => _propertyWidthText;
        set
        {
            _propertyWidthText = value;
            OnPropertyChanged();
            if (MaintainAspectRatio && TryParsePositive(value, out double width))
                UpdateHeightFromAspect(width);
        }
    }

    public string PropertyHeightText
    {
        get => _propertyHeightText;
        set
        {
            _propertyHeightText = value;
            OnPropertyChanged();
            if (MaintainAspectRatio && TryParsePositive(value, out double height))
                UpdateWidthFromAspect(height);
        }
    }

    public string PropertyRotationText
    {
        get => _propertyRotationText;
        set { _propertyRotationText = value; OnPropertyChanged(); }
    }

    public string LayerNameText
    {
        get => _layerNameText;
        set { _layerNameText = value; OnPropertyChanged(); }
    }

    public string LayerColorText
    {
        get => _layerColorText;
        set { _layerColorText = value; OnPropertyChanged(); }
    }

    public string LayerPowerText
    {
        get => _layerPowerText;
        set { _layerPowerText = value; OnPropertyChanged(); }
    }

    public string LayerSpeedText
    {
        get => _layerSpeedText;
        set { _layerSpeedText = value; OnPropertyChanged(); }
    }

    public string LayerPassCountText
    {
        get => _layerPassCountText;
        set { _layerPassCountText = value; OnPropertyChanged(); }
    }

    public LayerType SelectedLayerType
    {
        get => _selectedLayerType;
        set { _selectedLayerType = value; OnPropertyChanged(); }
    }

    public string OperationNameText
    {
        get => _operationNameText;
        set { _operationNameText = value; OnPropertyChanged(); }
    }

    public string OperationPowerText
    {
        get => _operationPowerText;
        set { _operationPowerText = value; OnPropertyChanged(); }
    }

    public string OperationSpeedText
    {
        get => _operationSpeedText;
        set { _operationSpeedText = value; OnPropertyChanged(); }
    }

    public string OperationPassCountText
    {
        get => _operationPassCountText;
        set { _operationPassCountText = value; OnPropertyChanged(); }
    }

    public string OperationPriorityText
    {
        get => _operationPriorityText;
        set { _operationPriorityText = value; OnPropertyChanged(); }
    }

    public OperationType SelectedOperationType
    {
        get => _selectedOperationType;
        set { _selectedOperationType = value; OnPropertyChanged(); }
    }

    public bool ExportHiddenLayers
    {
        get => _exportHiddenLayers;
        set { _exportHiddenLayers = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExportDxf)); NotifyCommandStateChanged(); }
    }

    public bool ExportReferenceLayer
    {
        get => _exportReferenceLayer;
        set { _exportReferenceLayer = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExportDxf)); NotifyCommandStateChanged(); }
    }

    public bool ExportSelectedOnly
    {
        get => _exportSelectedOnly;
        set { _exportSelectedOnly = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExportDxf)); NotifyCommandStateChanged(); }
    }

    public bool ExportPlateBorders
    {
        get => _exportPlateBorders;
        set { _exportPlateBorders = value; OnPropertyChanged(); NotifyCommandStateChanged(); }
    }

    public bool MaintainAspectRatio
    {
        get => _maintainAspectRatio;
        set { _maintainAspectRatio = value; OnPropertyChanged(); }
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public bool HasSelectedParts => _selectedParts.Count > 0;
    public bool HasScaledParts => Parts.Any(p => p.IsScaled);
    public bool CanExportDxf => Parts.Count > 0 && (!ExportSelectedOnly || SelectedCount > 0);

    // Project Commands
    public RelayCommand NewProjectCommand { get; }
    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand SaveProjectCommand { get; }
    public RelayCommand SaveProjectAsCommand { get; }
    public RelayCommand OpenRecentProjectCommand { get; }
    public RelayCommand ExportProjectPackageCommand { get; }
    public RelayCommand ImportProjectPackageCommand { get; }

    // Existing Commands
    public RelayCommand OpenDxfCommand { get; }
    public RelayCommand RunNestingCommand { get; }
    public RelayCommand ExportDxfCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand DeleteSelectedCommand { get; }
    public RelayCommand ScaleSelectedCommand { get; }
    public RelayCommand ScaleAllPartsCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public RelayCommand RotateSelected90Command { get; }
    public RelayCommand MirrorSelectedXCommand { get; }
    public RelayCommand MirrorSelectedYCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand DeselectAllCommand { get; }
    public RelayCommand ApplyPropertiesCommand { get; }
    public RelayCommand AddLayerCommand { get; }
    public RelayCommand DeleteLayerCommand { get; }
    public RelayCommand ApplyLayerPropertiesCommand { get; }
    public RelayCommand AssignSelectedToLayerCommand { get; }
    public RelayCommand ToggleLayerVisibilityCommand { get; }
    public RelayCommand ToggleLayerLockCommand { get; }

    // Operation Commands
    public RelayCommand AddOperationCommand { get; }
    public RelayCommand DeleteOperationCommand { get; }
    public RelayCommand MoveOperationUpCommand { get; }
    public RelayCommand MoveOperationDownCommand { get; }
    public RelayCommand AutoSuggestOperationsCommand { get; }
    public RelayCommand AnalyzeInnerOuterCommand { get; }
    public RelayCommand ApplyOperationPropertiesCommand { get; }

    // Material / Machine Commands
    public RelayCommand ApplyMaterialSettingsCommand { get; }
    public RelayCommand RefreshMaterialsCommand { get; }
    public RelayCommand AddMaterialCommand { get; }
    public RelayCommand DeleteMaterialCommand { get; }
    public RelayCommand AddMachineCommand { get; }
    public RelayCommand DeleteMachineCommand { get; }
    public RelayCommand SaveMaterialChangesCommand { get; }

    // Cost Estimation Commands
    public RelayCommand CalculateCostCommand { get; }
    public RelayCommand CopyQuotationCommand { get; }
    public RelayCommand ExportQuotationPdfCommand { get; }
    public RelayCommand ExportProductionPdfCommand { get; }

    // Import & Benchmark Commands
    public RelayCommand OpenDxfWithOptionsCommand { get; }
    public RelayCommand RunBenchmarkCommand { get; }
    public RelayCommand ApplySuggestedScaleCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestDrawPreview;

    public MainViewModel()
    {
        // Project Commands
        NewProjectCommand = new RelayCommand(async _ => await NewProject());
        OpenProjectCommand = new RelayCommand(async _ => await OpenProject());
        SaveProjectCommand = new RelayCommand(async _ => await SaveProjectAsync());
        SaveProjectAsCommand = new RelayCommand(async _ => await SaveProjectAsAsync());
        OpenRecentProjectCommand = new RelayCommand(async path => await OpenRecentProject(path as string));
        ExportProjectPackageCommand = new RelayCommand(async _ => await ExportProjectPackage());
        ImportProjectPackageCommand = new RelayCommand(async _ => await ImportProjectPackage());
        
        LoadRecentProjects();

        // Existing Commands
        OpenDxfCommand = new RelayCommand(async _ => await OpenDxf());
        RunNestingCommand = new RelayCommand(async _ => await RunNesting());
        ExportDxfCommand = new RelayCommand(async _ => await ExportDxf(), _ => CanExportDxf);
        ClearCommand = new RelayCommand(_ => ClearAll());
        DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => _selectedParts.Count > 0);
        ScaleSelectedCommand = new RelayCommand(_ => ScaleSelected(), _ => _selectedParts.Count > 0 && Parts.Count > 0);
        ScaleAllPartsCommand = new RelayCommand(_ => ScaleAllParts(), _ => Parts.Count > 0);
        UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
        RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);
        RotateSelected90Command = new RelayCommand(_ => RotateSelected90(), _ => _selectedParts.Count > 0);
        MirrorSelectedXCommand = new RelayCommand(_ => MirrorSelectedX(), _ => _selectedParts.Count > 0);
        MirrorSelectedYCommand = new RelayCommand(_ => MirrorSelectedY(), _ => _selectedParts.Count > 0);
        SelectAllCommand = new RelayCommand(_ => SelectAll(), _ => Parts.Count > 0);
        DeselectAllCommand = new RelayCommand(_ => DeselectAll(), _ => _selectedParts.Count > 0);
        ApplyPropertiesCommand = new RelayCommand(_ => ApplyProperties(), _ => _selectedParts.Count > 0);
        AddLayerCommand = new RelayCommand(_ => AddLayer());
        DeleteLayerCommand = new RelayCommand(_ => DeleteSelectedLayer(), _ => SelectedLayer != null && Layers.Count > 1);
        ApplyLayerPropertiesCommand = new RelayCommand(_ => ApplyLayerProperties(), _ => SelectedLayer != null);
        AssignSelectedToLayerCommand = new RelayCommand(_ => AssignSelectedToLayer(), _ => SelectedLayer != null && _selectedParts.Count > 0);
        ToggleLayerVisibilityCommand = new RelayCommand(layer => ToggleLayerVisibility(layer as LayerModel), layer => layer is LayerModel);
        ToggleLayerLockCommand = new RelayCommand(layer => ToggleLayerLock(layer as LayerModel), layer => layer is LayerModel);

        // Operation Commands
        AddOperationCommand = new RelayCommand(_ => AddOperation());
        DeleteOperationCommand = new RelayCommand(_ => DeleteOperation(), _ => SelectedOperation != null);
        MoveOperationUpCommand = new RelayCommand(_ => MoveOperation(-1), _ => SelectedOperation != null && Operations.IndexOf(SelectedOperation) > 0);
        MoveOperationDownCommand = new RelayCommand(_ => MoveOperation(1), _ => SelectedOperation != null && Operations.IndexOf(SelectedOperation) < Operations.Count - 1);
        AutoSuggestOperationsCommand = new RelayCommand(_ => AutoSuggestOperations());
        AnalyzeInnerOuterCommand = new RelayCommand(_ => AnalyzeInnerOuterCut());
        ApplyOperationPropertiesCommand = new RelayCommand(_ => ApplyOperationProperties(), _ => SelectedOperation != null);

        // Material / Machine Commands
        ApplyMaterialSettingsCommand = new RelayCommand(_ => ApplyMaterialSettingsToCurrentOperation());
        RefreshMaterialsCommand = new RelayCommand(_ => ReloadMaterials());
        AddMaterialCommand = new RelayCommand(_ => AddMaterial());
        DeleteMaterialCommand = new RelayCommand(_ => DeleteMaterial(), _ => SelectedMaterial != null);
        AddMachineCommand = new RelayCommand(_ => AddMachine());
        DeleteMachineCommand = new RelayCommand(_ => DeleteMachine(), _ => SelectedMachine != null);
        SaveMaterialChangesCommand = new RelayCommand(_ => SaveMaterialChanges(), _ => SelectedMaterial != null);

        // Cost Estimation Commands
        CalculateCostCommand = new RelayCommand(async _ => await CalculateCost());
        CopyQuotationCommand = new RelayCommand(_ => CopyQuotation(), _ => LastCostEstimate != null);
        ExportQuotationPdfCommand = new RelayCommand(async _ => await ExportQuotationPdf());
        ExportProductionPdfCommand = new RelayCommand(async _ => await ExportProductionPdf());

        // Import & Benchmark Commands
        OpenDxfWithOptionsCommand = new RelayCommand(async _ => await OpenDxfWithOptions());
        RunBenchmarkCommand = new RelayCommand(async _ => await RunBenchmark(), _ => Parts.Count > 0);
        ApplySuggestedScaleCommand = new RelayCommand(_ => ApplySuggestedScale());

        LoadMaterialProfiles();
        CostSettings = CostEstimationService.LoadSettings();
        CostCurrency = CostSettings.Currency;
        CostProfitMarginPercent = CostSettings.DefaultProfitMarginPercent;
        CostVatPercent = CostSettings.VatPercent;

        EnsureDefaultLayers();
        EnsureDefaultOperations();
        RefreshFilteredParts();
        SyncPropertyFieldsFromSelection();
    }

    private void SetDirty()
    {
        if (!_isLoading)
            HasUnsavedChanges = true;
    }

    private IProgress<WorkflowProgress> CreateProgress(string fallbackMessage)
    {
        ProgressOverlayText = fallbackMessage;
        IsProgressIndeterminate = true;
        ProgressPercent = 0;

        return new Progress<WorkflowProgress>(progress =>
        {
            ProgressOverlayText = progress.Message;
            if (progress.Percent.HasValue)
            {
                IsProgressIndeterminate = false;
                ProgressPercent = progress.Percent.Value;
            }
            else
            {
                IsProgressIndeterminate = true;
            }
        });
    }

    private void BeginBusy(string message)
    {
        IsLoading = true;
        StatusText = message;
        ProgressOverlayText = message;
        ProgressPercent = 0;
        IsProgressIndeterminate = true;
    }

    private void EndBusy()
    {
        IsLoading = false;
        IsProgressIndeterminate = true;
        ProgressPercent = 0;
    }

    private void LoadRecentProjects()
    {
        var list = ProjectWorkflowService.LoadRecentProjects();
        RecentProjects = new ObservableCollection<string>(list);
    }

    private void AddToRecentProjects(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        
        var list = ProjectWorkflowService.AddRecentProject(RecentProjects, path);
        RecentProjects = new ObservableCollection<string>(list);
    }

    private async Task NewProject()
    {
        if (HasUnsavedChanges)
        {
            var result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?", "Yeni Proje", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            if (result == MessageBoxResult.Yes)
            {
                if (!await SaveProjectAsync()) return;
            }
        }

        ClearAllInternal();
        ProjectName = "Yeni Proje";
        LastSavedPath = null;
        HasUnsavedChanges = false;
        _undoStack.Clear();
        _redoStack.Clear();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        StatusText = "Yeni proje oluşturuldu";
    }

    private void ClearAllInternal()
    {
        _isLoading = true;
        Parts = new ObservableCollection<PartModel>();
        _selectedParts.Clear();
        Operations = new ObservableCollection<LaserOperation>();
        NestResult = null;
        FilePath = string.Empty;
        FileName = string.Empty;
        TotalPartsArea = 0;
        _isLoading = false;
    }

    private async Task OpenProject()
    {
        var dlg = new OpenFileDialog { Filter = "NestLaser Project (*.nelp)|*.nelp" };
        if (dlg.ShowDialog() != true) return;

        await OpenProjectFile(dlg.FileName);
    }

    private async Task OpenRecentProject(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (!File.Exists(path))
        {
            MessageBox.Show("Dosya bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            var list = ProjectWorkflowService.RemoveRecentProject(RecentProjects, path);
            RecentProjects = new ObservableCollection<string>(list);
            return;
        }
        await OpenProjectFile(path);
    }

    private async Task OpenProjectFile(string path)
    {
        if (HasUnsavedChanges)
        {
            var result = MessageBox.Show("Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?", "Proje Aç", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            if (result == MessageBoxResult.Yes)
            {
                if (!await SaveProjectAsync()) return;
            }
        }

        BeginBusy("Proje yükleniyor...");
        var progress = CreateProgress("Proje yükleniyor...");

        try
        {
            var loadResult = await ProjectWorkflowService.OpenWithRecoveryAsync(path, progress);
            var project = loadResult.Project;
            if (project == null)
            {
                StatusText = "Hata: Proje dosyası okunamadı.";
                return;
            }

            ApplyProject(project, path);
            AddToRecentProjects(path);
            var recoveryWarning = loadResult.RecoveryReport.ToStatusText();
            if (!string.IsNullOrWhiteSpace(recoveryWarning))
                _lastProjectLoadWarning = string.IsNullOrWhiteSpace(_lastProjectLoadWarning)
                    ? recoveryWarning
                    : $"{_lastProjectLoadWarning} {recoveryWarning}";
            StatusText = string.IsNullOrWhiteSpace(_lastProjectLoadWarning)
                ? $"Proje yüklendi: {ProjectName}"
                : $"Proje yüklendi: {ProjectName} | {_lastProjectLoadWarning}";
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private void ApplyProject(NestLaserProject project, string path)
    {
        _isLoading = true;
        
        ProjectName = project.ProjectName;
        LastSavedPath = path;
        
        Plate = project.Plate;
        Settings = project.Settings;
        Layers = new ObservableCollection<LayerModel>(project.Layers);
        Operations = new ObservableCollection<LaserOperation>(project.Operations ?? new List<LaserOperation>());
        Parts = new ObservableCollection<PartModel>(project.Parts);
        NestResult = project.NestResult;
        
        RemapNestResultPartReferences();
        FilePath = project.SourceDxfPath;
        FileName = Path.GetFileName(FilePath);

        var profileWarnings = RestoreSelectedProfiles(project);

        CostProfitMarginPercent = project.CostProfitMarginPercent;
        CostVatPercent = project.CostVatPercent;
        CostCurrency = project.CostCurrency;
        LastCostEstimate = project.LastCostEstimate;
        CompanyProfile = project.CompanyProfile ?? new CompanyProfile();
        PdfReportSettings = project.PdfReportSettings ?? new PdfReportSettings();

        NormalizePartLayers();
        TotalPartsArea = Parts.Sum(p => p.Area);
        
        _undoStack.Clear();
        _redoStack.Clear();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        
        _isLoading = false;
        HasUnsavedChanges = false;
        
        if (profileWarnings.Count > 0)
            _lastProjectLoadWarning = string.Join(" ", profileWarnings);
        else
            _lastProjectLoadWarning = string.Empty;

        RequestDrawPreview?.Invoke();
    }

    public async Task<bool> SaveProjectAsync()
    {
        if (string.IsNullOrEmpty(LastSavedPath))
        {
            return await SaveProjectAsAsync();
        }

        BeginBusy("Proje kaydediliyor...");
        var progress = CreateProgress("Proje kaydediliyor...");

        try
        {
            var project = CreateProjectFromState();
            await ProjectWorkflowService.SaveAsync(LastSavedPath, project, progress);
            HasUnsavedChanges = false;
            StatusText = $"Proje kaydedildi: {DateTime.Now:HH:mm:ss}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            EndBusy();
        }
    }

    public async Task<bool> SaveProjectAsAsync()
    {
        var dlg = new SaveFileDialog 
        { 
            Filter = "NestLaser Project (*.nelp)|*.nelp",
            FileName = string.IsNullOrEmpty(LastSavedPath) ? ProjectWorkflowService.DefaultProjectFileName(ProjectName) : Path.GetFileName(LastSavedPath)
        };
        
        if (dlg.ShowDialog() != true) return false;

        LastSavedPath = dlg.FileName;
        ProjectName = ProjectWorkflowService.ProjectNameFromPath(LastSavedPath);
        AddToRecentProjects(LastSavedPath);
        return await SaveProjectAsync();
    }

    private async Task ExportProjectPackage()
    {
        var packageName = ProjectWorkflowService.DefaultProjectFileName(ProjectName);
        packageName = Path.ChangeExtension(packageName, ".nelpkg");

        var dlg = new SaveFileDialog
        {
            Filter = "NestLaser Project Package (*.nelpkg)|*.nelpkg",
            FileName = packageName
        };

        if (dlg.ShowDialog() != true) return;

        BeginBusy("Proje paketi hazirlaniyor...");
        var progress = CreateProgress("Proje paketi hazirlaniyor...");

        try
        {
            var project = CreateProjectFromState();
            string pdfPath = File.Exists(PdfReportSettings.LastQuotationPdfPath)
                ? PdfReportSettings.LastQuotationPdfPath
                : PdfReportSettings.LastProductionReportPdfPath;

            await Task.Run(() =>
            {
                progress.Report(new WorkflowProgress("Paket dosyasi yaziliyor...", 55));
                ProjectPackageService.ExportPackage(dlg.FileName, project, pdfPath);
                progress.Report(new WorkflowProgress("Paket tamamlandi.", 95));
            });

            StatusText = $"Proje paketi olusturuldu: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, "Project package export failed.");
            MessageBox.Show($"Paket olusturma hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task ImportProjectPackage()
    {
        if (HasUnsavedChanges)
        {
            var result = MessageBox.Show("Kaydedilmemis degisiklikler var. Kaydetmek ister misiniz?", "Paket Ice Aktar", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            if (result == MessageBoxResult.Yes)
            {
                if (!await SaveProjectAsync()) return;
            }
        }

        var dlg = new OpenFileDialog { Filter = "NestLaser Project Package (*.nelpkg)|*.nelpkg" };
        if (dlg.ShowDialog() != true) return;

        BeginBusy("Proje paketi aciliyor...");
        var progress = CreateProgress("Proje paketi aciliyor...");

        try
        {
            var loadResult = await Task.Run(() =>
            {
                progress.Report(new WorkflowProgress("Paket okunuyor...", 35));
                var result = ProjectPackageService.ImportPackage(dlg.FileName);
                progress.Report(new WorkflowProgress("Proje kurtarma kontrolleri yapiliyor...", 85));
                return result;
            });

            if (loadResult.Project == null)
            {
                StatusText = "Hata: Paket icindeki proje okunamadi.";
                return;
            }

            ApplyProject(loadResult.Project, string.Empty);
            LastSavedPath = null;
            FileName = Path.GetFileName(FilePath);
            HasUnsavedChanges = true;

            var recoveryWarning = loadResult.RecoveryReport.ToStatusText();
            StatusText = string.IsNullOrWhiteSpace(recoveryWarning)
                ? $"Proje paketi acildi: {ProjectName}"
                : $"Proje paketi acildi: {ProjectName} | {recoveryWarning}";
        }
        catch (Exception ex)
        {
            AppLogger.LogError(ex, "Project package import failed.");
            MessageBox.Show($"Paket acma hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private NestLaserProject CreateProjectFromState()
    {
        return new NestLaserProject
        {
            ProjectName = ProjectName,
            SourceDxfPath = FilePath,
            Plate = Plate,
            Parts = Parts.ToList(),
            Layers = Layers.ToList(),
            Operations = Operations.ToList(),
            NestResult = NestResult,
            Settings = Settings,
            SelectedMaterialId = SelectedMaterial?.Id,
            SelectedMachineId = SelectedMachine?.Id,
            ProfileSnapshot = new ProfileSnapshot
            {
                Material = SelectedMaterial,
                Machine = SelectedMachine,
                OperationSettings = MaterialSettings.ToList(),
                CostSettings = CostSettings
            },
            CostProfitMarginPercent = CostProfitMarginPercent,
            CostVatPercent = CostVatPercent,
            CostCurrency = CostCurrency,
            LastCostEstimate = LastCostEstimate,
            CompanyProfile = CompanyProfile.Clone(),
            PdfReportSettings = PdfReportSettings.Clone(),
            AppVersion = ProjectMigrationService.CurrentProjectVersion,
            ProjectVersion = ProjectMigrationService.CurrentProjectVersion,
            CreatedWithVersion = ProjectMigrationService.CurrentProjectVersion,
            LastSavedWithVersion = ProjectMigrationService.CurrentProjectVersion
        };
    }

    private List<string> RestoreSelectedProfiles(NestLaserProject project)
    {
        var warnings = new List<string>();
        if (project.ProfileSnapshot?.OperationSettings.Count > 0)
        {
            var localSettings = MaterialSettings.ToList();
            var added = 0;
            foreach (var setting in project.ProfileSnapshot.OperationSettings)
            {
                if (localSettings.Any(s => s.Id == setting.Id))
                    continue;

                localSettings.Add(setting);
                added++;
            }

            if (added > 0)
            {
                MaterialSettings = localSettings;
                warnings.Add("Profil bulunamadi: Operasyon ayarlari proje snapshot'indan yuklendi.");
            }
        }

        if (!string.IsNullOrEmpty(project.SelectedMaterialId))
        {
            SelectedMaterial = Materials.FirstOrDefault(m => m.Id == project.SelectedMaterialId);
            if (SelectedMaterial == null && project.ProfileSnapshot?.Material != null)
            {
                var restoredMaterial = project.ProfileSnapshot.Material;
                var list = Materials.ToList();
                if (!list.Any(m => m.Id == restoredMaterial.Id))
                    list.Add(restoredMaterial);
                Materials = list;
                SelectedMaterial = restoredMaterial;
                warnings.Add("Profil bulunamadı: Malzeme proje snapshot'ından yüklendi.");
            }
            else if (SelectedMaterial == null)
            {
                var temporaryMaterial = new MaterialProfile
                {
                    Id = project.SelectedMaterialId,
                    Name = "Temporary Material (Recovered)",
                    Category = "Recovered",
                    Notes = "Project portability recovery profile. Replace with a real material profile before production."
                };
                var list = Materials.ToList();
                list.Add(temporaryMaterial);
                Materials = list;
                SelectedMaterial = temporaryMaterial;
                warnings.Add("Profil bulunamadi: Secili malzeme gecici profil olarak olusturuldu.");
                warnings.Add("Profil bulunamadı: Seçili malzeme bu bilgisayarda yok.");
            }
        }

        if (!string.IsNullOrEmpty(project.SelectedMachineId))
        {
            SelectedMachine = Machines.FirstOrDefault(m => m.Id == project.SelectedMachineId);
            if (SelectedMachine == null && project.ProfileSnapshot?.Machine != null)
            {
                var restoredMachine = project.ProfileSnapshot.Machine;
                var list = Machines.ToList();
                if (!list.Any(m => m.Id == restoredMachine.Id))
                    list.Add(restoredMachine);
                Machines = list;
                SelectedMachine = restoredMachine;
                warnings.Add("Profil bulunamadı: Makine proje snapshot'ından yüklendi.");
            }
            else if (SelectedMachine == null)
            {
                var temporaryMachine = new MachineProfile
                {
                    Id = project.SelectedMachineId,
                    Name = "Temporary Machine (Recovered)",
                    Notes = "Project portability recovery profile. Replace with a real machine profile before production."
                };
                var list = Machines.ToList();
                list.Add(temporaryMachine);
                Machines = list;
                SelectedMachine = temporaryMachine;
                warnings.Add("Profil bulunamadi: Secili makine gecici profil olarak olusturuldu.");
                warnings.Add("Profil bulunamadı: Seçili makine bu bilgisayarda yok.");
            }
        }

        return warnings;
    }

    private void EnsureDefaultLayers()
    {
        if (Layers.Count > 0) return;

        Layers.Add(CreateLayer("Kesim", LayerType.Cut, "#4EC9B0", 80, 20, 1, 0));
        Layers.Add(CreateLayer("Gravür", LayerType.Engrave, "#D7BA7D", 35, 180, 1, 1));
        Layers.Add(CreateLayer("Markalama", LayerType.Mark, "#9CDCFE", 20, 250, 1, 2));
        Layers.Add(CreateLayer("Referans", LayerType.Reference, "#808080", 0, 0, 1, 3));
        SelectedLayer = Layers.FirstOrDefault(l => l.Type == LayerType.Cut) ?? Layers.FirstOrDefault();
        OnPropertyChanged(nameof(LayerCount));
    }

    private static LayerModel CreateLayer(string name, LayerType type, string color, double power, double speed, int passCount, int order) => new()
    {
        Name = name,
        Type = type,
        Color = color,
        Power = power,
        Speed = speed,
        PassCount = passCount,
        Order = order
    };

    private LayerModel DefaultCutLayer()
    {
        EnsureDefaultLayers();
        return Layers.FirstOrDefault(l => l.Type == LayerType.Cut)
            ?? Layers.OrderBy(l => l.Order).First();
    }

    private LayerModel EnsureLayerForName(string? layerName)
    {
        string normalized = string.IsNullOrWhiteSpace(layerName) || layerName == "0"
            ? DefaultCutLayer().Name
            : layerName.Trim();

        var existing = Layers.FirstOrDefault(l => string.Equals(l.Name, normalized, StringComparison.CurrentCultureIgnoreCase));
        if (existing != null) return existing;

        var layer = CreateLayer(normalized, LayerType.Cut, NextLayerColor(), 80, 20, 1, Layers.Count);
        Layers.Add(layer);
        OnPropertyChanged(nameof(LayerCount));
        SetDirty();
        return layer;
    }

    private string NextLayerColor()
    {
        string[] colors = { "#4EC9B0", "#D7BA7D", "#9CDCFE", "#CE9178", "#C586C0", "#B5CEA8" };
        return colors[Layers.Count % colors.Length];
    }

    private void NormalizePartLayers()
    {
        EnsureDefaultLayers();
        foreach (var part in Parts)
        {
            var layer = string.IsNullOrWhiteSpace(part.LayerId)
                ? EnsureLayerForName(part.LayerName)
                : Layers.FirstOrDefault(l => l.Id == part.LayerId) ?? EnsureLayerForName(part.LayerName);

            part.LayerId = layer.Id;
            part.LayerName = layer.Name;
        }

        RefreshFilteredParts();
        RequestDrawPreview?.Invoke();
    }

    public LayerModel? GetLayerForPart(PartModel part)
    {
        if (part == null) return null;
        return Layers.FirstOrDefault(l => l.Id == part.LayerId)
            ?? Layers.FirstOrDefault(l => string.Equals(l.Name, part.LayerName, StringComparison.CurrentCultureIgnoreCase))
            ?? DefaultCutLayer();
    }

    public bool IsPartVisible(PartModel part) => GetLayerForPart(part)?.IsVisible != false;

    public bool IsPartLocked(PartModel part) => GetLayerForPart(part)?.IsLocked == true;

    public bool IsPartSelectable(PartModel part) => IsPartVisible(part) && !IsPartLocked(part);

    private List<PartModel> EditableSelectedParts() => _selectedParts.Where(IsPartSelectable).ToList();

    private async Task OpenDxf()
    {
        var dlg = new OpenFileDialog { Filter = AppConstants.DxfFilter };
        if (dlg.ShowDialog() != true) return;

        BeginBusy("DXF yükleniyor...");
        var progress = CreateProgress("DXF yükleniyor...");

        try
        {
            var import = await ImportWorkflowService.ImportDxfAsync(
                dlg.FileName,
                ImportAutoDetect,
                ImportSourceUnit,
                ImportManualScale,
                progress);
            var result = import.ImportResult;

            if (!result.Success)
            {
                StatusText = $"Hata: {string.Join(" | ", result.Errors)}";
                return;
            }

            PushUndo("DXF Yükle");

            FilePath = result.FilePath;
            FileName = result.FileName;
            Parts = new ObservableCollection<PartModel>(result.Parts);
            NormalizePartLayers();
            TotalPartsArea = result.TotalArea;
            NestResult = null;
            _selectedParts.Clear();

            // Populate import verification
            LastImportUnitInfo = result.UnitInfo;
            ImportTotalBoundingWidth = result.TotalBoundingWidth;
            ImportTotalBoundingHeight = result.TotalBoundingHeight;
            ImportPartCount = result.Parts.Count;
            ImportVerificationText = import.VerificationText;
            ImportReferenceDimText = string.Empty;

            if (Parts.Count == 0)
            {
                StatusText = "Parça bulunamadı";
            }
            else
            {
                string warnings = result.Warnings.Count > 0 ? $" ({string.Join(", ", result.Warnings)})" : "";
                StatusText = $"{Parts.Count} parça yüklendi: {result.FileName} ({result.TotalArea:F0} mm²){warnings}";
            }

            NotifySelectionChanged();
            SetDirty();
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task OpenDxfWithOptions()
    {
        await OpenDxf();
    }

    private void ApplySuggestedScale()
    {
        if (string.IsNullOrWhiteSpace(ImportReferenceDimText) || Parts.Count == 0)
            return;
        if (!double.TryParse(ImportReferenceDimText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double refMm))
            return;
        double currentWidth = ImportTotalBoundingWidth;
        if (currentWidth <= 0) return;
        double suggested = refMm / currentWidth;
        if (suggested <= 0) return;

        ImportManualScale = suggested;
        ImportSourceUnit = DxfUnit.Millimeters;
        ImportAutoDetect = false;
        StatusText = $"Scale faktörü {suggested:F4} olarak ayarlandı. DXF'yi yeniden açın.";
    }

    private async Task RunBenchmark()
    {
        if (Parts.Count == 0)
        {
            StatusText = "Benchmark için önce DXF yükleyin.";
            return;
        }

        var nestableParts = Parts
            .Where(p => IsPartVisible(p) && !IsPartLocked(p) && GetLayerForPart(p)?.Type != LayerType.Reference)
            .ToList();
        if (nestableParts.Count == 0)
        {
            StatusText = "Benchmark için uygun parça yok.";
            return;
        }

        BeginBusy("Benchmark çalıştırılıyor...");
        var progress = CreateProgress("Benchmark çalıştırılıyor...");
        BenchmarkFreeRectResult = null;
        BenchmarkPolygonResult = null;
        BenchmarkIrregularResult = null;

        try
        {
            var benchmark = await NestingWorkflowService.RunBenchmarkAsync(nestableParts, Plate, Settings, progress);
            BenchmarkFreeRectResult = benchmark.FreeRectangle;
            BenchmarkPolygonResult = benchmark.PolygonCollision;
            BenchmarkIrregularResult = benchmark.Irregular;
            BenchmarkSummaryText = benchmark.SummaryText;
            var best = benchmark.Best;
            StatusText = $"Benchmark tamamlandı. En iyi: {best.AlgorithmName} (%{best.Efficiency:F1}, {best.NestingTimeMs} ms)";
        }
        catch (Exception ex)
        {
            StatusText = $"Benchmark hatası: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task RunNesting()
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

        var nestableParts = Parts
            .Where(p => IsPartVisible(p) && !IsPartLocked(p) && GetLayerForPart(p)?.Type != LayerType.Reference)
            .ToList();
        if (nestableParts.Count == 0)
        {
            MessageBox.Show("Nesting için Kesim, Gravür veya Markalama katmanında parça bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var oversizedParts = nestableParts.Where(p =>
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

        BeginBusy("Nesting çalıştırılıyor...");
        var progress = CreateProgress("Nesting çalıştırılıyor...");

        try
        {
            PushUndo("Nesting Çalıştır");

            NestResult = await NestingWorkflowService.RunNestingAsync(nestableParts, Plate, Settings, progress);

            string warningSuffix = NestResult.Warnings.Count > 0 ? $" | Uyarı: {NestResult.Warnings.Count}" : "";
            StatusText = $"Nesting tamamlandı ({NestResult.NestingTimeMs} ms): {NestResult.PlacedCount} yerleştirildi, {NestResult.UnplacedCount} yerleşemedi, {NestResult.PlateCount} plaka kullanıldı{warningSuffix}";
            RequestDrawPreview?.Invoke();
            SetDirty();
        }
        catch (Exception ex)
        {
            StatusText = $"Nesting hatası: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private void ScaleSelected()
    {
        if (_selectedParts.Count == 0)
        {
            MessageBox.Show("Önce parça seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(ScaleFactorText, out double factor) || factor <= 0)
        {
            MessageBox.Show("Geçerli bir ölçek çarpanı girin (0'dan büyük).", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Math.Abs(factor - 1.0) < 1e-9)
        {
            StatusText = "Ölçek çarpanı 1, değişim yok.";
            return;
        }

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        PushUndo($"Ölçekle ({editable.Count} parça)");

        foreach (var part in editable)
        {
            part.Geometry.Scale(factor);
            part.ScaleFactor *= factor;
            part.IsScaled = Math.Abs(part.ScaleFactor - 1.0) > 1e-9;
        }

        TotalPartsArea = Parts.Sum(p => p.Area);
        NestResult = null;
        RequestDrawPreview?.Invoke();

        StatusText = $"{editable.Count} parça {factor}x büyütüldü";

        RefreshFilteredParts();
        OnPropertyChanged(nameof(HasScaledParts));
        NotifySelectionChanged();
        SetDirty();
    }

    private void ScaleAllParts()
    {
        if (Parts.Count == 0)
        {
            MessageBox.Show("Önce DXF dosyası yükleyin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(ScaleFactorText, out double factor) || factor <= 0)
        {
            MessageBox.Show("Geçerli bir ölçek çarpanı girin (0'dan büyük).", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Math.Abs(factor - 1.0) < 1e-9)
        {
            StatusText = "Ölçek çarpanı 1, değişim yok.";
            return;
        }

        var editable = Parts.Where(IsPartSelectable).ToList();
        if (editable.Count == 0) return;

        PushUndo("Tümünü Ölçekle");

        foreach (var part in editable)
        {
            part.Geometry.Scale(factor);
            part.ScaleFactor *= factor;
            part.IsScaled = Math.Abs(part.ScaleFactor - 1.0) > 1e-9;
        }

        TotalPartsArea = Parts.Sum(p => p.Area);
        NestResult = null;
        RequestDrawPreview?.Invoke();

        StatusText = $"Tüm parçalar {factor}x büyütüldü ({editable.Count} parça)";

        RefreshFilteredParts();
        OnPropertyChanged(nameof(HasScaledParts));
        NotifySelectionChanged();
        SetDirty();
    }

    private void ClearAll()
    {
        PushUndo("Temizle");

        Parts = new ObservableCollection<PartModel>();
        _selectedParts.Clear();
        NestResult = null;
        FilePath = string.Empty;
        FileName = string.Empty;
        TotalPartsArea = 0;
        StatusText = "Temizlendi";
        NotifySelectionChanged();
        SetDirty();
    }

    private void DeleteSelected()
    {
        if (_selectedParts.Count == 0) return;

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        int deletedCount = editable.Count;
        PushUndo($"Sil ({deletedCount} parça)");

        foreach (var part in editable)
        {
            if (_nestResult != null)
            {
                var toRemove = _nestResult.Placed.Where(p => p.PartName == part.Name).ToList();
                foreach (var r in toRemove)
                    _nestResult.Placed.Remove(r);
            }
            Parts.Remove(part);
        }

        _selectedParts.Clear();
        TotalPartsArea = Parts.Sum(p => p.Area);
        StatusText = $"{deletedCount} parça silindi, {Parts.Count} kaldı";
        RefreshFilteredParts();
        NotifySelectionChanged();
        SetDirty();
    }

    private void RotateSelected90()
    {
        if (_selectedParts.Count == 0) return;

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        PushUndo($"Döndür 90° ({editable.Count} parça)");

        foreach (var part in editable)
            part.Geometry.Rotate90AroundCenter();

        TotalPartsArea = Parts.Sum(p => p.Area);
        NestResult = null;

        StatusText = $"{editable.Count} parça 90° döndürüldü";
        RefreshFilteredParts();
        NotifySelectionChanged();
        SetDirty();
    }

    private void MirrorSelectedX()
    {
        if (_selectedParts.Count == 0) return;

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        PushUndo($"Aynala X ({editable.Count} parça)");

        foreach (var part in editable)
            part.Geometry.MirrorX();

        NestResult = null;

        StatusText = $"{editable.Count} parça X ekseniyle aynalandı";
        NotifySelectionChanged();
        SetDirty();
    }

    private void MirrorSelectedY()
    {
        if (_selectedParts.Count == 0) return;

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        PushUndo($"Aynala Y ({editable.Count} parça)");

        foreach (var part in editable)
            part.Geometry.MirrorY();

        NestResult = null;

        StatusText = $"{editable.Count} parça Y ekseniyle aynalandı";
        NotifySelectionChanged();
        SetDirty();
    }

    private void SelectAll()
    {
        _selectedParts.Clear();
        foreach (var p in Parts.Where(IsPartSelectable))
            _selectedParts.Add(p);
        NotifySelectionChanged();
    }

    public void DeselectAll()
    {
        _selectedParts.Clear();
        NotifySelectionChanged();
    }

    public void BeginMoveSelected()
    {
        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;
        PushUndo($"Taşı ({editable.Count} parça)");
        SetDirty();
    }

    public void MoveSelected(double dx, double dy, bool requestDraw = true)
    {
        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        foreach (var part in editable)
            part.Geometry.Move(dx, dy);

        if (NestResult != null && NestResult.Plates.Count > 0)
        {
            foreach (var part in editable)
            {
                var placement = NestResult.Placed.FirstOrDefault(p => p.PartName == part.Name);
                if (placement != null)
                {
                    placement.TransformedGeometry.Move(dx, dy);
                    placement.X = placement.TransformedGeometry.Bounds.MinX;
                    placement.Y = placement.TransformedGeometry.Bounds.MinY;
                }
            }
        }
        else
        {
            NestResult = null;
        }

        TotalPartsArea = Parts.Sum(p => p.Area);
        if (requestDraw)
            RequestDrawPreview?.Invoke();
        SetDirty();
    }

    public void SelectPart(PartModel part)
    {
        if (!Parts.Contains(part) || !IsPartSelectable(part)) return;
        _selectedParts.Clear();
        _selectedParts.Add(part);
        NotifySelectionChanged();
    }

    private void ApplyProperties()
    {
        if (_selectedParts.Count == 0) return;

        if (!double.TryParse(PropertyXText, out double targetX) ||
            !double.TryParse(PropertyYText, out double targetY) ||
            !TryParsePositive(PropertyWidthText, out double targetWidth) ||
            !TryParsePositive(PropertyHeightText, out double targetHeight) ||
            !double.TryParse(PropertyRotationText, out double targetRotation))
        {
            StatusText = "Properties: Geçerli sayısal değerler girin";
            return;
        }

        var bounds = GetSelectionBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        PushUndo($"Properties uygula ({editable.Count} parça)");

        double selectionCenterX = (bounds.MinX + bounds.MaxX) / 2.0;
        double selectionCenterY = (bounds.MinY + bounds.MaxY) / 2.0;
        double scaleX = targetWidth / bounds.Width;
        double scaleY = targetHeight / bounds.Height;
        if (Math.Abs(scaleX - 1.0) > 1e-9 || Math.Abs(scaleY - 1.0) > 1e-9)
        {
            foreach (var part in editable)
            {
                part.Geometry.ScaleAround(selectionCenterX, selectionCenterY, scaleX, scaleY);
                part.IsScaled = true;
            }
        }

        if (Math.Abs(targetRotation) > 1e-9)
        {
            var rotatedBounds = GetSelectionBounds();
            double rotateCenterX = (rotatedBounds.MinX + rotatedBounds.MaxX) / 2.0;
            double rotateCenterY = (rotatedBounds.MinY + rotatedBounds.MaxY) / 2.0;
            foreach (var part in editable)
                part.Geometry.RotateAround(rotateCenterX, rotateCenterY, targetRotation);
        }

        var finalBounds = GetSelectionBounds();
        double moveDx = targetX - finalBounds.MinX;
        double moveDy = targetY - finalBounds.MinY;
        foreach (var part in editable)
            part.Geometry.Move(moveDx, moveDy);

        TotalPartsArea = Parts.Sum(p => p.Area);
        NestResult = null;
        RefreshFilteredParts();
        StatusText = $"Properties uygulandı: {editable.Count} parça";
        NotifySelectionChanged();
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private void AddLayer()
    {
        PushUndo("Katman ekle");
        var layer = CreateLayer($"Katman {Layers.Count + 1}", LayerType.Cut, NextLayerColor(), 80, 20, 1, Layers.Count);
        Layers.Add(layer);
        SelectedLayer = layer;
        OnPropertyChanged(nameof(LayerCount));
        StatusText = $"Katman eklendi: {layer.Name}";
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private void DeleteSelectedLayer()
    {
        if (SelectedLayer == null || Layers.Count <= 1) return;

        var removed = SelectedLayer;
        var fallback = Layers.FirstOrDefault(l => l.Id != removed.Id && l.Type == LayerType.Cut)
            ?? Layers.FirstOrDefault(l => l.Id != removed.Id);
        if (fallback == null) return;

        PushUndo($"Katman sil: {removed.Name}");
        foreach (var part in Parts.Where(p => p.LayerId == removed.Id))
        {
            part.LayerId = fallback.Id;
            part.LayerName = fallback.Name;
        }

        var removedOps = _operations.Where(o => o.LayerId == removed.Id).ToList();
        foreach (var op in removedOps)
        {
            op.LayerId = fallback.Id;
        }

        Layers.Remove(removed);
        int order = 0;
        foreach (var layer in Layers.OrderBy(l => l.Order))
            layer.Order = order++;

        SelectedLayer = fallback;
        RefreshFilteredParts();
        OnPropertyChanged(nameof(LayerCount));
        OnPropertyChanged(nameof(Operations));
        StatusText = $"Katman silindi: {removed.Name}";
        NotifySelectionChanged();
        SetDirty();
    }

    private void ApplyLayerProperties()
    {
        if (SelectedLayer == null) return;
        if (!double.TryParse(LayerPowerText, out double power) ||
            !double.TryParse(LayerSpeedText, out double speed) ||
            !int.TryParse(LayerPassCountText, out int passCount) ||
            passCount <= 0 ||
            string.IsNullOrWhiteSpace(LayerNameText))
        {
            StatusText = "Katman: Geçerli ad, güç, hız ve pas değerleri girin";
            return;
        }

        PushUndo($"Katman güncelle: {SelectedLayer.Name}");
        SelectedLayer.Name = LayerNameText.Trim();
        SelectedLayer.Type = SelectedLayerType;
        SelectedLayer.Color = string.IsNullOrWhiteSpace(LayerColorText) ? SelectedLayer.Color : LayerColorText.Trim();
        SelectedLayer.Power = Math.Max(0, power);
        SelectedLayer.Speed = Math.Max(0, speed);
        SelectedLayer.PassCount = passCount;

        foreach (var part in Parts.Where(p => p.LayerId == SelectedLayer.Id))
            part.LayerName = SelectedLayer.Name;

        RefreshFilteredParts();
        StatusText = $"Katman güncellendi: {SelectedLayer.Name}";
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private void AssignSelectedToLayer()
    {
        if (SelectedLayer == null || _selectedParts.Count == 0) return;

        var editable = EditableSelectedParts();
        if (editable.Count == 0) return;

        PushUndo($"Katmana ata: {SelectedLayer.Name}");
        foreach (var part in editable)
        {
            part.LayerId = SelectedLayer.Id;
            part.LayerName = SelectedLayer.Name;
        }

        NestResult = null;
        RefreshFilteredParts();
        StatusText = $"{editable.Count} parça {SelectedLayer.Name} katmanına atandı";
        NotifySelectionChanged();
        SetDirty();
    }

    private void ToggleLayerVisibility(LayerModel? layer)
    {
        if (layer == null) return;
        PushUndo($"Katman görünürlük: {layer.Name}");
        layer.IsVisible = !layer.IsVisible;
        if (!layer.IsVisible)
            RemoveSelectedPartsInLayer(layer);
        StatusText = $"{layer.Name}: {(layer.IsVisible ? "görünür" : "gizli")}";
        NotifyLayerStateChanged();
        SetDirty();
    }

    private void ToggleLayerLock(LayerModel? layer)
    {
        if (layer == null) return;
        PushUndo($"Katman kilit: {layer.Name}");
        layer.IsLocked = !layer.IsLocked;
        if (layer.IsLocked)
            RemoveSelectedPartsInLayer(layer);
        StatusText = $"{layer.Name}: {(layer.IsLocked ? "kilitli" : "açık")}";
        NotifyLayerStateChanged();
        SetDirty();
    }

    private void RemoveSelectedPartsInLayer(LayerModel layer)
    {
        foreach (var part in _selectedParts.Where(p => p.LayerId == layer.Id).ToList())
            _selectedParts.Remove(part);
    }

    private void NotifyLayerStateChanged()
    {
        RemoveNonSelectablePartsFromSelection();
        OnPropertyChanged(nameof(Layers));
        RefreshFilteredParts();
        NotifySelectionChanged();
        OnPropertyChanged(nameof(CanExportDxf));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        RequestDrawPreview?.Invoke();
    }

    private void RemoveNonSelectablePartsFromSelection()
    {
        foreach (var part in _selectedParts.Where(p => !IsPartSelectable(p)).ToList())
            _selectedParts.Remove(part);
    }

    public void ToggleSelection(PartModel part, bool ctrl, bool shift)
    {
        if (!IsPartSelectable(part)) return;

        if (shift && _selectedParts.Count > 0)
        {
            int lastIdx = Parts.IndexOf(_selectedParts[^1]);
            int currentIdx = Parts.IndexOf(part);
            if (lastIdx >= 0 && currentIdx >= 0)
            {
                int start = Math.Min(lastIdx, currentIdx);
                int end = Math.Max(lastIdx, currentIdx);
                _selectedParts.Clear();
                for (int i = start; i <= end; i++)
                {
                    if (IsPartSelectable(Parts[i]))
                        _selectedParts.Add(Parts[i]);
                }
            }
        }
        else if (ctrl)
        {
            if (_selectedParts.Contains(part))
                _selectedParts.Remove(part);
            else
                _selectedParts.Add(part);
        }
        else
        {
            _selectedParts.Clear();
            _selectedParts.Add(part);
        }
        NotifySelectionChanged();
    }

    public void SelectByRect(List<PartModel> partsInRect, bool ctrl)
    {
        if (!ctrl)
            _selectedParts.Clear();

        foreach (var p in partsInRect)
        {
            if (IsPartSelectable(p) && !_selectedParts.Contains(p))
                _selectedParts.Add(p);
        }
        NotifySelectionChanged();
    }

    public PartModel? HitTest(double worldX, double worldY)
    {
        if (NestResult != null && NestResult.Plates.Count > 0)
            return HitTestNested(worldX, worldY);

        var plate = Plate;
        for (int i = Parts.Count - 1; i >= 0; i--)
        {
            var part = Parts[i];
            if (!IsPartSelectable(part)) continue;
            var b = part.Geometry.Bounds;
            if (b.Width <= 0 && b.Height <= 0) continue;
            if (worldX >= b.MinX && worldX <= b.MaxX &&
                worldY >= (plate.Height - b.MaxY) && worldY <= (plate.Height - b.MinY))
                return part;
        }
        return null;
    }

    private PartModel? HitTestNested(double worldX, double worldY)
    {
        var nr = NestResult!;

        double gapMm = 20;
        double plateOffsetX = 0;

        for (int pi = 0; pi < nr.Plates.Count; pi++)
        {
            var plate = nr.Plates[pi];

            if (worldX >= plateOffsetX && worldX <= plateOffsetX + plate.Width &&
                worldY >= 0 && worldY <= plate.Height)
            {
                double localX = worldX - plateOffsetX;
                double localY = plate.Height - worldY;

                var placements = nr.Placed
                    .Where(pl => pl.PlateIndex == pi)
                    .Reverse();

                foreach (var placement in placements)
                {
                    if (!IsPartSelectable(placement.Part)) continue;
                    var b = placement.TransformedGeometry.Bounds;
                    if (b.Width <= 0 && b.Height <= 0) continue;
                    if (localX >= b.MinX && localX <= b.MaxX &&
                        localY >= b.MinY && localY <= b.MaxY)
                        return placement.Part;
                }
                break;
            }
            plateOffsetX += plate.Width + gapMm;
        }
        return null;
    }

    public List<PartModel> HitTestRect(double wx1, double wy1, double wx2, double wy2)
    {
        double minX = Math.Min(wx1, wx2);
        double maxX = Math.Max(wx1, wx2);
        double minY = Math.Min(wy1, wy2);
        double maxY = Math.Max(wy1, wy2);

        if (NestResult != null && NestResult.Plates.Count > 0)
            return HitTestRectNested(minX, minY, maxX, maxY);

        var plate = Plate;
        var result = new List<PartModel>();
        foreach (var part in Parts)
        {
            if (!IsPartSelectable(part)) continue;
            var b = part.Geometry.Bounds;
            if (b.Width <= 0 && b.Height <= 0) continue;
            double partScreenY1 = plate.Height - b.MaxY;
            double partScreenY2 = plate.Height - b.MinY;
            bool intersects = b.MaxX >= minX && b.MinX <= maxX && partScreenY2 >= minY && partScreenY1 <= maxY;
            if (intersects)
                result.Add(part);
        }
        return result;
    }

    private List<PartModel> HitTestRectNested(double minX, double minY, double maxX, double maxY)
    {
        var nr = NestResult!;
        var result = new List<PartModel>();
        double gapMm = 20;
        double plateOffsetX = 0;

        for (int pi = 0; pi < nr.Plates.Count; pi++)
        {
            var plate = nr.Plates[pi];
            double plateWorldX1 = plateOffsetX;
            double plateWorldX2 = plateOffsetX + plate.Width;
            double plateWorldY1 = 0;
            double plateWorldY2 = plate.Height;

            if (maxX < plateWorldX1 || minX > plateWorldX2 || maxY < plateWorldY1 || minY > plateWorldY2)
            {
                plateOffsetX += plate.Width + gapMm;
                continue;
            }

            var placements = nr.Placed.Where(pl => pl.PlateIndex == pi);


            foreach (var placement in placements)
            {
                if (!IsPartSelectable(placement.Part) || result.Contains(placement.Part)) continue;
                var b = placement.TransformedGeometry.Bounds;
                if (b.Width <= 0 && b.Height <= 0) continue;

                double pwX1 = plateOffsetX + b.MinX;
                double pwX2 = plateOffsetX + b.MaxX;
                double pwY1 = plate.Height - b.MaxY;
                double pwY2 = plate.Height - b.MinY;

                if (pwX2 >= minX && pwX1 <= maxX && pwY2 >= minY && pwY1 <= maxY)
                    result.Add(placement.Part);
            }

            plateOffsetX += plate.Width + gapMm;
        }
        return result;
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedArea));
        OnPropertyChanged(nameof(SelectedAreaText));
        OnPropertyChanged(nameof(SelectedPartInfo));
        OnPropertyChanged(nameof(HasSelectedParts));
        OnPropertyChanged(nameof(HasSingleSelection));
        OnPropertyChanged(nameof(SelectionWidthText));
        OnPropertyChanged(nameof(SelectionHeightText));
        OnPropertyChanged(nameof(SelectionAreaText));
        OnPropertyChanged(nameof(SelectionPerimeterText));
        OnPropertyChanged(nameof(SelectionTotalAreaText));
        OnPropertyChanged(nameof(CanExportDxf));
        SyncPropertyFieldsFromSelection();
        RequestDrawPreview?.Invoke();
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    private BoundingBox GetSelectionBounds()
    {
        if (_selectedParts.Count == 0)
            return new BoundingBox(0, 0, 0, 0);

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var part in _selectedParts)
        {
            var b = part.Geometry.Bounds;
            minX = Math.Min(minX, b.MinX);
            minY = Math.Min(minY, b.MinY);
            maxX = Math.Max(maxX, b.MaxX);
            maxY = Math.Max(maxY, b.MaxY);
        }

        return new BoundingBox(minX, minY, maxX, maxY);
    }

    public BoundingBox GetSelectionBoundsPublic() => GetSelectionBounds();

    public List<PartModel> HitTestRectFullyInside(double wx1, double wy1, double wx2, double wy2)
    {
        double minX = Math.Min(wx1, wx2);
        double maxX = Math.Max(wx1, wx2);
        double minY = Math.Min(wy1, wy2);
        double maxY = Math.Max(wy1, wy2);

        var selectRect = new BoundingBox(minX, minY, maxX, maxY);

        if (NestResult != null && NestResult.Plates.Count > 0)
            return HitTestRectFullyInsideNested(selectRect);

        var plate = Plate;
        var result = new List<PartModel>();
        foreach (var part in Parts)
        {
            if (!IsPartSelectable(part)) continue;
            var b = part.Geometry.Bounds;
            if (b.Width <= 0 && b.Height <= 0) continue;
            double partScreenY1 = plate.Height - b.MaxY;
            double partScreenY2 = plate.Height - b.MinY;
            var partRect = new BoundingBox(b.MinX, partScreenY1, b.MaxX, partScreenY2);
            if (selectRect.Contains(partRect))
                result.Add(part);
        }
        return result;
    }

    private List<PartModel> HitTestRectFullyInsideNested(BoundingBox selectRect)
    {
        var nr = NestResult!;
        var result = new List<PartModel>();
        double gapMm = 20;
        double plateOffsetX = 0;

        for (int pi = 0; pi < nr.Plates.Count; pi++)
        {
            var plate = nr.Plates[pi];
            foreach (var placement in nr.Placed.Where(pl => pl.PlateIndex == pi))
            {
                if (!IsPartSelectable(placement.Part) || result.Contains(placement.Part)) continue;
                var b = placement.TransformedGeometry.Bounds;
                if (b.Width <= 0 && b.Height <= 0) continue;

                double pwX1 = plateOffsetX + b.MinX;
                double pwX2 = plateOffsetX + b.MaxX;
                double pwY1 = plate.Height - b.MaxY;
                double pwY2 = plate.Height - b.MinY;
                var partRect = new BoundingBox(pwX1, pwY1, pwX2, pwY2);

                if (selectRect.Contains(partRect))
                    result.Add(placement.Part);
            }
            plateOffsetX += plate.Width + gapMm;
        }
        return result;
    }

    private void SyncPropertyFieldsFromSelection()
    {
        var b = GetSelectionBounds();
        PropertyXText = $"{b.MinX:F1}";
        PropertyYText = $"{b.MinY:F1}";
        PropertyWidthText = $"{Math.Max(0, b.Width):F1}";
        PropertyHeightText = $"{Math.Max(0, b.Height):F1}";
        PropertyRotationText = "0";
    }

    private void SyncLayerFieldsFromSelection()
    {
        if (SelectedLayer == null)
        {
            LayerNameText = string.Empty;
            LayerColorText = "#4EC9B0";
            LayerPowerText = "0";
            LayerSpeedText = "0";
            LayerPassCountText = "1";
            SelectedLayerType = LayerType.Cut;
            return;
        }

        LayerNameText = SelectedLayer.Name;
        LayerColorText = SelectedLayer.Color;
        LayerPowerText = $"{SelectedLayer.Power:F0}";
        LayerSpeedText = $"{SelectedLayer.Speed:F0}";
        LayerPassCountText = SelectedLayer.PassCount.ToString();
        SelectedLayerType = SelectedLayer.Type;
    }

    private void UpdatePlateValue(string text, Action<double> apply, Func<double, bool> isValid, string errorMessage, string propertyName)
    {
        if (!double.TryParse(text, out double value) || !isValid(value))
        {
            StatusText = errorMessage;
            OnPropertyChanged(propertyName);
            return;
        }

        apply(value);
        NestResult = null;
        StatusText = "Plaka ayarları güncellendi";
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(PlateWidthText));
        OnPropertyChanged(nameof(PlateHeightText));
        OnPropertyChanged(nameof(MarginText));
        OnPropertyChanged(nameof(GapText));
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private bool IsValidMargin(double margin)
        => margin >= 0 && margin * 2 < Plate.Width && margin * 2 < Plate.Height;

    private void UpdateHeightFromAspect(double width)
    {
        var b = GetSelectionBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        double height = width * b.Height / b.Width;
        if (Math.Abs(height - ParseOrZero(PropertyHeightText)) < 1e-6) return;
        _propertyHeightText = $"{height:F1}";
        OnPropertyChanged(nameof(PropertyHeightText));
    }

    private void UpdateWidthFromAspect(double height)
    {
        var b = GetSelectionBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        double width = height * b.Width / b.Height;
        if (Math.Abs(width - ParseOrZero(PropertyWidthText)) < 1e-6) return;
        _propertyWidthText = $"{width:F1}";
        OnPropertyChanged(nameof(PropertyWidthText));
    }

    private static bool TryParsePositive(string text, out double value)
        => double.TryParse(text, out value) && value > 0;

    private static double ParseOrZero(string text)
        => double.TryParse(text, out double value) ? value : 0;

    private void RefreshFilteredParts()
    {
        IEnumerable<PartModel> query = Parts;

        if (!string.IsNullOrWhiteSpace(PartSearchText))
        {
            string term = PartSearchText.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                p.LayerName.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                p.SourceFile.Contains(term, StringComparison.CurrentCultureIgnoreCase));
        }

        query = PartSortMode switch
        {
            "Alan" => query.OrderByDescending(p => p.Area),
            "Genişlik" => query.OrderByDescending(p => p.Width),
            "Yükseklik" => query.OrderByDescending(p => p.Height),
            _ => query.OrderBy(p => p.Name)
        };

        FilteredParts = new ObservableCollection<PartModel>(query);
    }

    private UndoSnapshot TakeSnapshot(string description)
    {
        var selIndices = _selectedParts
            .Select(p => Parts.IndexOf(p))
            .Where(i => i >= 0)
            .ToList();

        return new UndoSnapshot
        {
            Parts = Parts.Select(p => p.Clone()).ToList(),
            Layers = Layers.Select(l => l.Clone()).ToList(),
            Operations = Operations.Select(o => o.Clone()).ToList(),
            SelectedIndices = selIndices,
            SelectedLayerId = SelectedLayer?.Id,
            NestResult = NestResult,
            FilePath = FilePath,
            FileName = FileName,
            TotalPartsArea = TotalPartsArea,
            Description = description
        };
    }

    private void PushUndo(string description)
    {
        _undoStack.Push(TakeSnapshot(description));
        if (_undoStack.Count > MaxUndoDepth)
        {
            var temp = new Stack<UndoSnapshot>(_undoStack.Reverse().Skip(1));
            _undoStack.Clear();
            foreach (var s in temp) _undoStack.Push(s);
        }
        _redoStack.Clear();
        LastOperation = description;
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        SetDirty();
    }

    private void RestoreSnapshot(UndoSnapshot snap)
    {
        _isLoading = true;
        Layers = new ObservableCollection<LayerModel>(snap.Layers.Select(l => l.Clone()));
        Operations = new ObservableCollection<LaserOperation>(snap.Operations.Select(o => o.Clone()));
        EnsureDefaultLayers();
        Parts = new ObservableCollection<PartModel>(snap.Parts);
        _selectedParts.Clear();
        foreach (var idx in snap.SelectedIndices)
        {
            if (idx >= 0 && idx < Parts.Count)
                _selectedParts.Add(Parts[idx]);
        }
        TotalPartsArea = snap.TotalPartsArea;
        NestResult = snap.NestResult;
        if (NestResult != null) RemapNestResultPartReferences();
        FilePath = snap.FilePath ?? "";
        FileName = snap.FileName ?? "";
        SelectedLayer = Layers.FirstOrDefault(l => l.Id == snap.SelectedLayerId) ?? DefaultCutLayer();
        NormalizePartLayers();
        OnPropertyChanged(nameof(HasScaledParts));
        NotifySelectionChanged();
        _isLoading = false;
        SetDirty();
    }

    private async Task ExportDxf()
    {
        if (Parts.Count == 0)
        {
            MessageBox.Show("Önce DXF dosyası yükleyin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool hasNestResult = NestResult != null && NestResult.Placed.Count > 0;
        bool useNestResult = hasNestResult;
        bool exportUnplaced = false;

        if (!hasNestResult)
        {
            var confirm = MessageBox.Show(
                "Yerleşim bulunamadı. Parçalar orijinal konumlarıyla dışa aktarılsın mı?",
                "Orijinal Konum Export", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
                return;
            useNestResult = false;
        }
        else
        {
            if (NestResult!.Unplaced.Count > 0)
            {
                var confirm = MessageBox.Show(
                    $"{NestResult.Unplaced.Count} parça plakaya sığmadı. Bu parçalar plaka dışına dışa aktarılsın mı?",
                    "Sığmayan Parçalar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                exportUnplaced = confirm == MessageBoxResult.Yes;
            }
        }

        if (HasScaledParts && useNestResult)
        {
            var confirm = MessageBox.Show(
                "Bazı parçalar ölçeklenmiş durumda. Dışa aktarılan dosya mevcut (ölçekli) ölçüleri içerecektir.\n\nDevam etmek istiyor musunuz?",
                "Ölçekleme Uyarısı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var dlg = new SaveFileDialog { Filter = AppConstants.DxfSaveFilter, FileName = "nested_production.dxf" };
        if (dlg.ShowDialog() != true) return;

        BeginBusy("DXF dışa aktarılıyor...");
        var progress = CreateProgress("DXF dışa aktarılıyor...");

        try
        {
            var report = await ExportWorkflowService.ExportDxfAsync(
                dlg.FileName,
                Plate,
                NestResult,
                Parts.ToList(),
                SelectedParts.ToList(),
                Layers.ToList(),
                new DxfExportOptions
                {
                    ExportHiddenLayers = ExportHiddenLayers,
                    ExportReferenceLayer = ExportReferenceLayer,
                    SelectedOnly = ExportSelectedOnly,
                    ExportPlateBorders = ExportPlateBorders,
                    ExportUnplacedParts = exportUnplaced,
                    UseNestResult = useNestResult,
                    IncludeOperationOrder = true
                },
                Operations.ToList(),
                SelectedMaterial?.DisplayName,
                SelectedMachine?.Name,
                LastCostEstimate,
                progress);

            StatusText = $"Dışa aktarıldı: {Path.GetFileName(dlg.FileName)} | Rapor oluşturuldu.";
            
            if (File.Exists(report.ReportPath))
            {
                var viewReport = MessageBox.Show(
                    "Export başarıyla tamamlandı. Export raporunu görmek ister misiniz?",
                    "Export Başarılı", MessageBoxButton.YesNo, MessageBoxImage.Information);
                
                if (viewReport == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(report.ReportPath) { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "DXF Export Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"Export hatası: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private void Undo()
    {
        if (!CanUndo) return;
        var current = TakeSnapshot("current");
        var prev = _undoStack.Pop();
        _redoStack.Push(current);
        RestoreSnapshot(prev);
        LastOperation = $"Geri Al: {prev.Description}";
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void Redo()
    {
        if (!CanRedo) return;
        var current = TakeSnapshot("current");
        var next = _redoStack.Pop();
        _undoStack.Push(current);
        RestoreSnapshot(next);
        LastOperation = $"İleri: {next.Description}";
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    // --- Material / Machine ---

    private void LoadMaterialProfiles()
    {
        Materials = MaterialProfileService.LoadMaterials();
        Machines = MaterialProfileService.LoadMachines();
        MaterialSettings = MaterialProfileService.LoadSettings();
    }

    private void ReloadMaterials()
    {
        Materials = MaterialProfileService.LoadMaterials();
        Machines = MaterialProfileService.LoadMachines();
        MaterialSettings = MaterialProfileService.LoadSettings();
        StatusText = $"Malzemeler yenilendi: {Materials.Count} malzeme, {Machines.Count} makine";
    }

    public void ApplyMaterialSettingsToCurrentOperation()
    {
        if (SelectedOperation == null) { StatusText = "Uyarı: Önce bir operasyon seçin."; return; }
        if (SelectedMaterial == null) { StatusText = "Uyarı: Önce bir malzeme seçin."; return; }
        if (SelectedMachine == null) { StatusText = "Uyarı: Önce bir makine seçin."; return; }

        var setting = MaterialProfileService.FindSetting(
            MaterialSettings, SelectedMaterial.Id, SelectedMachine.Id, SelectedOperation.Type);
        if (setting == null) return;

        SelectedOperation.Power = setting.Power;
        SelectedOperation.Speed = setting.Speed;
        SelectedOperation.PassCount = setting.PassCount;

        SyncOperationFieldsFromSelection();
        OnPropertyChanged(nameof(Operations));
        StatusText = $"Güç/Hız/Pas önerisi uygulandı: %{setting.Power}, {setting.Speed} mm/s, {setting.PassCount} pas";
        SetDirty();
    }

    private void AddMaterial()
    {
        var dlg = new System.Windows.Window
        {
            Title = "Yeni Malzeme",
            Width = 350, Height = 300,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current.MainWindow
        };
        // Simple approach: add a blank material and let the user edit in-app
        var mat = new MaterialProfile
        {
            Name = "Yeni Malzeme",
            Category = "Diğer",
            ThicknessMm = 3
        };
        var list = Materials.ToList();
        list.Add(mat);
        MaterialProfileService.SaveMaterials(list);
        Materials = list;
        SelectedMaterial = mat;
        StatusText = $"Yeni malzeme eklendi: {mat.DisplayName}";
    }

    private void DeleteMaterial()
    {
        if (SelectedMaterial == null) return;
        var list = Materials.ToList();
        list.Remove(SelectedMaterial);
        MaterialProfileService.SaveMaterials(list);
        Materials = list;
        SelectedMaterial = Materials.FirstOrDefault();
        StatusText = "Malzeme silindi";
    }

    private void AddMachine()
    {
        var machine = new MachineProfile
        {
            Name = "Yeni Makine",
            Manufacturer = "Generic",
            LaserType = "CO2"
        };
        var list = Machines.ToList();
        list.Add(machine);
        MaterialProfileService.SaveMachines(list);
        Machines = list;
        SelectedMachine = machine;
        StatusText = $"Yeni makine eklendi: {machine.Name}";
    }

    private void DeleteMachine()
    {
        if (SelectedMachine == null) return;
        var list = Machines.ToList();
        list.Remove(SelectedMachine);
        MaterialProfileService.SaveMachines(list);
        Machines = list;
        SelectedMachine = Machines.FirstOrDefault();
        StatusText = "Makine silindi";
    }

    private void SaveMaterialChanges()
    {
        if (SelectedMaterial == null) return;
        var list = Materials.ToList();
        int idx = list.FindIndex(m => m.Id == SelectedMaterial.Id);
        if (idx >= 0)
        {
            list[idx] = SelectedMaterial;
            MaterialProfileService.SaveMaterials(list);
        }
        Materials = list;
        OnPropertyChanged(nameof(SelectedMaterial));
        OnPropertyChanged(nameof(CostMaterialName));
        StatusText = $"Malzeme bilgileri kaydedildi: {SelectedMaterial.DisplayName}";
        SetDirty();
    }

    // --- Cost Estimation ---

    private async Task CalculateCost()
    {
        var validation = CostWorkflowService.ValidateInputs(Parts, SelectedMaterial, SelectedMachine);
        if (!validation.IsValid)
        {
            StatusText = validation.Message;
            return;
        }

        if (!validation.HasUnitPrice)
            StatusText = validation.Message;

        var settings = CostWorkflowService.BuildCalculationSettings(
            CostSettings,
            CostCurrency,
            CostProfitMarginPercent,
            CostVatPercent);

        BeginBusy("Maliyet hesaplanıyor...");
        var progress = CreateProgress("Maliyet hesaplanıyor...");

        try
        {
            var estimate = await CostWorkflowService.CalculateAsync(
                ProjectName,
                SelectedMaterial!,
                SelectedMachine!,
                Parts.ToList(),
                Layers.ToList(),
                Operations.ToList(),
                NestResult,
                Plate.Width,
                Plate.Height,
                settings,
                progress);

            LastCostEstimate = estimate;
            CostCurrency = estimate.Currency;
            StatusText = CostWorkflowService.BuildStatus(estimate);
            SetDirty();
        }
        catch (Exception ex)
        {
            StatusText = $"Maliyet hatası: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private void CopyQuotation()
    {
        if (LastCostEstimate == null) return;
        try
        {
            string text = CostWorkflowService.GenerateQuotation(LastCostEstimate);
            System.Windows.Clipboard.SetText(text);
            StatusText = "Teklif özeti panoya kopyalandı";
        }
        catch (Exception ex)
        {
            StatusText = $"Kopyalama hatası: {ex.Message}";
        }
    }

    private async Task ExportQuotationPdf()
    {
        await ExportPdf(PdfReportType.Quotation);
    }

    private async Task ExportProductionPdf()
    {
        await ExportPdf(PdfReportType.Production);
    }

    private async Task ExportPdf(PdfReportType reportType)
    {
        if (!ValidatePdfExportInputs())
            return;

        string defaultName = reportType == PdfReportType.Quotation
            ? $"{ProjectName}_quotation.pdf"
            : $"{ProjectName}_production_report.pdf";

        string initialDirectory = PdfReportSettings.LastExportDirectory;
        if (!Directory.Exists(initialDirectory))
            initialDirectory = !string.IsNullOrEmpty(LastSavedPath)
                ? Path.GetDirectoryName(LastSavedPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var dlg = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = SanitizeFileName(defaultName),
            InitialDirectory = initialDirectory
        };

        if (dlg.ShowDialog() != true)
            return;

        BeginBusy("PDF oluşturuluyor...");
        var progress = CreateProgress("PDF oluşturuluyor...");

        try
        {
            await ExportWorkflowService.CreatePdfAsync(
                dlg.FileName,
                reportType,
                ProjectName,
                CompanyProfile,
                SelectedMaterial!,
                SelectedMachine!,
                Plate,
                NestResult!,
                LastCostEstimate!,
                Operations.ToList(),
                progress);

            if (reportType == PdfReportType.Quotation)
                PdfReportSettings.LastQuotationPdfPath = dlg.FileName;
            else
                PdfReportSettings.LastProductionReportPdfPath = dlg.FileName;

            PdfReportSettings.LastExportDirectory = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
            PdfReportSettings.LastReportType = reportType.ToString();
            StatusText = $"PDF oluşturuldu: {Path.GetFileName(dlg.FileName)}";
            SetDirty();

            var open = MessageBox.Show("PDF oluşturuldu. Dosyayı açmak ister misiniz?", "PDF Export", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (open == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF oluşturulamadı: {ex.Message}", "PDF Export Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"PDF hatası: {ex.Message}";
        }
        finally
        {
            EndBusy();
        }
    }

    private bool ValidatePdfExportInputs()
    {
        var validation = ExportWorkflowService.ValidatePdfInputs(Parts, NestResult, SelectedMaterial, SelectedMachine, LastCostEstimate);
        if (validation.IsValid) return true;

        MessageBox.Show(validation.Message, "PDF Export", MessageBoxButton.OK, MessageBoxImage.Warning);
        StatusText = validation.StatusText;
        return false;
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return fileName;
    }

    // --- Operation Management ---

    private void EnsureDefaultOperations()
    {
        if (_operations.Count > 0) return;

        foreach (var layer in _layers.OrderBy(l => l.Order))
        {
            var opType = layer.Type switch
            {
                LayerType.Cut => OperationType.CutOuter,
                LayerType.Engrave => OperationType.Engrave,
                LayerType.Mark => OperationType.Mark,
                LayerType.Reference => OperationType.Reference,
                _ => OperationType.CutOuter
            };

            _operations.Add(new LaserOperation
            {
                Name = $"{layer.Name} - {GetOperationDisplayName(opType)}",
                LayerId = layer.Id,
                LayerName = layer.Name,
                Color = layer.Color,
                Type = opType,
                Power = layer.Power,
                Speed = layer.Speed,
                PassCount = layer.PassCount,
                Priority = _operations.Count,
                Enabled = true
            });
        }

        OnPropertyChanged(nameof(Operations));
        OnPropertyChanged(nameof(OperationCount));
    }

    private static OperationType? MapLayerNameToOperationType(string layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName)) return null;
        string lower = layerName.ToLowerInvariant();
        if (lower.Contains("engrave") || lower.Contains("gravür") || lower.Contains("gravur") || lower.Contains("kazıma")) return OperationType.Engrave;
        if (lower.Contains("mark") || lower.Contains("işaret") || lower.Contains("isaret")) return OperationType.Mark;
        if (lower.Contains("reference") || lower.Contains("referans")) return OperationType.Reference;
        if (lower.Contains("cut") || lower.Contains("kesim") || lower.Contains("iç") || lower.Contains("ic") || lower.Contains("inner") || lower.Contains("dış") || lower.Contains("dis") || lower.Contains("outer")) return null;
        return null;
    }

    private static string GetOperationDisplayName(OperationType type) => type switch
    {
        OperationType.Engrave => "Gravür",
        OperationType.Mark => "Markalama",
        OperationType.CutInner => "İç Kesim",
        OperationType.CutOuter => "Dış Kesim",
        OperationType.Reference => "Referans",
        _ => type.ToString()
    };

    private void AddOperation()
    {
        PushUndo("Operasyon ekle");
        var layer = SelectedLayer ?? _layers.FirstOrDefault();
        var op = new LaserOperation
        {
            Name = $"Operasyon {_operations.Count + 1}",
            LayerId = layer?.Id ?? string.Empty,
            LayerName = layer?.Name ?? string.Empty,
            Color = layer?.Color ?? "#569CD6",
            Type = OperationType.CutOuter,
            Power = layer?.Power ?? 80,
            Speed = layer?.Speed ?? 20,
            PassCount = layer?.PassCount ?? 1,
            Priority = _operations.Count,
            Enabled = true
        };
        _operations.Add(op);
        SelectedOperation = op;
        OnPropertyChanged(nameof(Operations));
        OnPropertyChanged(nameof(OperationCount));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        StatusText = $"Operasyon eklendi: {op.Name}";
        SetDirty();
    }

    private void DeleteOperation()
    {
        if (SelectedOperation == null || _operations.Count <= 1) return;
        PushUndo($"Operasyon sil: {SelectedOperation.Name}");
        _operations.Remove(SelectedOperation);
        RenumberOperations();
        SelectedOperation = _operations.FirstOrDefault();
        OnPropertyChanged(nameof(Operations));
        OnPropertyChanged(nameof(OperationCount));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        StatusText = "Operasyon silindi";
        SetDirty();
    }

    private void MoveOperation(int direction)
    {
        if (SelectedOperation == null) return;
        int idx = _operations.IndexOf(SelectedOperation);
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= _operations.Count) return;

        PushUndo($"Operasyon taşı: {SelectedOperation.Name}");
        _operations.Move(idx, newIdx);
        RenumberOperations();
        OnPropertyChanged(nameof(Operations));
        StatusText = $"Operasyon sırası: {SelectedOperation.Priority + 1}";
        SetDirty();
    }

    internal void RenumberOperations()
    {
        for (int i = 0; i < _operations.Count; i++)
            _operations[i].Priority = i;
    }

    internal void MarkDirty() => SetDirty();

    private void AutoSuggestOperations()
    {
        PushUndo("Otomatik operasyon öner");
        _operations.Clear();

        foreach (var layer in _layers.OrderBy(l => l.Order))
        {
            var nameHint = MapLayerNameToOperationType(layer.Name);

            if (layer.Type == LayerType.Cut)
            {
                bool preferInner = nameHint == OperationType.CutInner ||
                    (layer.Name.Contains("iç", StringComparison.OrdinalIgnoreCase) ||
                     layer.Name.Contains("ic", StringComparison.OrdinalIgnoreCase) ||
                     layer.Name.Contains("inner", StringComparison.OrdinalIgnoreCase));

                if (!preferInner)
                {
                    _operations.Add(new LaserOperation
                    {
                        Name = $"{layer.Name} - Dış Kesim",
                        LayerId = layer.Id,
                        LayerName = layer.Name,
                        Color = layer.Color,
                        Type = OperationType.CutOuter,
                        Power = layer.Power,
                        Speed = layer.Speed,
                        PassCount = layer.PassCount,
                        Priority = _operations.Count,
                        Enabled = true
                    });
                }

                bool hasInnerCuts = Parts.Any(p =>
                    p.LayerId == layer.Id && IsInnerCutCandidate(p));

                if (hasInnerCuts || preferInner)
                {
                    _operations.Add(new LaserOperation
                    {
                        Name = $"{layer.Name} - İç Kesim",
                        LayerId = layer.Id,
                        LayerName = layer.Name,
                        Color = layer.Color,
                        Type = OperationType.CutInner,
                        Power = layer.Power * 0.9,
                        Speed = layer.Speed * 0.8,
                        PassCount = layer.PassCount + 1,
                        Priority = _operations.Count,
                        Enabled = true
                    });
                }
            }
            else
            {
                var opType = nameHint ?? (layer.Type switch
                {
                    LayerType.Engrave => OperationType.Engrave,
                    LayerType.Mark => OperationType.Mark,
                    LayerType.Reference => OperationType.Reference,
                    _ => OperationType.CutOuter
                });

                _operations.Add(new LaserOperation
                {
                    Name = $"{layer.Name} - {GetOperationDisplayName(opType)}",
                    LayerId = layer.Id,
                    LayerName = layer.Name,
                    Color = layer.Color,
                    Type = opType,
                    Power = layer.Power,
                    Speed = layer.Speed,
                    PassCount = layer.PassCount,
                    Priority = _operations.Count,
                    Enabled = true
                });
            }
        }

        if (_operations.Count == 0)
        {
            _operations.Add(new LaserOperation
            {
                Name = "Varsayılan Kesim",
                Type = OperationType.CutOuter,
                Priority = 0,
                Enabled = true
            });
        }

        SelectedOperation = _operations.FirstOrDefault();
        OnPropertyChanged(nameof(Operations));
        OnPropertyChanged(nameof(OperationCount));
        StatusText = $"Otomatik operasyon önerisi: {_operations.Count} operasyon";
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private bool IsInnerCutCandidate(PartModel part)
    {
        if (part.Geometry.Vertices.Count < 3) return false;
        foreach (var other in Parts)
        {
            if (other.Id == part.Id) continue;
            if (other.Geometry.Vertices.Count < 3) continue;
            if (GeometryUtils.PolygonContainsPolygon(other.Geometry, part.Geometry))
                return true;
        }
        return false;
    }

    private void AnalyzeInnerOuterCut()
    {
        PushUndo("İç/Dış kesim analizi");
        int innerCount = 0;
        int outerCount = 0;

        foreach (var part in Parts)
        {
            bool isInner = IsInnerCutCandidate(part);
            part.IsInnerCandidate = isInner;
            part.IsOuterCandidate = !isInner;
            var layer = GetLayerForPart(part);
            if (layer == null || layer.Type != LayerType.Cut) continue;

            if (isInner)
            {
                var existingOp = _operations.FirstOrDefault(o =>
                    o.LayerId == layer.Id && o.Type == OperationType.CutInner && o.Enabled);
                if (existingOp == null)
                {
                    _operations.Add(new LaserOperation
                    {
                        Name = $"{layer.Name} - İç Kesim (Analiz)",
                        LayerId = layer.Id,
                        LayerName = layer.Name,
                        Color = layer.Color,
                        Type = OperationType.CutInner,
                        Power = layer.Power * 0.9,
                        Speed = layer.Speed * 0.8,
                        PassCount = layer.PassCount + 1,
                        Priority = _operations.Count,
                        Enabled = true
                    });
                }
                innerCount++;
            }
            else
            {
                outerCount++;
            }
        }

        RenumberOperations();
        OnPropertyChanged(nameof(Operations));
        OnPropertyChanged(nameof(OperationCount));
        StatusText = $"İç/Dış analizi: {innerCount} iç kesim, {outerCount} dış kesim adayı bulundu";
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private void ApplyOperationProperties()
    {
        if (SelectedOperation == null) return;
        if (!double.TryParse(OperationPowerText, out double power) ||
            !double.TryParse(OperationSpeedText, out double speed) ||
            !int.TryParse(OperationPassCountText, out int passCount) ||
            !int.TryParse(OperationPriorityText, out int priority) ||
            passCount <= 0 ||
            string.IsNullOrWhiteSpace(OperationNameText))
        {
            StatusText = "Operasyon: Geçerli ad, güç, hız, pas ve öncelik değerleri girin";
            return;
        }

        PushUndo($"Operasyon güncelle: {SelectedOperation.Name}");
        SelectedOperation.Name = OperationNameText.Trim();
        SelectedOperation.Type = SelectedOperationType;
        SelectedOperation.Power = Math.Max(0, power);
        SelectedOperation.Speed = Math.Max(0, speed);
        SelectedOperation.PassCount = passCount;
        SelectedOperation.Priority = Math.Max(0, priority);

        RenumberOperations();
        OnPropertyChanged(nameof(Operations));
        StatusText = $"Operasyon güncellendi: {SelectedOperation.Name}";
        RequestDrawPreview?.Invoke();
        SetDirty();
    }

    private void SyncOperationFieldsFromSelection()
    {
        if (SelectedOperation == null)
        {
            OperationNameText = string.Empty;
            OperationPowerText = "80";
            OperationSpeedText = "20";
            OperationPassCountText = "1";
            OperationPriorityText = "0";
            SelectedOperationType = OperationType.CutOuter;
            return;
        }

        OperationNameText = SelectedOperation.Name;
        OperationPowerText = $"{SelectedOperation.Power:F0}";
        OperationSpeedText = $"{SelectedOperation.Speed:F0}";
        OperationPassCountText = SelectedOperation.PassCount.ToString();
        OperationPriorityText = SelectedOperation.Priority.ToString();
        SelectedOperationType = SelectedOperation.Type;
    }

    private void RemapNestResultPartReferences()
    {
        if (NestResult == null) return;
        var partById = Parts.ToDictionary(p => p.Id, p => p);

        foreach (var placement in NestResult.Placed)
        {
            if (partById.TryGetValue(placement.PartId, out var part))
                placement.Part = part;
        }

        var remappedUnplaced = new List<PartModel>();
        foreach (var up in NestResult.Unplaced)
        {
            if (partById.TryGetValue(up.Id, out var part))
                remappedUnplaced.Add(part);
            else
                remappedUnplaced.Add(up);
        }
        NestResult.Unplaced.Clear();
        NestResult.Unplaced.AddRange(remappedUnplaced);
    }

    public void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static void NotifyCommandStateChanged()
        => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
}
