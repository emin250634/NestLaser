# NestLaser Desktop — Architecture

## Layer Diagram

```
┌──────────────────────────────────────────────────┐
│                   UI Layer                        │
│  MainWindow.xaml / MainWindow.xaml.cs             │
│  Canvas Rendering, Mouse/Snap/Select              │
│  Tab Control: Parts, Transform, Properties,       │
│  Layers, Operations, Materials, Analysis, History  │
└──────────────────────┬───────────────────────────┘
                       │ binds to
┌──────────────────────▼───────────────────────────┐
│              ViewModel Layer                      │
│  MainViewModel.cs                                 │
│  State management, commands, undo/redo            │
│  NestResult, Part selection, layer/op CRUD        │
│  Material/Machine selection, auto-suggest         │
└──────┬──────────┬──────────┬──────────┬──────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Operation│ │ Material │ │ Project  │ │ Import/  │
│ Layer    │ │ & Machine│ │ Layer    │ │ Export   │
│          │ │ Layer    │ │          │ │ Layer    │
│ LaserOp  │ │ Material │ │ .nelp    │ │ DXF      │
│ Operation│ │ Profile  │ │ Save/Load│ │ Parse    │
│ Type     │ │ Machine  │ │ UndoSnap │ │ DXF Wri- │
│ Auto-    │ │ Profile  │ │ shot     │ │ te       │
│ suggest  │ │ Settings │ │          │ │ Report   │
└──────────┘ └──────────┘ └──────────┘ └──────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
┌──────────────────────────────────────────────────┐
│                  Nesting Layer                    │
│  NestingEngine.cs                                 │
│  Free Rectangle + Polygon Collision + Irregular   │
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
- Collision cache by (partId, rot, x, y, otherIdx)
- Timeout-based fallback mechanism

### 3. Import/Export Layer
- **Files:** `Services/DxfParser.cs`, `Services/DxfService.cs`
- DXF file parsing (LWPOLYLINE, POLYLINE, CIRCLE, ARC, LINE, SPLINE, ELLIPSE)
- Geometry validation pipeline on import: NormalizeWinding → CleanupVertices → IsValid
- DXF export with LWPOLYLINE output, ACI color mapping, multi-plate layout
- Export report generation (part/plate counts, efficiency, layers, operation order, material/machine)
- Debug counters: InvalidRemovedCount, DuplicatesCleanedCount, WindingNormalizedCount

### 4. Project Layer
- **Files:** `Services/ProjectService.cs`, `Models/NestLaserProject.cs`
- JSON serialization/deserialization (.nelp format)
- Full state save/restore: Parts, Layers, Operations, NestResult, Plate, Settings, Material/Machine
- Recent projects tracking (AppData/NestLaser/recent-projects.json)
- UndoSnapshot system with deep clone + NestResult remapping

### 5. Operation Layer
- **Files:** `Models/LaserOperation.cs`, `Models/OperationType.cs`
- Operation model: Id, Name, LayerId, LayerName, Color, Type, Power, Speed, PassCount, Priority, Enabled
- OperationType enum: Engrave, Mark, CutInner, CutOuter, Reference
- Layer name auto-mapping: "engrave" → Engrave, "mark" → Mark, etc.
- Inner/Outer cut candidate analysis via polygon containment
- Priority-based ordering for production flow
- Material/Machine-based auto-suggest for Power/Speed/Pass

### 6. Material & Machine Layer
- **Files:** `Models/MaterialProfile.cs`, `Models/MachineProfile.cs`, `Models/MaterialOperationSetting.cs`, `Services/MaterialProfileService.cs`
- Material profiles: Name, Category, Thickness, Density
- Machine profiles: Manufacturer, Model, LaserType, WorkingArea
- Operation settings: Material + Machine + OperationType → Power/Speed/Pass/Frequency/AirAssist
- JSON persistence in AppData/NestLaser/profiles/ (materials.json, machines.json, operation-settings.json)
- Seed data on first run: 18 materials, 7 machines, 28 operation settings
- Future: Cost Estimation, AI Recommendation, Material Optimization, Machine Analytics

### 7. UI Layer
- **Files:** `Views/MainWindow.xaml`, `Views/MainWindow.xaml.cs`
- Tab-based sidebar: Parts, Transform, Properties, Layers, Operations, **Materials**, Analysis, History
- Canvas rendering with zoom/pan/fit, grid, rulers, snap
- Drag-drop operation reordering
- Operation Preview mode with type-based coloring:
  - Engrave = Blue (#569CD6)
  - Mark = Yellow (#D6C656)
  - CutInner = Orange (#D68656)
  - CutOuter = Red (#D65656)
  - Reference = Gray (#808080)

## Data Flow

```
DXF File → DxfParser → DxfService.Import
  └→ NormalizeWinding → CleanupVertices → IsValid
    └→ PartModel list
      └→ MainViewModel.Parts
        └→ NestingEngine.Run → NestResult
          └→ DxfService.Export → Output DXF + Report
            └→ WriteExportReport (includes Operation Order, Material, Machine)
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

### Cost Estimation / AI Recommendation
- Input: MaterialProfile, MachineProfile, OperationType
- Output: Estimated cost, AI-optimized Power/Speed/Pass
- Depends on: Material/Machine database infrastructure (✅ FAZ 8H)

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
