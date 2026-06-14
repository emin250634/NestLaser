# DXF Regression Test Samples

This directory documents test DXF scenarios for regression testing.
Actual DXF files can be added here; the test plan below describes what each scenario validates.

## Test Scenarios

### 1. CorelDRAW Export DXF
- **Source:** CorelDRAW (common in sign/print industry)
- **Format:** LWPOLYLINE, inches-based, no HEADER $INSUNITS
- **Tests:** Unit fallback to mm, polyline parsing, layer name preservation
- **Expected:** All parts imported with correct scale (may need manual scale if inches)

### 2. AutoCAD Export DXF
- **Source:** AutoCAD 2018+ (common in engineering)
- **Format:** LWPOLYLINE + CIRCLE + ARC, mm-based, $INSUNITS=4
- **Tests:** HEADER unit detection, mixed entity types
- **Expected:** Auto-detected as mm, all geometry imported at correct scale

### 3. RDWorks Compatible DXF
- **Source:** RDWorks / LightBurn (laser software)
- **Format:** LWPOLYLINE, mm-based, color=7 (white) for all entities
- **Tests:** Monochrome layer handling
- **Expected:** All parts import with default "Cut" layer

### 4. Inch-Based DXF
- **Source:** Any CAD set to imperial units
- **Format:** $INSUNITS=1 (Inches)
- **Tests:** Unit detection → x25.4 scale, dimension verification
- **Expected:** Parts imported at 25.4x scale correction

### 5. Unitless DXF
- **Source:** Generic CAD with no unit metadata
- **Format:** No $INSUNITS/$MEASUREMENT/$LUNITS in HEADER
- **Tests:** Fallback to mm, manual scale override
- **Expected:** Warning shown, user can apply manual scale

### 6. Organic Shape DXF
- **Source:** Illustrator/Inkscape (curved shapes)
- **Format:** SPLINE + ELLIPSE + ARC tessellation
- **Tests:** Curve tessellation quality, vertex cleanup
- **Expected:** Shapes approximated with acceptable segment count

### 7. Many Small Parts DXF
- **Source:** Pattern with 100+ small parts
- **Format:** LWPOLYLINE, tightly packed
- **Tests:** Performance, nesting algorithm comparison
- **Expected:** All parts import quickly, benchmark comparison possible

## How to Test

1. Place the actual .dxf file in this directory
2. Open in NestLaser Desktop
3. Verify:
   - Part count matches source
   - Dimensions are correct (verify with measurement tool)
   - Unit detection shows correct source unit in Doğrulama tab
   - If scale is wrong, use manual scale in Doğrulama tab
   - Run benchmark to compare algorithm performance
4. Run `dotnet build` to confirm no regressions

## Adding New Samples

When adding a new test DXF:
1. Copy file to `samples/dxf/`
2. Add a scenario entry above describing the source, format, and expected behavior
3. Run the full test checklist
4. Commit both the DXF file and this README update
