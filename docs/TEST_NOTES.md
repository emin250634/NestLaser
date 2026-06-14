# Test Notes — NestLaser Desktop

## Build Verification
- `dotnet build NestLaserDesktop.csproj`
- Expected: ✅ 0 warning, 0 error

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
