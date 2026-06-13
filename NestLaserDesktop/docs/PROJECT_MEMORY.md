# Project Memory — NestLaser Desktop

## Overview
NestLaser Desktop is a CAM application for laser cutting nesting, production operation management, and material/machine profile management. It imports DXF files, nests parts onto plates, manages laser operations (cut, engrave, mark), and exports production-ready DXF with operation orders and material/machine details.

## Current Phase: FAZ 8P — Installer, Packaging & Release Readiness
Release hazırlığı tamamlandı:
- Assembly metadata eklendi: Product, Company, Copyright, Version, FileVersion
- `AppVersion` sınıfı ile merkezi sürüm yönetimi
- İlk çalıştırma kurulumu: AppData klasörleri otomatik oluşturuluyor
- Portable publish profili: `Properties/PublishProfiles/Portable.pubxml`
- Release publish profili: `Properties\PublishProfiles\Release.pubxml`
- `build-release.ps1` script ile tek komutla release üretimi
- About dialog version bilgisi gösteriyor
- `RELEASE_READINESS_REVIEW.md` ve `RELEASE_CHECKLIST.md` dokümanları eklendi

### Previous Phase: FAZ 8O.1 — Render Pipeline & Technical Debt Cleanup
Render pipeline optimize edildi ve teknik borç temizlendi:
- Viewport state tracking: `_viewportMinX/MaxX/MinY/MaxY`, `_renderedPartCount`, `_culledPartCount`, `_lastRenderTicks`, `_enableRenderDiagnostics`
- `UpdateViewportBounds()` ve `IsVisibleInViewport()` helper metodları eklendi
- `DrawPartsOnPlate()`: Viewport culling - ekran dışı parçalar atlanıyor
- `DrawMultiPlateNesting()`: Viewport culling - ekran dışı yerleştirilmiş parçalar atlanıyor
- `DrawPreview()`: Render diagnostics timing with AppLogger error logging
- Boş catch block'lar AppLogger ile değiştirildi

### Previous Phase: FAZ 8O — CAD Workspace Performance & Measurement UX
CAD workspace profesyonele yaklaştırıldı:
- Bounding Inspector (Area, Perimeter, Total Area) Properties panelinde
- Measurement Tool (mesafe, ΔX, ΔY, açı ölçümü) toolbar'da
- CAD-standard Marquee Selection (sol→sağ = içeridekiler, sağ→sol = temas)
- Brush cache render optimization (tüm brush'ler frozen)
- Selection UX, Snap UX, ZoomToSelection, Mini Status Inspector iyileştirmeleri

## Previous Phase: FAZ 8M — Responsiveness & Architecture Cleanup
- Workflow services for import, project, cost, export, and nesting/benchmark orchestration
- Background execution for long-running import, project IO, nesting, benchmark, cost, DXF export, and PDF export
- Progress overlay infrastructure with determinate/indeterminate progress state
- Architecture review in `docs/ARCHITECTURE_REVIEW.md`
- No new file formats or commercial features added

## Core Systems Status

| System | Status | Notes |
|--------|--------|-------|
| DXF Import | ✅ Stable | Unit detection + scaling, validation pipeline, manual override |
| DXF Export | ✅ Stable | Multi-plate, report, operation order, material/machine info, cost section |
| Nesting Engine | ✅ Stable | Free Rectangle + Polygon Collision + Irregular + Benchmark + Quality Guard |
| Operation Manager | ✅ Stable | FAZ 8E/8G/8H — full CRUD, auto-suggest, inner/outer analysis, material settings |
| Material/Machine Profiles | ✅ Stable | FAZ 8H — seed data, CRUD, auto-suggest for power/speed/pass |
| Cost Estimation | ✅ Stable | FAZ 8I — cutting length, time, material/waste/machine/labor cost, quotation |
| PDF Reports | ✅ Stable | FAZ 8J — quotation PDF, production report PDF, vector nesting preview, company header/logo |
| Layer System | ✅ Stable | Cut, Engrave, Mark, Reference — visibility, lock, CRUD, label toggles; clone regression covered |
| Project System | ✅ Stable | .nelp save/load, undo/redo, recent projects, profile snapshot portability, company/PDF/cost data |
| Preview/Rendering | ✅ Stable | Zoom, pan, grid, snap, operation preview, label display toggles |
| Undo/Redo | ✅ Stable | 50-level stack, NestResult preserved |
| Analysis Panel | ✅ Stable | Nesting efficiency, stats, warnings, benchmark results |
| Regression Tests | ✅ New | xUnit categories: Geometry, DXF Import/Export, Project, Cost, PDF, Layer, Operation |
| Diagnostics | ✅ New | AppDomain/Dispatcher/TaskScheduler exception logging |
| Data Safety | ✅ New | Safe JSON save/load with `.tmp` and `.bak` for project/profile/settings data |
| Workflow Services | ✅ New | Import/Project/Cost/Export/Nesting workflow orchestration extracted from MainViewModel |
| Responsiveness | ✅ Improved | Long-running workflows now execute via Task-based background services |

## Known Issues

| Issue | Severity | Status |
|-------|----------|--------|
| DXF parser could hang on CIRCLE/ELLIPSE/POLYLINE fixture parsing | Critical | Fixed in FAZ 8L |
| Concurrent execution blocks UI during nesting | Low | Documented |
| MainViewModel and MainWindow.xaml.cs remain large | Medium | Partially mitigated in FAZ 8M; further split needed |
| Canvas full redraw remains render bottleneck for very large files | Medium | Documented for FAZ 8O |
| Tessellation error grows with segment count (curves) | Low | Documented |
| Zero-size rect selection doesn't filter edge case | Low | Documented |
| Collision cache key precision may cause misses (0.1mm) | Low | Documented |
| Could create excessive plates for oversized parts | Low | Documented |

## Build
- Target: .NET 8.0-windows
- Dependencies: System.Text.Json
- Build command: `dotnet build NestLaserDesktop.sln`
- Test command: `dotnet test NestLaserDesktop.sln`
- Status: FAZ 8L validation must pass restore/build/test with 0 error

## Stabilization (FAZ 8L)
- `SafeJsonFileService` provides temp-file write, replace, and `.bak` backup flow for project/profile/settings JSON.
- `AppLogger` records unhandled, dispatcher, and unobserved task exceptions.
- `.nelp` now stores `ProfileSnapshot` so material/machine profiles survive project transfer to another computer.
- Cost estimation no longer reuses global total cut length for every operation during time estimation.
- `.github/workflows/dotnet.yml` runs restore/build/test on Windows.

## Responsiveness & Architecture (FAZ 8M)
- `ImportWorkflowService`, `ProjectWorkflowService`, `CostWorkflowService`, `ExportWorkflowService`, and `NestingWorkflowService` isolate workflow orchestration from `MainViewModel`.
- Import, save/open, nesting, benchmark, cost, DXF export, and PDF export use Task-based background execution.
- `ProgressOverlayText`, `ProgressPercent`, and `IsProgressIndeterminate` prepare cancel/progress UX for future phases.
- `docs/ARCHITECTURE_REVIEW.md` documents remaining MainViewModel/MainWindow debt, large-project risks, memory risks, and SVG/NFP preparation.

## PDF Reporting (FAZ 8J)
- `PdfReportService` creates professional PDF quotation and production report outputs directly from project, nesting, material, machine, and `JobCostEstimate` data.
- Quotation flow: DXF Aç → Yerleştir → Malzeme Seç → Makine Seç → Maliyeti Hesapla → PDF Teklif Oluştur.
- PDF contents include company profile/logo, nesting preview, efficiency/waste, estimated time, cost breakdown, sales price, VAT, and final price.
- `CompanyProfile` and `PdfReportSettings` are persisted in `.nelp`.
# Phase 8N - Project Portability & Data Migration

- Added project version fields: `ProjectVersion`, `CreatedWithVersion`, and `LastSavedWithVersion`.
- Added `ProjectMigrationService` for current 1.0.0 migration infrastructure.
- Added `ProjectIntegrityService` and `ProjectRecoveryReport` for explicit repair/reporting of missing IDs, invalid layer references, and profile recovery warnings.
- Expanded `ProfileSnapshot` to include operation settings and cost settings, not only material and machine.
- Added dated `Backups` folder support with latest 10 project versions retained before overwrite.
- Added zip-based `.nelpkg` package export/import with project, profile snapshots, optional PDF, optional export report, and manifest.
- Added portability regression tests for migration, recovery, package import/export, and backup retention.
