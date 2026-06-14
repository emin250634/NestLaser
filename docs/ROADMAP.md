# Roadmap — NestLaser Desktop

## ✅ Completed Phases

| Phase | Description | Status |
|-------|-------------|--------|
| FAZ 8A | Core DXF Import/Export + Part Management | ✅ |
| FAZ 8B | Nesting Engine (Free Rectangle + Polygon Collision) | ✅ |
| FAZ 8C | Layer System + Preview/Rendering | ✅ |
| FAZ 8D | Project System (.nelp) + Undo/Redo + Analysis | ✅ |
| FAZ 8E | Operation Manager & Laser Process Pipeline | ✅ |
| FAZ 8F | Technical Audit + Expected Behavior Validation | ✅ |
| FAZ 8F.1 | Geometry Integrity Cleanup (winding, validation, undo) | ✅ |
| FAZ 8G | Operation Manager & Production Pipeline | ✅ |
| **FAZ 8H** | **Material Database & Machine Profiles** | **✅ Current** |

## Future Phases

### FAZ 9 — No-Fit Polygon (NFP)
- NFP generation for two arbitrary polygons
- Integration with nesting engine for dense irregular packing
- Depends on: CCW winding (✅), CleanupVertices (✅), IsValid (✅)

### FAZ 10 — Common Line Cutting
- Shared edge detection between adjacent placed parts
- Reduced cutting path length and time
- Export visualization of common lines

### FAZ 11 — Offset Path / Kerf Compensation
- Inner/outer offset generation for laser kerf
- Compensation for material thickness and beam width
- Test pattern generation for kerf calibration

### FAZ 12 — Toolpath Generation
- G-code or proprietary format generation
- Operation-based toolpath sequencing (Engrave → Mark → CutInner → CutOuter)
- Lead-in/lead-out, pierce points, micro-joints

### FAZ 13 — Machine Control Integration
- Direct machine communication (Ethernet/Serial)
- Job queue management
- Production monitoring and logging

### Future: Cost Estimation / AI Recommendation / Material Optimization / Machine Analytics
- Based on material/machine profile infrastructure (FAZ 8H)
- Material cost estimation per project
- AI-powered power/speed/pass recommendations
- Machine utilization analytics

## Technical Debt / Low Priority

| Item | Notes |
|------|-------|
| Concurrent nesting (background thread) | UI blocks during nesting |
| Unit parsing from DXF header | Currently assumes mm |
| Tessellation quality control for curves | Configurable segment count |
| Collision cache precision | 0.1mm may cause misses |
