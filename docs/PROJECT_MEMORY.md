# Project Memory — NestLaser Desktop

## Overview
NestLaser Desktop is a CAM application for laser cutting nesting, production operation management, and material/machine profile management. It imports DXF files, nests parts onto plates, manages laser operations (cut, engrave, mark), and exports production-ready DXF with operation orders and material/machine details.

## Current Phase: FAZ 8H — Material Database & Machine Profiles
NestLaser now includes a material database and machine profile system. Users can:
- Select material (MDF, Pleksi, Kontraplak, Metal, etc.) with thickness
- Select machine (Ruida, CO2 Generic, Fiber, etc.)
- Automatically get recommended Power/Speed/Pass settings for operations
- Manage material and machine profiles with CRUD
- Material/machine info is saved in .nelp projects and included in export reports

## Core Systems Status

| System | Status | Notes |
|--------|--------|-------|
| DXF Import | ✅ Stable | Geometry validation pipeline (NormalizeWinding + CleanupVertices + IsValid) |
| DXF Export | ✅ Stable | Multi-plate, report, operation order, material/machine info |
| Nesting Engine | ✅ Stable | Free Rectangle + Polygon Collision + Irregular modes |
| Operation Manager | ✅ Stable | FAZ 8E/8G/8H — full CRUD, auto-suggest, inner/outer analysis, material settings |
| Material/Machine Profiles | ✅ Stable | FAZ 8H — seed data, CRUD, auto-suggest for power/speed/pass |
| Layer System | ✅ Stable | Cut, Engrave, Mark, Reference — visibility, lock, CRUD |
| Project System | ✅ Stable | .nelp save/load, undo/redo, recent projects, material/machine selection |
| Preview/Rendering | ✅ Stable | Zoom, pan, grid, snap, operation preview |
| Undo/Redo | ✅ Stable | 50-level stack, NestResult preserved |
| Analysis Panel | ✅ Stable | Nesting efficiency, stats, warnings |

## Known Issues

| Issue | Severity | Status |
|-------|----------|--------|
| Concurrent execution blocks UI during nesting | Low | Documented |
| Tessellation error grows with segment count (curves) | Low | Documented |
| Zero-size rect selection doesn't filter edge case | Low | Documented |
| Unplaced parts offset not included in NestResult mode bounds | Low | Documented |
| Collision cache key precision may cause misses (0.1mm) | Low | Documented |
| Could create excessive plates for oversized parts | Low | Documented |

## Build
- Target: .NET 8.0-windows
- Dependencies: System.Text.Json
- Build command: `dotnet build NestLaserDesktop.csproj`
- Status: ✅ 0 warning, 0 error
