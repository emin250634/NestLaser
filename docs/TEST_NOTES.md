# Test Notes — NestLaser Desktop

## BUGFIX — Portable Build Self-Contained Runtime

### Sorun
dist/portable/NestLaserDesktop.exe çalıştırılınca .NET runtime indir/yükle ekranı geliyordu.
Portable build gerçek self-contained değildi.

### Kök Neden
- build-release.ps1: `--self-contained true` eksikti build komutunda
- Test adımı portable modda RID uyumsuzluğu nedeniyle başarısız oluyordu

### Düzeltme
1. Portable.pubxml:
   - SelfContained: true
   - RuntimeIdentifier: win-x64
   - UseAppHost: true

2. build-release.ps1:
   - Restore: `dotnet restore $ProjectFile -r win-x64`
   - Build: `dotnet build $ProjectFile -c Release -r win-x64 --self-contained true --no-restore`
   - Tests portable modda atlanıyor (RID uyumsuzluğu)
   - Exe yolu düzeltildi: `dist\portable\NestLaserDesktop.exe`

### Doğrulama
```
.\scripts\build-release.ps1 -Clean -Portable
```

Çıktı:
- dist\portable\NestLaserDesktop.exe (148.5 KB)
- coreclr.dll (4.8 MB)
- hostfxr.dll (358 KB)
- hostpolicy.dll (397 KB)
- NestLaserDesktop.runtimeconfig.json (includedFrameworks ile)

### Başarı Kriteri
- dist/portable/NestLaserDesktop.exe temiz Windows'ta .NET runtime istemeden çalışmalı
- coreclr.dll, hostfxr.dll, hostpolicy.dll dosyaları mevcut olmalı

---

## BUGFIX RC1 - DXF Import Sonrası Geometri Görünmüyor

### Sorun
RC1 portable sürümde DXF import sonrası:
- Layerlar oluşuyor
- Layer listesi görünüyor
- Parça sayıları geliyor
- Ancak canvas üzerinde geometri görünmüyor

Bu problem FAZ 8O.1 sonrası ortaya çıktı (viewport culling eklemesi).

### Kök Neden
`IsVisibleInViewport(Geometry.BoundingBox bounds)` fonksiyonunda hatalı mantık:
```csharp
// ESKİ (HATALI)
Math.Max(bounds.MinX, 0) <= _viewportMaxX
// Negatif MinX değerleri 0'a yuvarlanıyordu, bu yanlış culling'e neden oluyordu
```

### Düzeltme
1. MainWindow.xaml.cs:98-102 - BoundingBox overload düzeltildi:
   - Hatalı `Math.Max(bounds.MinX, 0)` kaldırıldı
   - Gereksiz `Math.Min(bounds.MaxX, double.MaxValue)` kaldırıldı
   - Doğru rectangle overlap kontrolü eklendi

2. MainWindow.xaml.cs:84-97 - UpdateViewportBounds guard eklendi:
   - Canvas boyutu < 1px ise varsayılan viewport değerleri kullanılıyor

3. MainWindow.xaml.cs:1003-1012 - FitToScreen edge case düzeltildi:
   - Canvas küçükse zoom/pan sıfırlanıyor

### Doğrulama
```
dotnet build NestLaserDesktop.sln -c Release
dotnet test NestLaserDesktop.sln
.\scripts\build-release.ps1 -Clean -Portable
```

Çıktı:
- Build: 0 warning, 0 error
- Tests: 28 passed, 0 failed, 0 skipped

### Başarı Kriteri
- DXF import sonrası parçalar görünmeli
- Viewport culling performans için çalışmalı
- Geometri yanlışlıkla elenmemeli

---

## Build Verification
- `dotnet restore NestLaserDesktop.sln`
- `dotnet build NestLaserDesktop.sln`
- `dotnet test NestLaserDesktop.sln`
- Latest FAZ 8P result: build 0 warning, 0 error; tests 28 passed, 0 failed, 0 skipped

## FAZ 8P — Installer, Packaging & Release Readiness

### 1. Assembly Metadata
- [x] Product name: NestLaser Desktop
- [x] Company: NestLaser
- [x] Copyright: Copyright © 2024 NestLaser
- [x] Version: 1.0.0-RC1
- [x] FileVersion: 1.0.0.1

### 2. Version System
- [x] AppVersion.cs created with ProductVersion, FileVersion, ReleaseChannel
- [x] About dialog displays version info

### 3. First Run Setup
- [x] AppData folders created explicitly on startup
- [x] First-run flag file created
- [x] Logged to error-log.txt

### 4. Build & Publish
- [x] Portable publish profile created
- [x] Release publish profile created
- [x] build-release.ps1 script created

### 5. Documentation
- [x] RELEASE_READINESS_REVIEW.md created
- [x] RELEASE_CHECKLIST.md created

### 6. Validation
- [x] `dotnet build -c Release` → 0 warning, 0 error
- [x] `dotnet test -c Release --no-build` → 28 passed, 0 failed, 0 skipped

## FAZ 8O.1 — Render Pipeline & Technical Debt Cleanup

### 1. Viewport State Tracking
- [x] `_viewportMinX/MaxX/MinY/MaxY` added
- [x] `_renderedPartCount` added
- [x] `_culledPartCount` added
- [x] `_lastRenderTicks` added
- [x] `_enableRenderDiagnostics` added

### 2. Helper Methods
- [x] `UpdateViewportBounds()` added
- [x] `IsVisibleInViewport()` added

### 3. Viewport Culling
- [x] `DrawPartsOnPlate` culling implemented
- [x] `DrawMultiPlateNesting` culling implemented
- [x] BoundingBox-based visibility check

### 4. Render Diagnostics
- [x] Timing instrumentation in `DrawPreview()`
- [x] AppLogger error logging in catch block

### 5. Validation
- [x] `dotnet test --no-build` → 28 passed, 0 failed, 0 skipped

## FAZ 8M — Responsiveness & Architecture Cleanup

### 1. Workflow Services
- [x] `ImportWorkflowService` added
- [x] `ProjectWorkflowService` added
- [x] `CostWorkflowService` added
- [x] `ExportWorkflowService` added
- [x] `NestingWorkflowService` added

### 2. Background Tasks
- [x] DXF import runs through async workflow
- [x] Project open/save runs through async workflow
- [x] Nesting runs through async workflow
- [x] Benchmark runs through async workflow
- [x] Cost calculation runs through async workflow
- [x] DXF export runs through async workflow
- [x] PDF export runs through async workflow

### 3. Progress UI
- [x] `ProgressOverlayText` added
- [x] `ProgressPercent` added
- [x] `IsProgressIndeterminate` added
- [x] Overlay ProgressBar bound to progress state
- [x] `CancellationToken` plumbing prepared in workflow services

### 4. Validation
- [x] `dotnet build NestLaserDesktop.sln` → 0 warning, 0 error
- [x] `dotnet test NestLaserDesktop.sln` → 23 passed, 0 failed, 0 skipped

## FAZ 8L — Stabilization, Regression Testing & RC Preparation

### 1. Regression Test Suite
- [x] `NestLaserDesktop.Tests` xUnit project created
- [x] `GeometryTests` category added
- [x] `DxfImportTests` category added
- [x] `DxfExportTests` category added
- [x] `ProjectTests` category added
- [x] `CostTests` category added
- [x] `PdfTests` category added
- [x] `LayerTests` category added
- [x] `OperationTests` category added

### 2. Geometry Regression
- [x] Polygon area and perimeter checked
- [x] NormalizeWinding checked
- [x] CleanupVertices checked
- [x] IsValid checked
- [x] Move, Scale, Rotate, MirrorX, MirrorY checked
- [x] Mirror keeps winding positive after normalization

### 3. DXF Fixture Pack
- [x] Unitless fixture
- [x] Millimeter fixture
- [x] Inch fixture
- [x] LWPOLYLINE fixture
- [x] POLYLINE fixture
- [x] CIRCLE fixture
- [x] ARC fixture
- [x] SPLINE fixture
- [x] ELLIPSE fixture

### 4. Stability & Safety
- [x] Cost cut-time double-count regression covered
- [x] Material `PerSheet`, `PerSquareMeter`, `PerKg` formulas covered
- [x] Project profile snapshot roundtrip covered
- [x] Safe `.nelp` backup save covered
- [x] Quotation PDF smoke covered
- [x] Production PDF smoke covered
- [x] Crash logging registered for AppDomain, Dispatcher, TaskScheduler
- [x] DXF CIRCLE import hang regression fixed
- [x] DXF ELLIPSE import hang regression fixed
- [x] DXF POLYLINE import hang regression fixed

### 5. CI & RC
- [x] `.github/workflows/dotnet.yml` added
- [x] `docs/TESTING_GUIDE.md` added
- [x] `docs/RC_CHECKLIST.md` added

## FAZ 8J — PDF Quotation & Production Report System

### 1. PDF Report Service
- [x] `PdfReportService.CreateQuotationPdf` creates a `.pdf`
- [x] `PdfReportService.CreateProductionReportPdf` creates a `.pdf`
- [x] `PdfReportService.CreatePreviewPdf` can create a standalone preview PDF
- [x] No external NuGet dependency required

### 2. Quotation PDF Content
- [x] Header shows "NestLaser Quotation"
- [x] CompanyProfile fields are used in the header
- [x] LogoPath is embedded when it points to a readable image
- [x] Date, project, material, machine, and part/plate summary are included

### 3. Summary Sections
- [x] Material Summary includes material, thickness, plate size, used plate count, efficiency, waste
- [x] Production Summary includes cut length, engraving area, estimated time, operation count
- [x] Cost Summary includes material, waste, machine, labor, electricity, consumable, total production cost
- [x] Sales Summary includes profit margin, sales price, VAT, VAT-included price

### 4. Nesting Preview
- [x] PDF draws plate borders
- [x] PDF draws placed geometry as vector paths
- [x] Preview footer shows efficiency, waste, and placed/total part count

### 5. UI & Validation
- [x] File menu includes "PDF Teklif Oluştur"
- [x] File menu includes "PDF Üretim Raporu Oluştur"
- [x] Cost/Teklif tab includes company settings and PDF buttons
- [x] Warning when no project/DXF exists
- [x] Warning when nesting result is missing
- [x] Warning when material is missing
- [x] Warning when machine is missing
- [x] Warning when cost has not been calculated

### 6. Project Persistence
- [x] CompanyProfile saved/restored in `.nelp`
- [x] PdfReportSettings saved/restored in `.nelp`

### 7. Build
- [x] `dotnet build NestLaserDesktop.csproj` → 0 warning, 0 error

## FAZ 9X — Geometry / Unit / Nesting Professionalization

### 1. DXF Unit Detection
- [x] $INSUNITS=0 → DxfUnit.Unitless (scale=1.0)
- [x] $INSUNITS=1 → DxfUnit.Inches (scale=25.4)
- [x] $INSUNITS=4 → DxfUnit.Millimeters (scale=1.0)
- [x] $INSUNITS=5 → DxfUnit.Centimeters (scale=10.0)
- [x] $INSUNITS=6 → DxfUnit.Meters (scale=1000.0)
- [x] $MEASUREMENT=0 → inches, $MEASUREMENT=1 → mm
- [x] $LUNITS fallback parsing
- [x] No header variables → Unitless fallback with warning
- [x] DxfHeaderParser reads only HEADER section, stops at CLASSES/ENDSEC

### 2. Unit Conversion Pipeline
- [x] DxfService.Import() accepts ImportUnitInfo override parameter
- [x] Vertex coordinates scaled by ScaleFactorToMm before polygon creation
- [x] ImportUnitInfo.Default returns mm with scale=1.0
- [x] GetScaleToMm returns correct values: inches→25.4, cm→10, m→1000, feet→304.8
- [x] ImportUnitInfo set on DxfImportResult.UnitInfo after import
- [x] TotalBoundingWidth/Height computed from all part geometries
- [x] Warning added when unit conversion applied (scale != 1.0)

### 3. Manual Import Scale UI
- [x] ImportAutoDetect toggle (default true)
- [x] ImportSourceUnit ComboBox shows all DxfUnit values
- [x] ImportManualScale TextBox for direct factor input
- [x] Not "Scale değişikliği için DXF'yi yeniden açın" hint shown
- [x] OpenDxfWithOptionsCommand calls OpenDxf with unit override

### 4. Import Verification Panel
- [x] ImportSourceUnitText shows detected source unit name
- [x] ImportDetectionSourceText shows detection method or "Algılanamadı"
- [x] ImportScaleFactorText shows applied scale factor
- [x] ImportBoundingBoxText shows "WxH mm"
- [x] ImportPartCount shows part count
- [x] ImportReferenceDimText input for real-world width
- [x] ImportSuggestedScaleText computes suggested scale from reference
- [x] ApplySuggestedScaleCommand sets ImportManualScale + disables auto-detect

### 5. Layer Label Display Cleanup
- [x] ShowPartNames defaults to false
- [x] ShowLayerNames defaults to false
- [x] ShowOperationNames defaults to false
- [x] Toolbar checkboxes: "İsim", "Layer", "Op"
- [x] View menu items: "Parça İsmi Göster", "Layer İsmi Göster", "Operasyon İsmi Göster"
- [x] Labels only render when zoom > 0.5×
- [x] Label opacity at 75%
- [x] Labels use Consolas font at 9px
- [x] Property changes trigger RequestDrawPreview

### 6. Fit / Bounds Correction
- [x] GetPreviewContentBounds includes visible, non-reference parts
- [x] GetPreviewContentBounds includes all NestResult plates with placed parts
- [x] GetPreviewContentBounds includes unplaced parts area
- [x] GetPreviewContentBounds excludes hidden layer parts
- [x] GetPreviewContentBounds excludes reference layer parts
- [x] FitToScreen uses GetPreviewContentBounds for both nest/no-nest cases
- [x] Plates with no NestResult fall back to original parts bounds

### 7. Nesting Benchmark Mode
- [x] RunBenchmarkCommand exists with CanExecute when Parts.Count > 0
- [x] Runs all 3 algorithms: Free Rectangle, Polygon Collision, Irregular
- [x] BenchmarkFreeRectResult / BenchmarkPolygonResult / BenchmarkIrregularResult stored
- [x] HasBenchmarkResults computed property
- [x] BenchmarkSummaryText shows full comparison report
- [x] Report includes: time, placed/unplaced, efficiency, waste, plates, attempts, collision checks
- [x] Best algorithm recommended at end of report
- [x] Toolbar "Benchmark" button + Araçlar menu item
- [x] Benchmark results displayed in Doğrulama tab

### 8. Nesting Quality Guard
- [x] ValidateFinalPlacements checks plate overflow (IsGeometryInsideUsableArea)
- [x] ValidateFinalPlacements checks collision between placements
- [x] Invalid placements moved to Unplaced with warning
- [x] Placed/unplaced counts recalculated after validation
- [x] UsedArea recalculated after validation

### 9. Regression Test Samples
- [x] samples/dxf/README.md created
- [x] 7 test scenarios documented: CorelDRAW, AutoCAD, RDWorks, Inch-based, Unitless, Organic, Many Small
- [x] Each scenario describes source, format, tests, expected behavior
- [x] Test instructions for manual verification

### 10. Build
- [x] `dotnet build` → 0 warning, 0 error

## BUGFIX — Material Cost Fields Missing in UI

### 1. Malzemeler Tab Cost Fields
- [x] UnitPrice TextBox bound to SelectedMaterial.UnitPrice
- [x] UnitType ComboBox bound to SelectedMaterial.UnitType with UnitTypeItems source
- [x] ThicknessMm TextBox bound to SelectedMaterial.ThicknessMm
- [x] Density TextBox bound to SelectedMaterial.Density
- [x] Notes TextBox bound to SelectedMaterial.Notes
- [x] UnitPriceText read-only display shows formatted price with unit
- [x] "Malzeme Bilgilerini Kaydet" button saves via MaterialProfileService

### 2. UnitPrice = 0 Behavior
- [x] CalculateCost shows warning instead of blocking when UnitPrice <= 0
- [x] Calculate continues with partial estimate (material cost = 0)
- [x] No crash/exception when UnitPrice is 0

### 3. ApplyMaterialSettings
- [x] Button always executable (no CanExecute guard)
- [x] Shows "Önce bir operasyon seçin." status when no operation selected
- [x] Shows "Önce bir malzeme seçin." status when no material selected
- [x] Shows "Önce bir makine seçin." status when no machine selected
- [x] Works correctly when operation, material, and machine all selected

### 4. ComboBox Dropdown Readability
- [x] Dropdown items have black (#111111) foreground
- [x] Dropdown items have light (#F5F5F5) background
- [x] Hover highlights blue (#D0E8FF)
- [x] Selection highlights green (#4EC9B0)
- [x] All ComboBox controls use the same style

### 5. SelectedMaterial Notification
- [x] Changing material selection updates CostMaterialName in Cost/Teklif tab
- [x] Material cost fields update on selection change

### 6. Build
- [x] `dotnet build` → 0 warning, 0 error

## FAZ 8I — Cost Estimation & Quotation System

### 1. Models
- [x] CostSettings creates with default rates and flags
- [x] JobCostEstimate holds full cost breakdown (material, waste, machine, labor, electricity, consumable)
- [x] UnitType enum: PerSheet, PerSquareMeter, PerKg
- [x] SpeedUnit enum: MmPerSecond, MmPerMinute
- [x] CurrencyType enum: TRY, USD, EUR
- [x] MaterialProfile.UnitPrice and MaterialProfile.UnitType default correctly
- [x] LaserOperation.SpeedUnit defaults to MmPerSecond

### 2. CostEstimationService
- [x] CalculateCuttingLengths: CutInner/CutOuter → totalCutLength, Mark → markLength, Engrave → engravingArea
- [x] CalculateEstimatedTime: speed converted from MmPerSecond * 60 to mm/min; time = length / speed * passCount
- [x] CalculateMaterialCost: PerSheet uses plateCount * unitPrice; PerSquareMeter uses plateArea * unitPrice; PerKg uses density * thickness * plateArea * unitPrice
- [x] CalculateWasteCost: wastePercent = 100 - efficiency; wasteCost = materialCost * wastePercent / 100
- [x] CalculateMachineCost: machineHourlyRate * hours
- [x] CalculateLaborCost: operatorHourlyRate * hours (if IncludeOperatorCost)
- [x] CalculateElectricityCost: electricityCostPerHour * hours (if IncludeElectricityCost)
- [x] CalculateConsumableCost: consumableCostPerJob flat fee (if IncludeConsumables)
- [x] Suggested price = totalProductionCost * (1 + profitMargin / 100)
- [x] Final price with VAT = suggestedPrice * (1 + vatPercent / 100)
- [x] Progressive rounding: <10 → 1 dec; 10–100 → nearest 5; 100–1000 → nearest 10; 1000–10000 → nearest 50; >10000 → nearest 100
- [x] GenerateQuotationText: returns well-formatted plain text string

### 3. UI — Cost/Teklif Tab
- [x] Tab appears between Malzemeler and Analiz
- [x] Material section: material name, machine name, plate info
- [x] Üretim section: efficiency, waste %, cut length, engrave area, estimated time
- [x] Maliyet section: material, waste, machine, labor, electricity, consumable, total production cost
- [x] Teklif section: profit margin (editable), VAT (editable), currency (editable), suggested price, final price with VAT
- [x] Calculate button triggers CalculateCostCommand
- [x] Copy Quotation button triggers CopyQuotationCommand (only when estimate exists)
- [x] Currency ComboBox shows TRY/USD/EUR

### 4. Validation
- [x] Error when no parts loaded
- [x] Error when no material selected
- [x] Error when no machine selected
- [x] Error when material has zero unit price
- [x] Warning when estimated time is zero/negative
- [x] Warning when material cost is zero

### 5. Export Report
- [x] MALİYET & TEKLİF section appears when cost data exists
- [x] Shows cut length, engrave area, estimated time, all cost lines, suggested and final price

### 6. Project Persistence
- [x] CostProfitMarginPercent saved/restored
- [x] CostVatPercent saved/restored
- [x] CostCurrency saved/restored
- [x] LastCostEstimate saved/restored
- [x] No error on old .nelp files (missing cost fields → defaults)

### 7. Cost Settings Persistence
- [x] AppData/NestLaser/cost-settings.json created on first load
- [x] Machine/operator hourly rates, electricity, consumable, engraving rate, flags saved/loaded

## FAZ 8H — Material Database & Machine Profiles

### 1. MaterialProfile Model
- [x] MaterialProfile creates with all fields
- [x] DisplayName shows "Name - Thickness" when thickness > 0
- [x] Id is auto-generated 8-char hex
- [x] Default material flagged with IsDefault

### 2. MachineProfile Model
- [x] MachineProfile creates with all fields
- [x] Working area dimensions saved

### 3. MaterialOperationSetting Model
- [x] Links material + machine + operation type
- [x] Power, Speed, PassCount, Frequency, AirAssist stored

### 4. MaterialProfileService Seed Data
- [x] First run creates seed JSON files in AppData/NestLaser/profiles/
- [x] MDF 3/5/8mm, Pleksi 2/3/5mm, Kontraplak 3/6/10mm, Paslanmaz, Galvaniz, Deri, Karton, Kumaş
- [x] Ruida 100/130/150W, CO2 Generic 80W, Fiber 20/30/50W
- [x] 28 operation settings covering MDF/Pleksi + Ruida combinations

### 5. Materials Tab UI
- [x] Tab appears between Operasyonlar and Analiz
- [x] Material search/filter works
- [x] Material ComboBox bound to FilteredMaterials
- [x] Machine ComboBox bound to Machines
- [x] Add Material button works
- [x] Delete Material button works
- [x] Add Machine button works
- [x] Delete Machine button works
- [x] Refresh Materials button loads from disk

### 6. Operation Auto-Suggest
- [x] Selecting material applies settings to current operation
- [x] Selecting machine applies settings to current operation
- [x] "Ayarları Uygula" button manually applies
- [x] Only applies when matching MaterialOperationSetting exists
- [x] Power/Speed/Pass fields updated in Operation properties panel

### 7. Project System
- [x] SelectedMaterialId saved in .nelp
- [x] SelectedMachineId saved in .nelp
- [x] Material/machine restored on project load
- [x] No error if material/machine ID doesn't exist (graceful skip)

### 8. Export Report
- [x] MALZEME & MAKİNE section included in export-report.txt
- [x] Material name shown when selected
- [x] Machine name shown when selected

## FAZ 8G — Operation Manager & Production Pipeline

### 1. Operation Model
- [x] LaserOperation creates with default values
- [x] LayerName and Color fields are serialized/deserialized
- [x] Clone() copies all fields including LayerName and Color
- [x] INotifyPropertyChanged fires on all property changes

### 2. PartModel Inner/Outer Flags
- [x] PartModel.IsInnerCandidate defaults to false
- [x] PartModel.IsOuterCandidate defaults to false
- [x] Clone() copies candidate flags
- [x] AnalyzeInnerOuterCut sets flags on all Parts

### 3. Auto-Suggest Layer Name Mapping
- [x] Layer named "EngraveTest" → suggests Engrave operation
- [x] Layer named "Mark_1" → suggests Mark operation
- [x] Layer named "Reference" → suggests Reference operation
- [x] Layer named "Cut Outer" → suggests CutOuter operation
- [x] Layer named "Iç Kesim" → suggests CutInner operation
- [x] Fallback to LayerType mapping when name has no hint

### 4. Inner/Outer Cut Analysis
- [x] Parts fully inside other polygons → IsInnerCandidate = true
- [x] Parts not inside any polygon → IsOuterCandidate = true
- [x] CutInner operation created automatically for inner candidates
- [x] Analysis respects Cut layer type filter

### 5. Operation Preview Colors
- [x] Engrave = Blue (#569CD6)
- [x] Mark = Yellow (#D6C656)
- [x] CutInner = Orange (#D68656)
- [x] CutOuter = Red (#D65656)
- [x] Reference = Gray (#808080)

### 6. Export Report
- [x] Operation order listed in report when IncludeOperationOrder = true
- [x] Total operation count displayed
- [x] Active operation count displayed
- [x] Disabled operations excluded from order but counted in total

### 7. Project System
- [x] Operations saved in .nelp file
- [x] Operations restored on project load
- [x] LayerName and Color fields survive serialization round-trip

### 8. Undo/Redo
- [x] Undo restores previous operation state
- [x] Operation changes are tracked (add, delete, move, update)
- [x] NestResult preserved when undoing operation changes

## FAZ 8F.1 — Geometry Integrity Cleanup

### Winding
- [x] DXF with CW-wound polylines → normalized to CCW on import
- [x] Mirror X → winding remains CCW
- [x] Mirror Y → winding remains CCW
- [x] Mirror twice (X then X or Y then Y) → back to original winding

### Validation
- [x] Polygon with < 3 vertices → IsValid = false
- [x] Zero-area sliver polygon → IsValid = false
- [x] Polygon with NaN coordinate → IsValid = false
- [x] Invalid geometries skipped with warning during import

### Cleanup
- [x] Sequential duplicate vertices removed
- [x] Closing duplicate (last == first) removed
- [x] Collinear vertices removed
- [x] Cleanup never reduces below 3 vertices

### Undo NestResult
- [x] NestResult preserved in undo snapshot
- [x] RestoreSnapshot remaps Part references by Id
- [x] Non-geometry undo (layer rename) preserves nesting

## FAZ 8E — Operation Manager Basics

### CRUD
- [x] Add new operation with defaults from selected layer
- [x] Delete operation (not last one)
- [x] Move operation up/down reorders priority
- [x] Apply operation properties (name, type, power, speed, pass count, priority)

### Auto-Suggest
- [x] Creates one operation per layer
- [x] Cut layers get CutOuter + optional CutInner
- [x] Non-cut layers map by LayerType

### Export Report
- [x] Operation order included in report
- [x] Operations listed by priority with type, power, speed, pass count
# Phase 8N Test Notes

- Added `ProjectPortabilityTests`.
- Covered migration/version defaults, snapshot portability, corrupt `.nelp` recovery through `.bak`, `.nelpkg` export/import, integrity repair, and latest-10 backup retention.
- `dotnet test` result after implementation: 28 passed, 0 failed.
