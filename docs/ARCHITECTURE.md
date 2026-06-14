# NestLaser Desktop — Architecture

## Layer Diagram

```
┌──────────────────────────────────────────────────┐
│                   UI Layer                        │
│  MainWindow.xaml / MainWindow.xaml.cs             │
│  Canvas Rendering, Mouse/Snap/Select              │
│  Tab Control: Parts, Transform, Properties,       │
│  Layers, Operations, Materials, Cost, Analysis,    │
│  Doğrulama, History                                 │
└──────────────────────┬───────────────────────────┘
                       │ binds to
┌──────────────────────▼───────────────────────────┐
│              ViewModel Layer                      │
│  MainViewModel.cs                                 │
│  State management, commands, undo/redo            │
│  NestResult, Part selection, layer/op CRUD        │
│  Material/Machine selection, auto-suggest         │
│  Cost estimation, quotation generation, PDF export│
│  Profile snapshot restore warnings                │
│  Async workflow orchestration + progress state     │
└──────┬──────────┬──────────┬──────────┬──────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Operation│ │ Material │ │  Cost    │ │ Project  │ │ Import/  │
│ Layer    │ │ & Machine│ │  Layer   │ │ Layer    │ │ Export   │
│          │ │ Layer    │ │          │ │          │ │ Layer    │
│ LaserOp  │ │ Material │ │ CostSett │ │ .nelp    │ │ DXF      │
│ Operation│ │ Profile  │ │ ings     │ │ Save/Load│ │ Parse    │
│ Type     │ │ Machine  │ │ JobCost  │ │ UndoSnap │ │ DXF Wri- │
│ Auto-    │ │ Profile  │ │ Estimate │ │ shot     │ │ te       │
│ suggest  │ │ Settings │ │ Quotatio │ │          │ │ Report   │
└──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
┌──────────────────────────────────────────────────┐
│                  Nesting Layer                    │
│  NestingEngine.cs                                 │
│  Free Rectangle + Polygon Collision + Irregular   │
│  TrueShape candidate / gap-fill debug counters     │
│  Collision cache, timeout fallback                │
└──────────────────────┬───────────────────────────┘
                       │
                       ▼
            ┌──────────────────┐
            │  Geometry Layer   │
            │                  │
            │ Polygon          │
            │ Point2D          │
            │ BoundingBox      │
            │ GeometryUtils    │
            │ (SAT, PIP, NFP)  │
            └──────────────────┘
```

## Layer Responsibilities

### 1. Geometry Layer
- **Files:** `Geometry/Polygon.cs`, `Geometry/Point2D.cs`, `Geometry/BoundingBox.cs`, `Geometry/GeometryUtils.cs`
- Core geometric primitives and algorithms
- Polygon winding (CCW convention), area, bounds, transforms
- SAT collision detection, point-in-polygon, polygon-contains-polygon
- Future: No-Fit Polygon (NFP), Common Line Cutting, Offset Path, Toolpath generation

### 2. Nesting Layer
- **Files:** `Nesting/NestingEngine.cs`
- Free Rectangle (Guillotine) placement algorithm
- Polygon Collision mode with SAT exact check
- Irregular experimental mode with anchor-point generation
- TrueShape mode with explicit anchor translation, gap sampling, and debug counters
- Collision cache by (partId, rot, x, y, otherIdx)
- Timeout-based fallback mechanism

### 3. Import/Export Layer
- **Files:** `Services/DxfParser.cs`, `Services/DxfHeaderParser.cs`, `Services/DxfService.cs`, `Services/ImportWorkflowService.cs`, `Services/ExportWorkflowService.cs`
- DXF file parsing (LWPOLYLINE, POLYLINE, CIRCLE, ARC, LINE, SPLINE, ELLIPSE)
- DXF header parsing: $INSUNITS, $MEASUREMENT, $LUNITS → ImportUnitInfo (unit detection)
- Unit conversion pipeline: detected/overridden scale factor applied to all vertex coordinates; all geometry stored in mm internally
- Geometry validation pipeline on import: NormalizeWinding → CleanupVertices → IsValid
- DXF export with LWPOLYLINE output, ACI color mapping, multi-plate layout
- Export report generation (part/plate counts, efficiency, layers, operation order, material/machine)
- Debug counters: InvalidRemovedCount, DuplicatesCleanedCount, WindingNormalizedCount
- Workflow services provide Task-based import/export execution and progress reporting

### 4. Project Layer
- **Files:** `Services/ProjectService.cs`, `Services/ProjectWorkflowService.cs`, `Models/NestLaserProject.cs`, `Models/ProfileSnapshot.cs`, `Services/SafeJsonFileService.cs`
- JSON serialization/deserialization (.nelp format)
- Full state save/restore: Parts, Layers, Operations, NestResult, Plate, Settings, Material/Machine
- Recent projects tracking (AppData/NestLaser/recent-projects.json)
- Safe write path: temp file → replace → `.bak`
- ProfileSnapshot embeds selected material/machine profile data for project portability
- UndoSnapshot system with deep clone + NestResult remapping
- ProjectWorkflowService provides async open/save wrappers and recent-project list management

### 5. Operation Layer
- **Files:** `Models/LaserOperation.cs`, `Models/OperationType.cs`
- Operation model: Id, Name, LayerId, LayerName, Color, Type, Power, Speed, PassCount, Priority, Enabled
- OperationType enum: Engrave, Mark, CutInner, CutOuter, Reference
- Layer name auto-mapping: "engrave" → Engrave, "mark" → Mark, etc.
- Inner/Outer cut candidate analysis via polygon containment
- Priority-based ordering for production flow
- Material/Machine-based auto-suggest for Power/Speed/Pass

### 6. Material & Machine Layer
- **Files:** `Models/MaterialProfile.cs`, `Models/MachineProfile.cs`, `Models/MaterialOperationSetting.cs`, `Services/MaterialProfileService.cs`, `Services/SafeJsonFileService.cs`
- Material profiles: Name, Category, Thickness, Density, UnitPrice, UnitType
- Machine profiles: Manufacturer, Model, LaserType, WorkingArea
- Operation settings: Material + Machine + OperationType → Power/Speed/Pass/Frequency/AirAssist
- JSON persistence in AppData/NestLaser/profiles/ (materials.json, machines.json, operation-settings.json)
- Profile JSON writes use temp/replace/backup and parse failures are logged
- Seed data on first run: 18 materials, 7 machines, 28 operation settings
- Future: AI Recommendation, Material Optimization, Machine Analytics

### 7. Cost Estimation Layer
- **Files:** `Models/CostSettings.cs`, `Models/JobCostEstimate.cs`, `Models/UnitType.cs`, `Models/SpeedUnit.cs`, `Models/CurrencyType.cs`, `Services/CostEstimationService.cs`, `Services/CostWorkflowService.cs`
- CostSettings: Machine hourly rate, operator rate, electricity, consumable, engraving rate, flags
- Cost estimation: cutting length per operation type, time from speed/pass, material cost by unit type, waste, machine/labor/electricity/consumable
- JobCostEstimate: full cost breakdown with provided/optional cost components
- Quotation text generation (plain text, clipboard-ready)
- Progressive rounding by price tier
- CostSettings persisted in AppData/NestLaser/cost-settings.json
- Cost settings use safe JSON load/save
- Time estimation is computed per operation geometry to avoid global cut-length multiplication when multiple operations exist on one layer
- CostWorkflowService owns cost validation, settings assembly, async calculation, and quotation text forwarding

### 7.5 PDF Report Layer
- **Files:** `Services/PdfReportService.cs`, `Models/CompanyProfile.cs`, `Models/PdfReportSettings.cs`
- Native PDF writer for quotation, production report, and preview outputs
- Uses `JobCostEstimate`, `NestResult`, `PlateModel`, material/machine profiles, operations, and `CompanyProfile`

### 7.6 Diagnostics & Test Layer
- **Files:** `Services/AppLogger.cs`, `Services/WorkflowProgress.cs`, `NestLaserDesktop.Tests/`, `.github/workflows/dotnet.yml`
- Crash logging target: `%APPDATA%/NestLaser/logs/error-log.txt`
- Handlers: AppDomain unhandled exceptions, WPF Dispatcher unhandled exceptions, TaskScheduler unobserved task exceptions
- xUnit regression suite covers geometry, DXF import/export, project save/load, cost formulas, PDF smoke, layer clone, and operation clone
- GitHub Actions executes restore, build, and test on Windows
- WorkflowProgress supports long-operation overlay messages and determinate progress
- Draws nesting preview directly into the PDF as vector plate/part geometry
- Embeds logo from `CompanyProfile.LogoPath` when the image can be loaded
- Persists per-project company and last PDF export settings in `.nelp`

### 8. UI Layer
- **Files:** `Views/MainWindow.xaml`, `Views/MainWindow.xaml.cs`
- Tab-based sidebar: Parts, Transform, Properties, Layers, Operations, **Materials**, **Cost**, Analysis, **Doğrulama**, History
- Canvas rendering with zoom/pan/fit, grid, rulers, snap
- Label display toggles: ShowPartNames, ShowLayerNames, ShowOperationNames (zoom-dependent, 75% opacity, hidden below 0.5× zoom)
- Import Verification panel: source unit, scale, bounding box, reference dimension input with suggested scale
- Nesting Benchmark: compare Free Rectangle / Polygon Collision / Irregular side-by-side with timing and stats
- Drag-drop operation reordering
- **Measurement Tool:** İki nokta arası mesafe, ΔX, ΔY, açı ölçümü. Overlay + status bar entegrasyonu.
- **CAD Marquee Selection:** Sol→sağ = tam içeridekiler, sağ→sol = temas edenler standardı.
- **Bounding Inspector:** Properties panelinde Area, Perimeter, Total Area.
- **Brush Cache:** Tüm brush/pen'ler static frozen olarak cache'lendi (FAZ 8O).
- Operation Preview mode with type-based coloring:
  - Engrave = Blue (#569CD6)
  - Mark = Yellow (#D6C656)
  - CutInner = Orange (#D68656)
  - CutOuter = Red (#D65656)
  - Reference = Gray (#808080)

## Data Flow

```
DXF File → DxfHeaderParser.DetectUnit → ImportUnitInfo
  └→ DxfParser.Parse → DxfEntity list
    └→ DxfService.Import (applies scale × vertices)
      └→ NormalizeWinding → CleanupVertices → IsValid
        └→ PartModel list (scaled to mm)
          └→ MainViewModel.Parts
            └→ NestingEngine.Run → NestResult
              └→ ValidateFinalPlacements (quality guard)
  └→ DxfService.Export → Output DXF + Report
    └→ WriteExportReport (includes Operation Order, Material, Machine, Cost)
```

```
.nelp File → ProjectService.LoadProject
  └→ MainViewModel.ApplyProject
    └→ RemapNestResultPartReferences()
    └→ Parts, Layers, Operations restored
    └→ SelectedMaterial, SelectedMachine restored by ID
```

```
Material/Machine Selection → ApplyMaterialSettingsToCurrentOperation()
  └→ MaterialProfileService.FindSetting(materialId, machineId, operationType)
    └→ Returns MaterialOperationSetting
      └→ SelectedOperation.Power/Speed/PassCount updated

```
Cost Estimation → CalculateCostCommand
  └→ CostEstimationService.Calculate(project, material, machine, parts, layers, ops, nestResult, plate, settings)
    └→ CalculateCuttingLengths (perimeter × PassCount per operation type)
    └→ CalculateEstimatedTime (speed → mm/min, length/speed × passCount)
    └→ CalculateMaterialCost (PerSheet / PerSquareMeter / PerKg)
    └→ CalculateWasteCost (materialCost × wastePercent / 100)
    └→ CalculateMachineCost / CalculateLaborCost / CalculateElectricityCost / CalculateConsumableCost
    └→ CalculateSuggestedPrice (cost × (1 + profitMargin%))
    └→ CalculateFinalPrice (suggested × (1 + vat%))
    └→ Progressive rounding
    └→ Returns JobCostEstimate
      └→ LastCostEstimate bound to Cost/Teklif tab
```

```
PDF Export → ExportQuotationPdfCommand / ExportProductionPdfCommand
  └→ ExportWorkflowService.ValidatePdfInputs
    └→ ExportWorkflowService.CreatePdfAsync
      └→ PdfReportService.CreateQuotationPdf / CreateProductionReportPdf
      └→ Header with CompanyProfile and optional logo
      └→ Vector nesting preview from NestResult.Placed
      └→ Material, production, cost, and sales summaries
```

```
Long Operation → MainViewModel async command
  └→ BeginBusy + Progress<WorkflowProgress>
    └→ WorkflowService.*Async on background Task
      └→ UI thread receives result
        └→ ObservableCollection / bound state updated
        └→ RequestDrawPreview
```
```

## Undo/Redo Flow

```
PushUndo → TakeSnapshot (clones Parts, Layers, Operations, NestResult)
  └→ _undoStack.Push(snapshot)
  └→ _redoStack.Clear()

Undo → TakeSnapshot("current") → _redoStack.Push(current)
  └→ RestoreSnapshot(prev)
    └→ Restore Parts, Layers, Operations, NestResult
    └→ RemapNestResultPartReferences()
```

## Material/Machine Data Storage

```
AppData/NestLaser/profiles/
  ├── materials.json          ← List<MaterialProfile>
  ├── machines.json           ← List<MachineProfile>
  └── operation-settings.json ← List<MaterialOperationSetting>
```

Created automatically with seed data on first run. Editable via the Materials tab in-app or by editing JSON files directly.

## Cost Data Storage

```
AppData/NestLaser/
  ├── profiles/                 ← Material/Machine profiles
  ├── cost-settings.json        ← CostSettings (system-wide defaults)
```

Cost settings stored in AppData (system-wide, not per-project). Per-project cost data (profit margin, VAT, currency, last estimate) stored inside .nelp.

## Future-Phase Preparation

### No-Fit Polygon (NFP)
- Input: Two Polygon objects (stationary + orbiting)
- Output: NFP polygon representing all collision-free positions
- Depends on: CCW winding (✅ normalized on import), CleanupVertices (✅), IsValid (✅)

### Common Line Cutting
- Input: Two adjacent NestPlacement polygons
- Output: Shared edge segments for reduced cutting path
- Depends on: Polygon containment hierarchy (inner/outer candidate marking ✅)

### Toolpath Generation
- Input: LaserOperation + Polygon geometry
- Output: G-code or proprietary toolpath commands
- Depends on: Operation order (✅ Priority field), inner/outer distinction (✅)

### PDF Quotation & Customer Management
- Status: ✅ Implemented in FAZ 8J for quotation and production reports
- Input: JobCostEstimate + CompanyProfile + NestResult
- Output: PDF document with company header/logo, nesting preview, cost and sales summaries
- Future work: customer database, quote history, terms templates, work order approval flow

### AI Recommendation
- Input: Historical JobCostEstimate + MaterialProfile + MachineProfile + OperationType
- Output: AI-optimized Power/Speed/Pass and suggested price
- Depends on: Cost Estimation infrastructure (✅ FAZ 8I)

### Automatic Best Algorithm Selection
- Input: Benchmark results from current dataset
- Output: Recommended NestAlgorithm
- Depends on: Benchmark mode infrastructure (✅ FAZ 9X)

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| CCW winding convention | SAT collision is winding-independent, but NFP requires consistent winding. CCW chosen as standard. |
| NestResult in UndoSnapshot | Non-geometry undo (layer rename) should not lose nesting results. RemapNestResultPartReferences reconnects Part references by Id. |
| Polygon validation on import | Catches degenerate geometry early. CW → CCW conversion, duplicate removal, colinear cleanup. |
| Operation stored in .nelp | Operations are part of project state, not regenerated on load. Full CRUD with undo support. |
| AutoSuggest from layer name | Reduces manual configuration. Name-based hints supplement LayerType mapping. |
| Material/Machine in AppData JSON | Profiles are system-wide (not per-project) but selection is project-specific. Users edit profiles once, use in many projects. |
| Material/Machine selection IDs in .nelp | Only the selected material/machine IDs are stored in project files, not the full profile data. Decouples project from profile definitions. |
| CostSettings in AppData (not .nelp) | System-wide default rates (machine hourly, operator, electricity) are shared across projects. Per-project overrides (profit margin, VAT, currency) stored in .nelp. |
| SpeedUnit on LaserOperation | Operations created from scratch default to MmPerSecond (matching seed data convention 20 mm/s). When converting to mm/min for time estimation, multiply by 60. |
| UnitType on MaterialProfile | Material cost can be calculated per sheet, per square meter, or per kg without changing the cost estimation formula. |
| Progressive rounding | Avoids unrealistic precision at different price tiers: under 10 TRY → 1 decimal, 10–100 → nearest 5, etc. |
| Quotation as PDF | FAZ 8J adds direct PDF quotation and production report generation while keeping clipboard text export for quick sharing. |
| DXF unit detection | HEADER stream parsing for $INSUNITS/$MEASUREMENT/$LUNITS. Non-blocking, stops at CLASSES/ENDSEC. Fallback to mm with warning if no variables found. |
| Unit conversion on import | All vertex coordinates scaled by ScaleFactorToMm immediately after parsing. All internal geometry stored in mm. Users can override via manual scale factor. |
| ImportUnitInfo passed to Import() | Optional override parameter allows both auto-detection and manual control. When null, auto-detect; when provided, use the given scale. |
| Label display toggles + zoom gate | Labels disabled by default (clean canvas). Show only when zoom > 0.5× to avoid clutter at low zoom. 75% opacity for subtle rendering. Selection-only display not implemented — always shows for all visible parts when enabled. |
| Benchmark runs all 3 algorithms sequentially | Single-threaded, UI blocks during benchmark. Acceptable for now as each run is <10s. Results displayed in Doğrulama tab as formatted text. |
| Quality guard as built-in validation | ValidateFinalPlacements runs after every nesting operation, not just benchmark. Invalid placements are demoted to Unplaced with warnings. This prevents corrupt nesting results from reaching export. |
# Phase 8N Architecture Update

## Project Portability Layer

- `ProjectMigrationService`: upgrades loaded projects to the current schema baseline (`1.0.0`).
- `ProjectIntegrityService`: validates and repairs IDs, layer references, operation references, and profile references after load/import.
- `ProjectBackupService`: creates dated project backups before overwrite and keeps the latest 10 revisions.
- `ProjectPackageService`: exports/imports `.nelpkg` zip packages containing project data and embedded snapshots.
- `ProjectLoadResult` / `ProjectRecoveryReport`: carry recovery, corruption, migration, and integrity information without silently swallowing problems.

The project system now separates raw file IO, migration, integrity repair, and user workflow orchestration.
