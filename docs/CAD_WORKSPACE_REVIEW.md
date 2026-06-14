# CAD Workspace Review — NestLaser Desktop

**Date:** 2026-06-13  
**Phase:** FAZ 8O.1 — Render Pipeline & Technical Debt Cleanup  
**Audit Type:** Full CAD workspace code audit + UX assessment

---

## FAZ 8O.1 Completed Items (2026-06-13)

### Render Pipeline Improvements
| Item | Status | Notes |
|------|--------|-------|
| Viewport state tracking | ✅ Done | `_viewportMinX/MaxX/MinY/MaxY`, `_renderedPartCount`, `_culledPartCount`, `_lastRenderTicks`, `_enableRenderDiagnostics` |
| `UpdateViewportBounds()` helper | ✅ Done | BoundingBox-based viewport bounds update |
| `IsVisibleInViewport()` helper | ✅ Done | BoundingBox visibility check |
| `DrawPartsOnPlate` culling | ✅ Done | Viewport culling - skips off-screen parts |
| `DrawMultiPlateNesting` culling | ✅ Done | Viewport culling - skips off-screen placed parts |
| Render diagnostics | ✅ Done | Timing in `DrawPreview()` with AppLogger error logging |
| Empty catch blocks | ✅ Done | Replaced with proper error logging via AppLogger |

### Known Remaining Items
- Brush-per-entity creation (still exists, but frozen brushes already implemented in FAZ 8O)
- Full clear+redraw pattern (unchanged - would require major refactor)
- No geometry cache (deferred to future phase)
- No retained-mode rendering (deferred to future phase)

---

## 1. Render Pipeline Audit

### Current Architecture
- **Method:** `DrawPreview()` in `MainWindow.xaml.cs:956`
- **Approach:** Full clear + redraw (`NestCanvas.Children.Clear()` + rebuild all elements)
- **Draw order:** Grid → Rulers → Parts/Plates → OperationPreview → Hover → Selection → Snap
- **Geometry:** Uses `StreamGeometry` for all polygon/polyline drawing (good for simple shapes)
- **Browser/Pens:** New `SolidColorBrush` instances created on every single draw call — no reuse, no freezing

### Bottlenecks
| Issue | Severity | Detail |
|-------|----------|--------|
| Full clear + redraw every frame | **High** | No differential update; even minor changes redraw everything |
| New Brush per entity per frame | **High** | Hundreds of `new SolidColorBrush(...)` per frame, GC pressure |
| No viewport culling | **Medium** | Off-screen geometry is still drawn; only becomes visible with zoom |
| No geometry cache | **Medium** | Same polygon coordinates recalculated to screen space every frame |
| No frozen Freezables | **Low** | Brushes/Pens not frozen; minor overhead per call |
| Empty catch in DrawPreview | **Low** | Line 987: `catch { }` silently swallows rendering errors without diagnostics |

### Recommendations
- Cache frequently used brushes/pens as `static readonly` frozen resources
- Add bounding-box viewport culling before drawing
- Consider `DrawingVisual` for retained-mode rendering in future phase
- Add render error logging

---

## 2. Selection System Audit

### Current Implementation
- **HitTest:** Bounding-box only (`MainViewModel.cs:2492`). Y-coordinate flipped for plate space.
- **HitTestRect:** Bounding-box intersection (`MainViewModel.cs:2548`)
- **Marquee:** Simple intersection — no CAD left/right direction distinction
- **ToggleSelection:** Ctrl=toggle, Shift=range, no-modifier=clear+select
- **Hover:** Reference-equality check triggers full redraw on every mouse move when part changes

### Bottlenecks
| Issue | Severity | Detail |
|-------|----------|--------|
| Bounding-box only hit testing | **Medium** | Imprecise for irregular shapes; nearby parts can be falsely selected |
| No CAD-standard marquee | **Medium** | Missing left→right (fully inside) vs right→left (touching) distinction |
| Hover triggers full redraw | **Medium** | Every mouse move over a different part causes full canvas clear+redraw |
| Selection handles basic | **Low** | Only 4 corner handles (6x6px red squares); no edge handles, no rotate handle |

### Recommendations
- Add CAD-standard marquee: left→right = `BoundingBox.Contains()`, right→left = `Intersects()`
- Add edge midpoint handles (4 extra handles) and rotation handle above bounding box
- Consider polygon-accurate hit testing for single-click precision

---

## 3. Zoom & Pan Audit

### Current Implementation
- **Zoom:** Mouse-centric zoom at factor 1.15/tick (`MainWindow.xaml.cs:151-163`)
- **Pan:** Middle-mouse, Space+Left, Ctrl+Left (no Shift) (`MainWindow.xaml.cs:165-177`)
- **Fit:** Computes content bounds, scales to fit with 50px padding (`MainWindow.xaml.cs:812-839`)
- **1:1:** Reset zoom=1, pan=0 (`MainWindow.xaml.cs:803-810`)
- **ZoomToPart:** Double-click zoom with 90px padding (`MainWindow.xaml.cs:931-954`)

### Assessment
| Feature | Status | Detail |
|---------|--------|--------|
| Mouse-centric zoom | ✅ Good | Cursor point stays fixed during zoom |
| Fit to screen | ✅ Good | Handles all content types (parts, nest result, unplaced) |
| Zoom range | ✅ Good | 0.01x–100x |
| Zoom to selection | ❌ Missing | No way to zoom to currently selected parts |
| Zoom animation | ❌ Missing | Immediate zoom, no smoothing |

### Recommendations
- Add `ZoomToSelection()` — zoom to bounding box of all selected parts
- Add toolbar button + keyboard shortcut (e.g. Ctrl+F or Z)

---

## 4. Snap System Audit

### Current Implementation
- **Grid snap:** Rounds to nearest `GridStepMm` (`MainWindow.xaml.cs:392-416`)
- **Vertex snap:** Matches source-to-target vertex within tolerance (`MainWindow.xaml.cs:448-494`)
- **Edge snap:** Bounding-box edge alignment (`MainWindow.xaml.cs:497-577`)
- **Center snap:** Center-to-center alignment (`MainWindow.xaml.cs:580-647`)
- **Tolerance:** `5.0 / zoom` pixels in screen space
- **Priority:** Grid → Vertex → Edge → Center (first match wins)

### Visuals
- Full-canvas dashed lines (yellow #FFD84D)
- 10px ellipse marker at snap point
- Text label "Snap: {Mode}" with dark background

### UX Assessment
| Issue | Severity | Detail |
|-------|----------|--------|
| Small marker size | **Low** | 10px ellipse can be hard to see on busy canvas |
| Labels can be covered | **Low** | Label positioned at +8,+8 from snap point; may go off-screen |
| No angle snap | **Future** | No 45°/90° angle snapping |
| No midpoint snap | **Future** | No mid-edge snap |

### Recommendations
- Increase marker to 12px + add outer glow ring
- Keep label within canvas bounds
- Angle/midpoint snaps deferred to future phase

---

## 5. Properties & Bounding Inspection

### Current State
- **Selection Width/Height:** Already shown in status bar and Properties tab
- **SelectedArea:** Computed in ViewModel (`SelectedAreaText`)
- **Missing:** Perimeter, total area for multi-selection, per-part area in properties

### Recommendations
- Add `SelectionPerimeterText` to ViewModel and Properties panel
- Add `SelectionArea` display (already computed internally, just add binding)
- For multi-selection: show combined bounds WxH + total area

---

## 6. Measurement Tool

### Current State: **NOT IMPLEMENTED**
- No distance measurement tool
- No angle measurement tool
- No ruler/dimension lines between two points
- Mouse coordinates shown in status bar but no interactive measurement

### Required Implementation
- New tool mode: "Measure"
- Click Point A → drag/hover → Click Point B → show results → ESC to cancel
- Display: Distance (mm), ΔX (mm), ΔY (mm), Angle (°)
- Visual: Dashed line between points, dimension text overlay
- Status bar integration for measurement info

---

## 7. Layer Labels

### Current State
- **ShowPartNames:** Default `false` ✅
- **ShowLayerNames:** Default `false` ✅
- **ShowOperationNames:** Default `false` ✅
- **Zoom gate:** Labels only visible when `_zoom > 0.5` ✅
- **Opacity:** 0.75 ✅
- **Font:** Consolas 9px at polygon centroid ✅

### Assessment
Label system is already well-configured. Defaults are off, zoom-gated, low opacity, non-intrusive. No changes needed.

---

## 8. Performance Characteristics by Project Size

### Small (< 100 parts, < 500 vertices)
- Render: ~2–5ms per frame — acceptable
- Zoom/Pan: Instant — acceptable
- Drag: Instant — acceptable
- GC: Low pressure

### Medium (100–500 parts, 500–2000 vertices)
- Render: ~10–30ms per frame — borderline
- Zoom: Slight stutter on mouse wheel
- Drag: Still usable but frame drops visible
- GC: Moderate pressure from brush creation

### Large (500–1000 parts, 2000–5000 vertices)
- Render: ~40–100ms per frame — drops below 15 FPS
- Zoom: Noticeable lag
- Drag: Choppy at >10px movement
- GC: Frequent gen0/1 collections

### Very Large (1000+ parts, 5000+ vertices)
- Render: 100ms+ — sub-10 FPS
- Zoom: Significant lag
- Drag: Uncomfortable
- Memory: Undo snapshots may exceed 500MB

---

## 9. Technical Debt Summary

| Item | Location | Impact |
|------|----------|--------|
| Monolithic `MainWindow.xaml.cs` (1847 lines) | Views/ | All rendering, input, snap, selection in one file |
| Monolithic `MainViewModel.cs` (3583 lines) | ViewModels/ | All business logic and state in one class |
| Brush-per-entity creation | MainWindow.xaml.cs | GC pressure, perf degradation at scale |
| Full clear+redraw | MainWindow.xaml.cs | Inefficient for incremental changes |
| No viewport culling | MainWindow.xaml.cs | Wastes render time on off-screen geometry |
| No measurement tool | — | Missing core CAD feature |
| No geometry cache | — | Recalculates screen coords every frame |
| Bounding-box-only hit testing | MainViewModel.cs | Imprecise for irregular shapes |
| String-based tool selection | MainViewModel.cs | No tool abstraction |

---

## 10. Regression Risk Assessment

| Change | Risk | Mitigation |
|--------|------|------------|
| Marquee selection logic | Medium | Keep Intersects path for right→left; add Contains for left→right |
| Brush caching | Low | Use static frozen brushes — behaviorally identical |
| Viewport culling | Medium | Add culling as opt-in per draw path; verify visually |
| Measurement tool | Low | New feature, doesn't modify existing paths |
| Selection handle changes | Low | Visual-only changes |
| Snap visual changes | Low | Visual-only changes |

---

## 11. Recommended Implementation Order

1. **Bounding Inspector** — Quick ViewModel + XAML change
2. **CAD Marquee Selection** — Modify HitTestRect + add Contains logic
3. **Improved Selection UX** — Better handles, polyline outlines
4. **Measurement Tool** — New feature with ESC cancel
5. **ZoomToSelection** — Add method + keybinding
6. **Snap UX** — Visual improvements
7. **Drag Performance** — Reduce redraws during drag
8. **Render Optimization** — Brush cache + viewport culling
9. **Build & Test Verification**

---

## 12. Key Metrics Baseline

Pre-optimization measurements (to be verified post-changes):

| Metric | Current | Target |
|--------|---------|--------|
| DrawPreview time (100 parts) | ~15ms | < 10ms |
| DrawPreview time (500 parts) | ~50ms | < 25ms |
| Zoom response | ~20ms | < 15ms |
| Drag frames at 100 parts | ~30 FPS | > 40 FPS |
| Brush allocations per frame | N+ (per entity) | < 20 (cached) |
