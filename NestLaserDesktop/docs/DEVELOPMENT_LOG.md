# Development Log — NestLaser Desktop

## 2026-06-13 — FAZ 8P: Installer, Packaging & Release Readiness

### Completed
- **Assembly Metadata**: Added ProductName, Company, Copyright, Version, FileVersion to csproj
- **Centralized Version**: Created `Services/AppVersion.cs` with ProductVersion, FileVersion, ReleaseChannel
- **First Run Setup**: Added explicit AppData folder creation in App.xaml.cs with first-run flag
- **Portable Build**: Created `Properties/PublishProfiles/Portable.pubxml` for self-contained deployment
- **Release Build**: Created `Properties/PublishProfiles/Release.pubxml` for framework-dependent deployment
- **Build Script**: Created `scripts/build-release.ps1` for automated release builds
- **About Dialog**: Updated to show version info from AppVersion class
- **Documentation**: Created RELEASE_READINESS_REVIEW.md and RELEASE_CHECKLIST.md

### Files Created
- `Services/AppVersion.cs`
- `Properties/PublishProfiles/Portable.pubxml`
- `Properties/PublishProfiles/Release.pubxml`
- `scripts/build-release.ps1`
- `docs/RELEASE_READINESS_REVIEW.md`
- `docs/RELEASE_CHECKLIST.md`

### Files Changed
- `NestLaserDesktop.csproj`: Added assembly metadata
- `App.xaml.cs`: Added first-run setup and folder creation
- `Views/MainWindow.xaml.cs`: Updated About_Click to use AppVersion

### Validation
- `dotnet build -c Release` → 0 warning, 0 error
- `dotnet test -c Release --no-build` → 28 passed, 0 failed

---

## 2026-06-13 — FAZ 8O.1: Render Pipeline & Technical Debt Cleanup

### Completed
- **Viewport State Tracking**: Added `_viewportMinX/MaxX/MinY/MaxY`, `_renderedPartCount`, `_culledPartCount`, `_lastRenderTicks`, `_enableRenderDiagnostics` fields.
- **Helper Methods**: Added `UpdateViewportBounds()` and `IsVisibleInViewport()` for bounding box-based viewport culling.
- **DrawPartsOnPlate Culling**: Implemented viewport culling - skips rendering off-screen parts using BoundingBox visibility check.
- **DrawMultiPlateNesting Culling**: Implemented viewport culling - skips rendering off-screen placed parts.
- **Render Diagnostics**: Added timing instrumentation in `DrawPreview()` with try/catch and AppLogger error logging.
- **Error Handling**: Replaced empty catch blocks with proper error logging via AppLogger.

### Files Changed
- `Views/MainWindow.xaml.cs`: Added viewport state, culling logic, diagnostics timing

### Validation
- Build: locked by running process (NestLaserDesktop.exe PID 4084)
- Tests: `dotnet test --no-build` → 28 passed, 0 failed

---

## 2026-06-13 — FAZ 8M: Responsiveness & Architecture Cleanup

### Completed
- **Architecture Audit**: Reviewed `MainViewModel` and `MainWindow.xaml.cs` responsibilities. Documented remaining debt in `docs/ARCHITECTURE_REVIEW.md`.
- **Workflow Services**: Added `ImportWorkflowService`, `ProjectWorkflowService`, `CostWorkflowService`, `ExportWorkflowService`, and `NestingWorkflowService`.
- **Background Tasks**: Moved DXF import, project open/save, nesting, benchmark, cost calculation, DXF export, and PDF export to Task-based background execution.
- **Progress Reporting**: Extended existing loading overlay with `ProgressOverlayText`, `ProgressPercent`, and `IsProgressIndeterminate`.
- **Cancel Preparation**: Workflow service signatures accept `CancellationToken`; full UI cancel button is deferred.
- **MainViewModel Cleanup**: Moved import verification formatting, project recent handling, cost validation/settings assembly, export validation, PDF/DXF export execution, and benchmark summary generation into services.
- **MainWindow Cleanup**: Updated close/save flow for async project save. Rendering remains in code-behind and is documented as a future split.

### Files Created
- `Services/WorkflowProgress.cs`
- `Services/ImportWorkflowService.cs`
- `Services/ProjectWorkflowService.cs`
- `Services/CostWorkflowService.cs`
- `Services/ExportWorkflowService.cs`
- `Services/NestingWorkflowService.cs`
- `docs/ARCHITECTURE_REVIEW.md`

### Files Changed
- `ViewModels/MainViewModel.cs`: async commands, progress state, workflow service calls
- `Views/MainWindow.xaml`: progress overlay bindings
- `Views/MainWindow.xaml.cs`: async close/save path

### Validation
- `dotnet build NestLaserDesktop.sln` → 0 warning, 0 error
- `dotnet test NestLaserDesktop.sln` → 23 passed, 0 failed, 0 skipped

---

## 2026-06-13 — FAZ 8L: Stabilization, Regression Testing & Release Candidate Preparation

### Completed
- **Regression Test Suite**: Added `NestLaserDesktop.Tests` xUnit project with Geometry, DXF Import, DXF Export, Project, Cost, PDF, Layer, and Operation test categories.
- **DXF Fixture Pack**: Added test coverage for unitless, millimeter, inch, LWPOLYLINE, POLYLINE, CIRCLE, ARC, SPLINE, and ELLIPSE DXF samples under `samples/dxf`.
- **Cost Formula Audit**: Fixed cut time estimation so multiple cut operations on the same layer do not multiply global total cut length per operation. Added regression coverage.
- **DXF Parser Stability**: Fixed CIRCLE, ELLIPSE, and POLYLINE parser progress bugs that could hang import on valid fixture files.
- **Project Portability**: Added `ProfileSnapshot` persistence for material and machine profiles inside `.nelp`; missing local profiles are restored from snapshot when possible and surfaced through status warnings.
- **AppData Safety Layer**: Added safe JSON save/load service using temp file, replace, and `.bak` backup flow for projects, recent projects, materials, machines, operation settings, and cost settings.
- **Crash Logging**: Added `AppLogger` and registered AppDomain, Dispatcher, and TaskScheduler exception handlers.
- **PDF Smoke Tests**: Added quotation and production PDF creation tests with `%PDF` header validation.
- **CI**: Added `.github/workflows/dotnet.yml` for restore/build/test on push and pull request.
- **RC Docs**: Added `docs/TESTING_GUIDE.md` and `docs/RC_CHECKLIST.md`.

### Files Created
- `NestLaserDesktop.Tests/`
- `Services/AppLogger.cs`
- `Services/SafeJsonFileService.cs`
- `Models/ProfileSnapshot.cs`
- `.github/workflows/dotnet.yml`
- `docs/TESTING_GUIDE.md`
- `docs/RC_CHECKLIST.md`

### Files Changed
- `App.xaml.cs`: registered crash logging handlers
- `NestLaserDesktop.csproj`: excluded nested test source files from the WPF app compile glob
- `Services/DxfParser.cs`: fixed parser index advancement for CIRCLE, ELLIPSE, and POLYLINE
- `Services/ProjectService.cs`: safe project/recent-project save/load
- `Services/MaterialProfileService.cs`: safe profile/settings save/load
- `Services/CostEstimationService.cs`: safe settings save/load and cut-time regression fix
- `Models/NestLaserProject.cs`: added `ProfileSnapshot`
- `ViewModels/MainViewModel.cs`: restored missing profiles from project snapshots with warning status

### Validation
- `dotnet restore NestLaserDesktop.sln` → 0 error
- `dotnet build NestLaserDesktop.sln` → 0 warning, 0 error
- `dotnet test NestLaserDesktop.sln` → 23 passed, 0 failed, 0 skipped

---

## 2026-06-13 — FAZ 8J: PDF Quotation & Production Report System

### Completed
- **PdfReportService**: Added native PDF generation without external NuGet dependencies. Generates quotation PDFs, production report PDFs, and standalone nesting preview PDFs.
- **Quotation PDF**: Includes NestLaser Quotation header, company information, date, project, material, machine, part/plate summary, nesting preview, material summary, production summary, cost summary, and sales summary.
- **Production PDF**: Uses the same cost and nesting data with a production report title for shop-floor/customer-facing reporting.
- **Nesting Preview**: Draws plate borders and placed part geometry directly into the PDF as vector paths, including efficiency, waste, and placed/total part count.
- **CompanyProfile**: Added CompanyName, Address, Phone, Email, Website, LogoPath. LogoPath is persisted and, when the file exists, embedded in the PDF header.
- **PDF Settings Persistence**: Added PdfReportSettings with last quotation path, last production report path, last export directory, include-preview flag, and last report type. Stored inside `.nelp`.
- **UI Integration**: Added File menu commands and Cost/Teklif tab buttons for "PDF Teklif Oluştur" and "PDF Üretim Raporu Oluştur". Company fields are editable in Cost/Teklif.
- **Validation**: PDF export warns when no project/DXF exists, no nesting exists, material is missing, machine is missing, or cost has not been calculated.

### Files Created
- `Models/CompanyProfile.cs`
- `Models/PdfReportSettings.cs`
- `Services/PdfReportService.cs`

### Files Changed
- `Models/NestLaserProject.cs`: Added CompanyProfile and PdfReportSettings persistence
- `ViewModels/MainViewModel.cs`: Added company/PDF properties, PDF export commands, validation, save dialog integration
- `Views/MainWindow.xaml`: Added PDF menu items, company settings, and PDF action buttons

### Build
- `dotnet build NestLaserDesktop.csproj` → ✅ 0 warning, 0 error

---

## 2026-06-13 — BUGFIX: Material Cost Fields Missing in UI

### Fixed
- **Malzemeler tab**: Added Material Cost editing fields (UnitPrice, UnitType, ThicknessMm, Density, Notes) inside a green-bordered section after material selection. Uses direct two-way binding to `SelectedMaterial` properties.
- **SaveMaterialChanges command**: New button "Malzeme Bilgilerini Kaydet" persists changes to AppData/NestLaser/profiles/materials.json via `MaterialProfileService.SaveMaterials()`.
- **UnitPrice = 0 handling**: `CalculateCost()` no longer blocks on zero UnitPrice. Instead shows warning "birim fiyat tanımlanmamış, tahmini maliyet 0 olarak hesaplanacak" and continues with partial estimate.
- **SelectedOperation binding**: `ApplyMaterialSettingsCommand` no longer uses `CanExecute` guard. Method now shows status text warnings when no operation/material/machine selected instead of silently returning.
- **ComboBox dropdown readability**: Added global `ComboBoxItem` style with black (#111111) foreground, light (#F5F5F5) background, blue hover highlight, and green selection highlight. All ComboBox elements now have readable dropdown options.
- **SelectedMaterial setter**: Added `OnPropertyChanged(nameof(CostMaterialName))` notification so Cost/Teklif tab updates immediately when material selection changes.

### Files Changed
- `Views/MainWindow.xaml`: Material cost fields in Malzemeler tab, global ComboBoxItem style for dropdown readability
- `ViewModels/MainViewModel.cs`: Added SaveMaterialChangesCommand/UnitTypeItems/SaveMaterialChanges(), fixed SelectedMaterial notification, changed ApplyMaterialSettings to always-executable with status warnings, changed CalculateCost to warn instead of block on zero UnitPrice

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 9X: Geometry / Unit / Nesting Professionalization

### Completed
- **DXF Unit Detection**: Parse $INSUNITS/$MEASUREMENT/$LUNITS from DXF HEADER section. Supports Unitless, mm, cm, m, inches, feet. Falls back to mm with warning.
- **ImportUnitInfo model**: SourceUnit, TargetUnit, ScaleFactorToMm, IsUnitDetected, DetectionSource, WarningMessage. Static helpers for display name and INSUNITS→enum conversion.
- **DxfHeaderParser service**: Stream-based HEADER parsing that detects unit variables before entity parsing. Non-blocking, reads only header section.
- **Unit conversion pipeline**: DxfService.Import() now accepts optional ImportUnitInfo override and applies ScaleFactorToMm to all vertex coordinates on import. All geometry is stored in mm internally.
- **Manual Import Scale**: UI in new Doğrulama tab with Auto/Manual toggle, source unit ComboBox, manual scale factor input, and per-file override.
- **Import Verification panel**: Shows source unit, detection source, applied scale, bounding box dimensions, part count. Reference dimension input with suggested scale calculation (user enters real-world width, system computes required scale factor).
- **Layer Label Display**: Added ShowPartNames/ShowLayerNames/ShowOperationNames bool properties bound to toolbar checkboxes and View menu items. Labels only render when zoom > 0.5×. 75% opacity for subtle overlay. Selection-aware display.
- **Fit To Screen improvement**: Replaced simple NestResult-based bounds with GetPreviewContentBounds() that includes original visible parts, placed parts (all plates), unplaced parts area, and plate borders. Excludes hidden layer parts and reference layer parts.
- **Nesting Benchmark Mode**: RunBenchmarkCommand executes all 3 algorithms (Free Rectangle, Polygon Collision, Irregular Experimental) on the same dataset. Reports time, placed/unplaced count, efficiency, waste, placement attempts, collision checks, cache hits per algorithm. Recommends best algorithm.
- **Nesting Quality Guard**: Existing ValidateFinalPlacements already checks plate boundary overflow, inter-part collisions, plate reference validity. Invalid placements are demoted to Unplaced with warning messages.
- **Regression test samples**: Created samples/dxf/README.md with 7 test scenarios (CorelDRAW, AutoCAD, RDWorks, Inch-based, Unitless, Organic, Many Small Parts).
- **Doğrulama tab**: New sidebar tab combining Import Verification, Reference Dimension tool, Manual Import Settings, and Nesting Benchmark results.

### Files Created
- `Models/ImportUnitInfo.cs`
- `Services/DxfHeaderParser.cs`
- `samples/dxf/README.md`

### Files Changed
- `Services/DxfParser.cs` — (header parsing is in new DxfHeaderParser, no change to parser)
- `Services/DxfService.cs` — Import() accepts ImportUnitInfo override, applies scaling, computes bounding box
- `Models/DxfImportResult.cs` (inline in DxfService.cs) — added UnitInfo, TotalBoundingWidth/Height
- `ViewModels/MainViewModel.cs` — import options/verification fields, benchmark commands, label toggles, ApplySuggestedScale
- `Views/MainWindow.xaml` — label checkboxes in toolbar, benchmark button, Doğrulama tab with full verification/benchmark UI
- `Views/MainWindow.xaml.cs` — zoom-dependent label rendering, improved GetPreviewContentBounds with placed/unplaced/plate coverage
- `Nesting/NestingEngine.cs` — (quality guard already existed in ValidateFinalPlacements, no changes needed)

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 8I: Cost Estimation & Quotation System

### Completed
- **Models**: CostSettings (hourly rates, VAT, flags), JobCostEstimate (full cost breakdown), UnitType (PerSheet/PerSquareMeter/PerKg), SpeedUnit (MmPerSecond/MmPerMinute), CurrencyType (TRY/USD/EUR)
- **Profile updates**: MaterialProfile gained UnitPrice + UnitType; LaserOperation gained SpeedUnit
- **CostEstimationService**: Cutting length calculation (perimeter × PassCount per operation type), time estimation (speed → mm/min × passCount), material cost (PerSheet/PerSquareMeter/PerKg), waste cost, machine/labor/electricity/consumable costs, profit margin, VAT, progressive rounding, quotation text generation
- **MainViewModel**: CostSettings/LastCostEstimate/Currency/ProfitMargin/Vat properties, CalculateCostCommand (with validation), CopyQuotationCommand (clipboard)
- **MainWindow.xaml**: New "Cost/Teklif" tab with Material/Üretim/Maliyet/Teklif sections, Calculate & Copy buttons
- **NestLaserProject**: CostProfitMarginPercent, CostVatPercent, CostCurrency, LastCostEstimate persisted in .nelp; restored on load
- **DxfExportReport**: Cost fields (cut length, engrave area, time, costs, suggested/final price)
- **DxfService**: Export() accepts JobCostEstimate; WriteExportReport writes MALİYET & TEKLİF section
- **CostSettings persistence**: AppData/NestLaser/cost-settings.json, CurrencyTypes enum in ViewModel

### Files Created
- `Models/CostSettings.cs`
- `Models/JobCostEstimate.cs`
- `Models/UnitType.cs`
- `Models/SpeedUnit.cs`
- `Models/CurrencyType.cs`
- `Services/CostEstimationService.cs`

### Files Changed
- `Models/MaterialProfile.cs`: Added UnitPrice, UnitType
- `Models/LaserOperation.cs`: Added SpeedUnit
- `Models/NestLaserProject.cs`: Added cost fields + LastCostEstimate
- `Models/DxfExportReport.cs`: Added cost fields
- `ViewModels/MainViewModel.cs`: Cost properties, commands, methods, project save/restore, export integration
- `Services/DxfService.cs`: Export() cost parameter, report section
- `Views/MainWindow.xaml`: Added Cost/Teklif tab

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 8H: Material Database & Machine Profiles

### Completed
- **MaterialProfile model**: Id, Name, Category, ThicknessMm, Density, Notes, IsDefault, DisplayName
- **MachineProfile model**: Id, Name, Manufacturer, Model, LaserType, WorkingAreaX, WorkingAreaY, Notes
- **MaterialOperationSetting model**: MaterialId, MachineId, OperationType, Power, Speed, PassCount, Frequency, AirAssist, Notes
- **MaterialProfileService**: Full CRUD for all three types with JSON persistence in AppData/NestLaser/profiles/
- **Seed data**: 18 materials (MDF/Pleksi/Kontraplak/Paslanmaz/Galvaniz/Deri/Karton/Kumaş with various thicknesses), 7 machines (Ruida 100W/130W/150W, CO2 Generic 80W, Fiber 20W/30W/50W), 28 operation settings (covering MDF/Pleksi + Ruida combinations for CutOuter/CutInner/Engrave/Mark)
- **MainViewModel integration**: Materials/Machines/MaterialSettings collections, SelectedMaterial/SelectedMachine bindings, ApplyMaterialSettingsToCurrentOperation(), CRUD commands
- **Malzemeler tab**: New tab in the sidebar with material selection (filterable), machine selection, CRUD buttons, operation settings info, and "Apply to Operation" button
- **Project system**: SelectedMaterialId and SelectedMachineId saved in .nelp, restored on load
- **Export report**: Material name and machine name shown in MALZEME & MAKİNE section of export-report.txt
- **Operation auto-suggest**: When material and machine are selected, Power/Speed/Pass are auto-suggested from matching MaterialOperationSetting

### Files Created
- `Models/MaterialProfile.cs`
- `Models/MachineProfile.cs`
- `Models/MaterialOperationSetting.cs`
- `Services/MaterialProfileService.cs`

### Files Changed
- `Models/NestLaserProject.cs`: Added SelectedMaterialId, SelectedMachineId
- `Models/DxfExportReport.cs`: Added MaterialName, MachineName
- `ViewModels/MainViewModel.cs`: Added material/machine fields, properties, commands, methods
- `Services/DxfService.cs`: Added material/machine parameters to Export(), updated report
- `Views/MainWindow.xaml`: Added Malzemeler tab

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 8G: Operation Manager & Production Pipeline

### Completed
- **LaserOperation model**: Added `LayerName` and `Color` fields to track source layer info directly on operations. Updated `Clone()` and all creation sites.
- **PartModel**: Added `IsInnerCandidate` and `IsOuterCandidate` boolean flags for geometry classification. Updated `Clone()`.
- **Layer name auto-mapping**: New `MapLayerNameToOperationType()` helper analyzes layer names for keywords (engrave, mark, reference, cut) and suggests the matching OperationType. Integrated into `AutoSuggestOperations()` as a supplement to existing LayerType mapping.
- **Inner/Outer candidate marking**: `AnalyzeInnerOuterCut()` now sets `IsInnerCandidate`/`IsOuterCandidate` on all PartModel objects based on polygon containment analysis.
- **Export report enhancement**: Added total operation count and active operation count to the operation order section. Added `TotalOperationCount` and `ActiveOperationCount` fields to `DxfExportReport`.
- **ARCHITECTURE.md**: Created with full layer diagram (Geometry, Nesting, Import/Export, Project, Operation, UI), data flow documentation, future-phase preparation notes (NFP, Common Line Cutting, Toolpath), and key design decisions.

### Files Changed
- `Models/LaserOperation.cs`: Added `LayerName`, `Color` fields + Clone update
- `Models/PartModel.cs`: Added `IsInnerCandidate`, `IsOuterCandidate` + Clone update
- `Models/DxfExportReport.cs`: Added `TotalOperationCount`, `ActiveOperationCount`
- `ViewModels/MainViewModel.cs`: Added `MapLayerNameToOperationType()`, updated `AutoSuggestOperations()`, `AnalyzeInnerOuterCut()`, `AddOperation()`, `EnsureDefaultOperations()`
- `Services/DxfService.cs`: Updated export report section with operation counts

### Files Created
- `docs/ARCHITECTURE.md`
- `docs/PROJECT_MEMORY.md`
- `docs/DEVELOPMENT_LOG.md`
- `docs/ROADMAP.md`
- `docs/TEST_NOTES.md`

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 8F.1: Geometry Integrity Cleanup

### Completed
- **MirrorX/MirrorY winding fix**: Replaced `Vertices.Reverse()` with `NormalizeWinding()` to preserve CCW winding after mirror operations (Polygon.cs:206,219).
- **Polygon.IsValid()**: Added validation checks (≥3 vertices, area > 1e-9, no NaN/Infinity) at Polygon.cs:223.
- **Polygon.CleanupVertices()**: Added sequential duplicate removal, closing-duplicate removal, and collinear point removal with repeat-pass (Polygon.cs:236).
- **DxfService validation pipeline**: Added `NormalizeWinding()` → `CleanupVertices()` → `IsValid()` sequence in `Import()`. Invalid geometries are skipped with warning messages.
- **Debug counters**: Added `InvalidRemovedCount`, `DuplicatesCleanedCount`, `WindingNormalizedCount` static counters to DxfService.
- **Undo NestResult preservation**: `TakeSnapshot()` now saves `NestResult` (was null). `RestoreSnapshot()` restores it and calls `RemapNestResultPartReferences()`.
- **TECHNICAL_AUDIT.md**: Marked 3 Medium findings as fixed, added detail sections.

### Fixed Issues
1. **Medium**: Polygon winding not normalized on DXF import — ✅ Fixed
2. **Medium**: MirrorX/MirrorY flips polygon winding — ✅ Fixed
3. **Medium**: Undo snapshot always discards NestResult — ✅ Fixed

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 8F: Technical Audit & Expected Behavior Validation

### Completed
- Audited 11 systems across the codebase
- **Critical fix**: NestPlacement.Part deserialization duplication — added `RemapNestResultPartReferences()`
- **High fix**: DeleteSelectedLayer orphans Operation.LayerId — remap to fallback layer
- **High fix**: Export report path crash — null-check + CreateDirectory + try-catch
- Created `docs/TECHNICAL_AUDIT.md`

### Build
- `dotnet build` → ✅ 0 warning, 0 error

---

## 2026-06-13 — FAZ 8E: Operation Manager & Laser Process Pipeline

### Completed
- OperationType enum (Engrave, Mark, CutInner, CutOuter, Reference)
- LaserOperation model with INotifyPropertyChanged
- Operations tab in MainWindow.xaml with drag-drop list, CRUD, properties
- AutoSuggestOperations from layers with inner/outer detection
- AnalyzeInnerOuterCut using polygon containment (GeometryUtils.PolygonContainsPolygon)
- Operation Preview mode with type-based coloring
- Drag-drop operation reordering in MainWindow.xaml.cs
- Undo/redo integration (Operations cloned in snapshots)
- DXF export report with operation order
- Project system integration (.nelp serialization)

### Build
- `dotnet build` → ✅ 0 warning, 0 error
# Phase 8N - Project Portability & Data Migration

- Added `ProjectMigrationService` with 1.0.0 migration baseline.
- Added `ProjectIntegrityService`, `ProjectLoadResult`, and `ProjectRecoveryReport` for non-silent project recovery.
- Added `ProjectBackupService` with dated `Backups` folder and latest-10 retention.
- Added `ProjectPackageService` for `.nelpkg` export/import.
- Expanded project snapshots to include material, machine, operation settings, cost settings, company profile, and PDF settings.
- Added File menu commands for project package export/import.
- Added portability regression tests. Test count is now 28.
