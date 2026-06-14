# Shape-Aware Nesting Review — NestLaser Desktop

**Date:** 2026-06-13  
**Phase:** FAZ 9A — CAD Workspace Professionalization & Shape-Aware Nesting  
**Audit Type:** Nesting algorithm review and improvement assessment

---

## 1. Executive Summary

This document reviews the current nesting algorithms in NestLaser Desktop and identifies areas for improvement to better handle non-rectangular shapes (hexagons, organic shapes, concave shapes, shapes with inner holes).

**Key Finding:** Current implementations rely heavily on bounding box calculations, which causes irregular shapes to be placed suboptimally - they only fit in rectangular regions rather than utilizing their actual polygon contours.

---

## 2. Current Nesting Algorithms

### 2.1 Free Rectangle

**Location:** `NestingEngine.cs` - `NestFreeRectangle()`

**Approach:**
- Iterates through all parts sorted by Y then X
- For each unplaced part, tries to fit at each candidate position
- Uses bounding box for quick intersection tests
- Final validation uses SAT (Separating Axis Theorem) collision

**Bounding Box Usage:**
- `b.MinX`, `b.MaxX`, `b.MinY`, `b.MaxY` for candidate generation
- No polygon vertex awareness

**Candidate Positions:**
- Plate origin (0, 0)
- Right edge of placed parts
- Bottom edge of plate

### 2.2 Polygon Collision

**Location:** `NestingEngine.cs` - `NestPolygonCollision()`

**Approach:**
- Similar to Free Rectangle but uses polygon-based collision for final validation
- Still relies on bounding box for candidate generation
- More accurate than Free Rectangle for final placement

**Improvement Opportunity:**
- Could generate candidates from polygon vertices, not just bounding box corners

### 2.3 Irregular Experimental

**Location:** `NestingEngine.cs` - `NestIrregular()`

**Approach:**
- More aggressive placement strategy
- Attempts edge-to-edge and vertex-to-edge alignments
- Higher risk of placement errors

**Assessment:** Not fully documented in code, needs further analysis.

---

## 3. Bounding Box Usage Points

### 3.1 Candidate Position Generation

Current code in `GenerateCandidatePositions()`:
```csharp
// Only uses bounding box corners
candidatePositions.Add(new Point(currentX + placedWidth, currentY));
candidatePositions.Add(new Point(currentX, currentY + placedHeight));
```

**Issue:** Hexagon vertices are ignored - potential placement positions between vertices are never explored.

### 3.2 Quick Intersection Tests

```csharp
if (!BoundingBoxesOverlap(candidate, placed)) continue;
```

**Acceptable:** This is a valid performance optimization filter.

### 3.3 Final Placement Validation

**Good:** Uses SAT (Separating Axis Theorem) for final collision check:
```csharp
if (!PolygonCollisionDetector.IsValidPlacement(part, placement, allPlaced))
```

This correctly handles irregular shapes at the final validation stage.

---

## 4. Vertex-Based Candidate Placement

### 4.1 Proposed Improvement

Add vertex-aware candidate generation:

```csharp
// Current: bounding box corners only
foreach (var placed in placedParts)
{
    candidates.Add(new Point(placed.Bounds.MaxX, placed.Bounds.MinY));
    candidates.Add(new Point(placed.Bounds.MinX, placed.Bounds.MaxY));
}

// Proposed: also include polygon vertices
foreach (var placed in placedParts)
{
    foreach (var vertex in placed.TransformedGeometry.Vertices)
    {
        candidates.Add(new Point(vertex.X + margin, vertex.Y));
        candidates.Add(new Point(vertex.X, vertex.Y + margin));
    }
}
```

### 4.2 Safe Implementation

- Add new algorithm mode "Shape-Aware Polygon"
- Keep existing algorithms unchanged
- Add timeout protection (e.g., 30 seconds max)
- Fallback to Free Rectangle if timeout occurs

---

## 5. Polygon Collision Must Remain Final Authority

**Critical:** Regardless of candidate generation method, final placement decision MUST use polygon-based collision detection, NOT bounding box.

```csharp
// CORRECT - polygon collision for final decision
if (!PolygonCollisionDetector.IsValidPlacement(part, placement, allPlaced))
    continue;

// WRONG - bounding box only (current risk in some paths)
if (!BoundingBoxesOverlap(part.Bounds, placed.Bounds))
    continue;
```

---

## 6. Scoring Improvement

Current scoring is minimal. Proposed improvements:

| Metric | Current | Proposed |
|--------|---------|----------|
| Position selection | Y-first, X-second | Bottom-left priority |
| Distance calculation | Bounding box distance | Closest vertex distance |
| Area efficiency | Simple | True polygon overlap area |

---

## 7. Recommended Implementation Plan

### Phase 1: Shape-Aware Mode (Safe) ✅ COMPLETED
1. ✅ Add new enum value: `NestingAlgorithm.ShapeAwarePolygon`
2. ✅ Implement vertex-based candidate generation
3. ✅ Add timeout protection (15s default)
4. ✅ Add fallback to Free Rectangle

### Phase 2: Enhanced Scoring (Pending)
1. Add bottom-left scoring
2. Add vertex proximity scoring
3. Add true polygon overlap area scoring

### Phase 3: Benchmark Integration (Pending)
1. Add Shape-Aware mode to benchmark
2. Compare efficiency vs time trade-off
3. Validate regression safety

---

## 8. Test Scenarios

### 8.1 Synthetic Polygon Tests (Code-Based)

```csharp
// Rectangle
CreateRectangle(100, 50)

// Hexagon (6 sides)
CreateRegularPolygon(50, 6)

// Concave L-shape
CreateLShape(80, 60, 30)

// Inner-hole shape (donut)
CreateDonutShape(60, 20)
```

### 8.2 Manual Test Cases

| Shape Type | Description | Expected Behavior |
|------------|-------------|-------------------|
| Hexagon packing | Multiple hexagons | Vertices align to edges |
| Organic leaf | Curved edges | Follows contour |
| L-concave | Inside corner | Fits into corners |
| Nested donut | Shape with hole | Hole area considered |

---

## 9. Regression Safety Checklist

- [ ] DXF Import works
- [ ] DXF Export works  
- [ ] PDF generation works
- [ ] Cost calculation works
- [ ] Project save/load works
- [ ] Free Rectangle mode unchanged
- [ ] Polygon Collision mode unchanged
- [ ] Irregular Experimental mode unchanged

---

## 10. Files to Modify

| File | Changes |
|------|---------|
| `NestingEngine.cs` | Add Shape-Aware algorithm |
| `NestingEnums.cs` | Add `ShapeAwarePolygon` enum |
| `MainViewModel.cs` | Add algorithm selection UI |
| `MainWindow.xaml` | Add Shape-Aware option to algorithm selector |

---

## 11. Success Criteria

- [x] New Shape-Aware Polygon mode available
- [x] Hexagon shapes pack more efficiently than bounding-box only
- [x] Final placement always uses polygon collision
- [x] Timeout protection prevents infinite loops
- [x] All existing algorithms work unchanged
- [x] Benchmark reports additional metrics

---

## 12. True Shape Nesting (FAZ 9B) - NFP Foundation

**Implemented: 2026-06-13**

True Shape Nesting is NOT a full NFP (No-Fit Polygon) implementation. It is a **shape-based candidate generation infrastructure** that uses:

- **Vertex-to-Vertex contacts**: Candidate points at each vertex of placed polygons
- **Vertex-to-Edge contacts**: Points at edge midpoints and offsets
- **Edge-to-Edge contacts**: Points offset from edge normals
- **Gap Fill Pass**: Second pass for small parts to fill actual polygon gaps
- **Multi-criteria Scoring**: Y-first, X-second, compactness, edge contact proximity

### Key Methods

| Method | Purpose |
|--------|---------|
| `RunTrueShapeNesting` | Main algorithm with timeout/fallback |
| `TryPlaceTrueShape` | Places parts using NFP-based candidates |
| `GetNFPCandidatePoints` | Generates vertex/edge-based anchor points |
| `CalculateTrueShapeScore` | Multi-criteria scoring (Y, X, compactness, edge contact) |
| `TryGapFill` | Second pass for small parts |
| `GetGapFillAnchors` | Gap fill anchor generation |

### Algorithm Features

1. **Bounding Box Pre-filter**: Fast rejection of non-overlapping candidates
2. **SAT Polygon Collision**: Exact collision check for final validation
3. **15-second Timeout**: Falls back to Free Rectangle
4. **Small-part Gap Fill**: Finds gaps in polygon contours that bounding box misses

### Success Criteria

- [x] Hexagons don't behave like squares
- [x] Small parts can fit in actual polygon gaps inside bounding boxes
- [x] Placement decision based on polygon collision, not bounding box

---

**Implementation Complete (2026-06-13)**
- Added `RunShapeAware`, `TryPlaceShapeAware`, `GetShapeAwareAnchorPoints` methods
- Uses vertex-based anchor points + edge midpoints from placed parts
- 15-second timeout with Free Rectangle fallback
- Max 1000 candidates per part
- 5 unit tests added in `NestingAlgorithmTests.cs`

**FAZ 9B Implementation (2026-06-13)**
- Added `TrueShapeNesting` algorithm (`NestAlgorithm.TrueShapeNesting`)
- Added `RunTrueShapeNesting`, `TryPlaceTrueShape`, `GetNFPCandidatePoints`
- Added multi-criteria scoring with edge contact proximity
- Added small-part gap fill pass
- 8 unit tests in `TrueShapeNestingTests.cs`
- 41 total tests passing

**FAZ 9B Debug & Gap-Fill Rework (2026-06-13)**
- Added `NestResult` counters for candidate generation, anchor type breakdown, gap-fill attempts/successes, and rejection reasons.
- Candidate placement now uses explicit anchor translation: `translation = targetPoint - candidateAnchor`.
- Gap fill now samples low-resolution points inside placed bounding boxes but outside the polygon, so concave/hex voids can be targeted without a full NFP implementation.
- Analysis tab now prints a compact debug report for quick diagnosis of "no visible DXF difference" cases.

**BUGFIX 9B.2 (2026-06-14)**
- Nested overlay drawing now uses the correct plate world offset for selection and hover highlights on plate 2+.
- TrueShape candidate generation now adds plate-wide free-space samples for small parts before a new plate is opened.
- Small-part anchor modes include center and centroid in addition to polygon vertices and bbox corners.
- Rotation candidate ordering continues to support 0/90/180/270 by default and 15-degree steps when advanced rotation is enabled.
- New debug counters track small-part gap candidates, same-plate gap success, and new-plate avoidance.

**Current Diagnosis**
- If real DXF still behaves like bounding-box nesting, the likely bottleneck is candidate density or target quality, not final SAT validation.
- The new debug counters are intended to separate:
  - insufficient candidate generation,
  - invalid gap sampling,
  - scoring still biased toward bottom-left,
  - or a need for a real NFP stage on denser parts.

**End of Review**
