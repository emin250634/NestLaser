# Development Log — NestLaser Desktop

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
