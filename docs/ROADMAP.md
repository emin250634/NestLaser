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
| **FAZ 8H** | **Material Database & Machine Profiles** | ✅ |
| **FAZ 8I** | **Cost Estimation & Quotation System** | ✅ |
| **FAZ 8J** | **PDF Quotation & Production Report System** | ✅ |
| **FAZ 8K** | **Full System Audit & Next Action Report** | ✅ |
| **FAZ 8L** | **Stabilization, Regression Testing & RC Preparation** | ✅ |
| **FAZ 8M** | **Responsiveness & Architecture Cleanup** | ✅ |
| **FAZ 8O** | **CAD Workspace Performance & Measurement UX** | ✅ |
| **FAZ 8O.1** | **Render Pipeline & Technical Debt Cleanup** | ✅ |
| **FAZ 8P** | **Installer, Packaging & Release Readiness** | ✅ |
| **FAZ 9X** | **Geometry / Unit / Nesting Professionalization** | ✅ |
| **FAZ 9A** | **CAD Workspace Professionalization & Shape-Aware Nesting** | ✅ |

## FAZ 8M Responsiveness Summary

- Workflow services created for import, project, cost, export, and nesting/benchmark orchestration.
- Long-running import, project IO, nesting, benchmark, cost, DXF export, and PDF export now use Task-based background execution.
- Progress overlay infrastructure is active; cancellation token plumbing is prepared.
- Architecture review added in `docs/ARCHITECTURE_REVIEW.md`.
- No SVG, PLT, NFP, Common Line Cutting, or new commercial feature was added.

## FAZ 8L Stabilization Summary

- Full audit findings are now actioned for testability, diagnostics, data safety, and project portability.
- Added automated xUnit regression suite and DXF fixture tests.
- Added GitHub Actions restore/build/test workflow.
- Added crash logging and safe JSON backup writes.
- Added RC checklist and testing guide.
- v1.0 before-release must-haves remaining: installer/auto-update, licensing if commercial distribution requires it, crash report collection UI/upload, broader manual CAD compatibility matrix, performance hardening for very large DXF files, and packaging/signing.

## FAZ 8K Audit Summary

- Full audit completed in `docs/FULL_SYSTEM_AUDIT.md`.
- Current maturity: **Beta**.
- General readiness: end-to-end workflow exists, but v1.0 needs stabilization, automated tests, data safety, packaging, and performance hardening.
- Recommended next phases:
  1. **FAZ 8N — Project Migration & Compatibility Matrix**
  2. **FAZ 8O — CAD Workspace Performance & Measurement UX**
  3. **FAZ 8P — v1.0 Packaging & Release Readiness**
  4. **FAZ 8Q — Customer/Quote Management Foundation**
  5. **FAZ 8R — Render Pipeline Split**
- v1.0 before-release must-haves: installer, migration compatibility tests, manual CAD compatibility matrix, crash diagnostics review path, packaging/signing, and large-file render optimization.

## Future Phases

### FAZ 10 — No-Fit Polygon (NFP)
- NFP generation for two arbitrary polygons
- Integration with nesting engine for dense irregular packing
- Depends on: CCW winding (✅), CleanupVertices (✅), IsValid (✅)

### FAZ 11 — Common Line Cutting
- Shared edge detection between adjacent placed parts
- Reduced cutting path length and time
- Export visualization of common lines

### FAZ 12 — Offset Path / Kerf Compensation
- Inner/outer offset generation for laser kerf
- Compensation for material thickness and beam width
- Test pattern generation for kerf calibration

### FAZ 13 — Toolpath Generation
- G-code or proprietary format generation
- Operation-based toolpath sequencing (Engrave → Mark → CutInner → CutOuter)
- Lead-in/lead-out, pierce points, micro-joints

### FAZ 14 — Machine Control Integration
- Direct machine communication (Ethernet/Serial)
- Job queue management
- Production monitoring and logging

### FAZ 15 — Customer Management & Quote History
- Per-customer quote records based on FAZ 8J PDF output
- Per-customer quotes and price history
- Work order generation from approved quotes
- Stock/roll material tracking (length-based material cost)

### FAZ 16 — AI & Analytics
- AI-powered power/speed/pass recommendations
- Machine utilization analytics
- Predictive pricing models from historical data

## Technical Debt / Low Priority

| Item | Notes |
|------|-------|
| Concurrent nesting (background thread) | UI blocks during nesting |
| Unit parsing from DXF header | Currently assumes mm |
| Tessellation quality control for curves | Configurable segment count |
| Collision cache precision | 0.1mm may cause misses |
# Phase 8N Completed - Portability & Migration

- `.nelp` projects now include version metadata for future migrations.
- Project migration, recovery reporting, integrity repair, dated backups, and `.nelpkg` package import/export are implemented.
- v1.0 readiness improved: projects are less dependent on local AppData material/machine databases.

## v1.0 Follow-up

- Add user-facing recovery dialog with detailed section-by-section report.
- Add explicit migration tests when the first post-1.0 schema change lands.
- Extend package contents when formal job/customer management is introduced.
