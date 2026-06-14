# Technical Audit Report — NestLaser Desktop

**Audit Date:** 2026-06-13
**Scope:** FAZ 8A–8G feature set
**Audit Type:** Static code analysis + expected behavior validation
**Build Status:** ✅ 0 warning, 0 error

---

## 1. DXF Import

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| LWPOLYLINE | Parsed from code 10/20 pairs | All closed polylines imported | — | — |
| POLYLINE | Parsed via VERTEX/SEQEND | Same as LWPOLYLINE | — | — |
| CIRCLE/ARC/ELLIPSE | Tessellated to line segments (36 segs) | Smooth curves approximated | Tessellation error grows with segment count | Low |
| LINE | 2-vertex polyline | Single edge imported | — | — |
| Poly winding | NormalizeWinding() called in DxfService.Import pipeline | Consistent CCW winding | — | Medium ✅ Fixed |
| Zero-area polygons | Not filtered | Degenerate geometry should be skipped | Poor nesting performance | Low |
| Duplicate vertices | Not removed | Colinear/near-identical points should be cleaned | Minor numerical noise | Low |
| Unit handling | Assumes mm | No DXF unit header parsing | — | Low |

---

## 2. DXF Export

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| LWPOLYLINE output | WriteLwPolyline with AC1015 header | RDWorks/CorelDRAW compatible | — | — |
| ACI color mapping | GetAciFromHex maps hex → ACI index | Standard colors matched | Falls back to 7 (white) for unknown | Low |
| Multi-plate offset | BuildPlateLayouts with 20mm gap | Plates side-by-side | — | — |
| Unplaced parts | Tiled right of last plate | Organized unplaced area | — | — |
| Export report | WriteExportReport to export-report.txt | Report includes efficiency/waste/layers | **Path vulnerability — Path.GetDirectoryName may return null** | **High ✅ Fixed** |
| Report write safety | No try-catch around File.WriteAllText | Should not fail export on report write error | **Report write failure crashes export** | **High ✅ Fixed** |
| Directory creation | Not checked | Directory should be created if missing | **Causes export failure if target directory missing** | **High ✅ Fixed** |

---

## 3. Project System (.nelp)

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Serialization | System.Text.Json, ReferenceHandler.IgnoreCycles | Full state save/restore | — | — |
| NestResult load | Deserialized as independent object graph | Part references must match Parts list | **NestPlacement.Part is a different object from Parts[] after load → stale data on selection/display** | **Critical ✅ Fixed** |
| Undo NestResult | Preserved in snapshot, remapped on restore | Nesting results preserved for non-geometry undo | — | Medium ✅ Fixed |
| Polygon deserialization | Standard JSON array → List<Point2D> | Vertices restored correctly | — | — |
| Recent projects | AppData/NestLaser/recent-projects.json | Last 5 projects listed | — | — |

---

## 4. Layer System

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| CRUD | Add, delete, rename, reorder | Layers managed | — | — |
| Visibility toggle | Hides parts from canvas, nest, export | Consistent across all systems | — | — |
| Lock toggle | Prevents selection and transform | Consistent across all systems | — | — |
| Layer delete + operations | Does not update referencing Operations | Operations referencing deleted layer should be cleaned/mapped | **Orphaned operation.LayerId** | **High ✅ Fixed** |
| Assign parts to layer | Updates part.LayerId/LayerName | Parts moved to target layer | — | — |
| Part-layer sync | NormalizePartLayers ensures consistency | Default layers created if missing | — | — |

---

## 5. Preview / Zoom / Pan / Fit

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Zoom to cursor | MouseWheel: factor 1.15, clamp 0.01–100 | Smooth zoom-to-cursor | — | — |
| Pan | Middle-button or Space+Left | Infinite canvas pan | — | — |
| Fit to screen | Calculates bounds from plates/parts | All content visible | **Unplaced parts offset not included in NestResult mode bounds** | Low |
| Grid | Adaptive step (GetEffectiveStep), major/minor lines | User-selected base step | — | — |
| Rulers | Horizontal + vertical with tick labels | mm labels | — | — |
| Render performance | Full canvas clear + redraw on each frame | Smooth for < 100 parts | Redraws all children every time | Low |
| Ghost overlay | No double-buffer artifacts observed | Clean | — | — |

---

## 6. Selection / Multi Selection

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Single click | HitTest → select BBox match | Intuitive part selection | — | — |
| Ctrl+click | Toggle selection | Add/remove from selection | — | — |
| Shift+click | Range select from last | Sequential selection | — | — |
| Rectangle drag | HitTestRect → SelectByRect | Rubber-band select | **Zero-size rect doesn't filter edge case** | Low |
| Selection highlight | Red outline + corner handles | Clear visual feedback | — | — |

---

## 7. Transform: Move / Scale / Rotate / Mirror

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Move selected | geometry.Move(dx, dy) | Free movement in world space | — | — |
| Scale selected | geometry.Scale(factor) centroid | Proportional scaling | — | — |
| Scale all | Same as selected | Global scaling | — | — |
| Rotate 90° | Rotate90AroundCenter | 90° fixed rotation | — | — |
| Mirror X/Y | MirrorX/Y + NormalizeWinding() | Geometry mirrored, CCW preserved | — | Medium ✅ Fixed |
| Numeric properties | ApplyProperties: position + size + rotation | Precise numeric control | — | — |
| Aspect ratio lock | MaintainAspectRatio on width/height sync | Proportional resize | — | — |

---

## 8. Snap System

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Grid snap | Round to GridStepMm | Precise alignment | — | — |
| Vertex snap | Match vertex positions within tolerance | Magnetic to vertices | — | — |
| Edge snap | Match bounding edge positions | Magnetic to edges | — | — |
| Center snap | Match bounding center | Magnetic to centers | — | — |
| Snap visual | Yellow crosshair + label | Feedback on active snap | — | — |
| Grid step vs snap step | Snap uses raw GridStepMm, display uses GetEffectiveStep | Visual grid adapts but snap uses configured step | Intentional — user sets snap resolution independently | Info |

---

## 9. Undo / Redo

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Stack depth | 50 levels | Depth-limited | — | — |
| Snapshot scope | Full Parts/Layers/Operations clone | Complete state | — | — |
| NestResult in snapshot | Preserved in snapshot, remapped on restore | Nesting results preserved where possible | — | Medium ✅ Fixed |
| Stack cleanup | Cleared on NewProject/OpenProject | Fresh state | — | — |

---

## 10. Nesting Algorithms

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Free Rectangle | Guillotine maximal rect split + prune + merge | Fast rectangular packing | — | — |
| Polygon Collision | FreeRect + SAT exact check | Collision-safe placement | — | — |
| Irregular Experimental | Anchor-point generation + SAT + scoring | Dense irregular packing | — | — |
| Fallback | After 10s timeout → FreeRectangle | Graceful degradation | — | — |
| Collision cache | Dictionary keyed by (partId, rot, x, y, otherIdx) | Reuses collision results | 0.1mm cache key precision may cause misses | Low |
| New plate on overflow | Always adds new plate | Allows unlimited plates | Could create excessive plates for oversized parts | Low |
| Winding dependency | SAT is winding-independent | Collision works regardless of DXF winding | — | — |
| Concurrent execution | Single-threaded, synchronous | UI blocks during nesting | Freezes UI for 10+ seconds | Low |

---

## 11. Analysis Panel

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Efficiency | %Efficiency from UsedArea/TotalPlateArea | Accurate percentage | — | — |
| Waste rate | %WasteRate complement of efficiency | Consistent | — | — |
| Placed/Unplaced counts | Sum from NestResult | Accurate | — | — |
| Algorithm display | Name + fallback status | Shows actual algo | — | — |
| Performance stats | PlacementAttempts, Candidates, CacheHits, CollisionChecks | Debug visibility | — | — |
| Warnings | List from NestResult | Shows fallback/validation issues | — | — |

---

---

## 12. Operation Manager

| Area | Current Behavior | Expected Behavior | Issues | Priority |
|------|-----------------|-------------------|--------|----------|
| Model fields | Id, Name, LayerId, LayerName, Color, Type, Power, Speed, PassCount, Priority, Enabled | Full operation definition | — | — |
| INotifyPropertyChanged | All properties fire change notification | WPF binding works | — | — |
| Clone() | Copies all fields including LayerName, Color | Complete copy | — | — |
| Type enum | Engrave, Mark, CutInner, CutOuter, Reference | Covers all laser ops | — | — |
| Auto-suggest from layers | Creates operations per layer, name analysis supplements LayerType | Reduces manual config | — | — |
| Layer name mapping | "engrave"→Engrave, "mark"→Mark, "reference"→Reference, "iç"/"ic"/"inner"→CutInner | Name-based hint system | — | — |
| Inner/Outer analysis | PartModel.IsInnerCandidate/IsOuterCandidate set via polygon containment | Geometry classification | — | — |
| Drag-drop reorder | PreviewMouseLeftButtonDown → PreviewMouseMove → Drop | Intuitive reordering | — | — |
| Priority system | Auto-renumbered on move/add/delete | Consistent ordering | — | — |
| Operation Preview | Type-based colors (Blue/Yellow/Orange/Red/Gray) | Visual operation check | — | — |
| Export report | Operation order listed with total/active counts | Production-ready output | — | — |
| Project serialization | Operations stored in .nelp, restored on load | Full persistence | — | — |
| Undo/redo | Operations cloned in snapshots | Complete undo support | — | — |
| Auto-suggest edge case | No layers → single default CutOuter created | Graceful fallback | — | Low |

---

## Priority Summary

| Priority | Count | Fixed |
|----------|-------|-------|
| **Critical** | 1 | ✅ 1/1 |
| **High** | 2 | ✅ 2/2 |
| **Medium** | 4 | ✅ 4/4 |
| Low | 7 | — (documented) |

---

## Fixed Issues Detail

### Critical: NestPlacement.Part deserialization duplication
- **File:** `ViewModels/MainViewModel.cs`
- **Problem:** After loading a .nelp project, `NestResult.Placed[].Part` referred to different `PartModel` objects than the main `Parts` list. Layer changes on a part in the list did not reflect in the displayed nesting result, and selection matching via part name could hit the wrong placement.
- **Fix:** Added `RemapNestResultPartReferences()` called in `ApplyProject()`. After loading, it rebuilds the part-by-ID dictionary and remaps every `NestPlacement.Part` and `NestResult.Unplaced` entry to the matching object in the canonical `Parts` list.
- **Test:** After .nelp load, changing a part's layer updates both the list item and the nesting preview.

### High: DeleteSelectedLayer orphans Operation.LayerId
- **File:** `ViewModels/MainViewModel.cs`
- **Problem:** When a layer was deleted, any `LaserOperation` referencing that `LayerId` was left with a dangling reference. The operation would still appear in the list but pointed to a nonexistent layer.
- **Fix:** Before removing the layer, iterate through `Operations` and update each operation's `LayerId` to the fallback layer's ID.
- **Test:** Deleting a layer that has associated operations now remaps them to the fallback layer.

### High: Export report path crash
- **File:** `Services/DxfService.cs`
- **Problem:** `Path.GetDirectoryName()` returns `null` for root-relative paths (e.g., `"output.dxf"`). When concatenated with `"export-report.txt"`, the result was `"export-report.txt"` — but the directory might not exist. `File.WriteAllText` would throw if the directory was missing.
- **Fix:** Fall back to `Directory.GetCurrentDirectory()` when `GetDirectoryName` is null. Call `Directory.CreateDirectory()` before writing. Wrap `File.WriteAllText` in try-catch so export continues even if report write fails.
- **Test:** Export with a bare filename like `"nested.dxf"` now creates the report in the current directory.

### Medium: Polygon winding not normalized on DXF import
- **Files:** `Services/DxfService.cs`, `Geometry/Polygon.cs`
- **Problem:** `DxfService.Import` created polygons directly from DXF entity vertices without normalizing winding direction. If the DXF was CW, all downstream operations (nesting, collision) would see reversed winding. While SAT collision is winding-independent, future NFP and toolpath generation would produce wrong results.
- **Fix:** Added a geometry validation pipeline in `DxfService.Import`: after calculating the polygon, call `NormalizeWinding()` (ensures CCW), then `CleanupVertices()` (removes duplicate and collinear vertices), then `IsValid()` (rejects degenerate geometry with < 3 vertices, zero area, NaN/Inf).
- **Test:** Import a DXF with CW-wound polylines; all resulting parts have CCW winding. Import a DXF with degenerate/sliver polygons; they are filtered out.

### Medium: MirrorX/MirrorY flips polygon winding
- **Files:** `Geometry/Polygon.cs`
- **Problem:** `MirrorX()` and `MirrorY()` called `Vertices.Reverse()` after mirroring, which always reversed the vertex order regardless of the mirror's effect on winding. This could produce CW winding from CCW input.
- **Fix:** Replaced `Vertices.Reverse()` with `NormalizeWinding()` which checks the signed area and reverses only if the result would be CW.
- **Test:** Mirror a CCW polygon on X then Y; after both operations the winding remains CCW. Mirror is idempotent (mirror twice returns to original winding).

### Medium: Undo snapshot always discards NestResult
- **Files:** `ViewModels/MainViewModel.cs`
- **Problem:** `TakeSnapshot()` set `NestResult = null` unconditionally. When undoing non-geometry operations (e.g., layer rename), the nesting result was lost and had to be recomputed.
- **Fix:** `TakeSnapshot()` now saves the current `NestResult` reference (Parts are cloned with the same Ids). `RestoreSnapshot()` assigns `NestResult = snap.NestResult` and calls `RemapNestResultPartReferences()` to reconnect Part references to the restored Parts list by Id.
- **Test:** Run nesting, then rename a layer, then undo. The nesting result is preserved and displayed correctly.
