# Full System Audit & Next Action Report — FAZ 8K

**Date:** 2026-06-13  
**Scope:** NestLaser Desktop full system review after FAZ 8J  
**Mode:** Analysis only. No feature implementation.  
**Build target:** `dotnet build NestLaserDesktop.csproj`

## FAZ 8L Stabilization Update

FAZ 8L implements the first stabilization response to this audit without adding new commercial features. The main changes are automated regression tests, DXF fixture smoke coverage, safe JSON writes, crash logging, project profile snapshots, and a GitHub Actions restore/build/test pipeline.

Audit items reduced in priority:

- Automated regression tests: **High → In progress/covered for core services**
- DXF fixture suite: **High → Initial fixture pack added**
- Cost formula double-count risk: **Critical/High → Fixed and regression-tested**
- AppData JSON corruption risk: **High → Mitigated with temp/replace/backup and logging**
- Missing crash logging: **High → Mitigated with local error log**
- Project portability for missing profiles: **High → Mitigated with profile snapshots**
- DXF parser hang risk on CIRCLE/ELLIPSE/POLYLINE fixtures: **Critical → Fixed and covered by fixture tests**

Remaining v1.0 risks after FAZ 8L are packaging, large-file performance, manual CAD compatibility matrix, background task responsiveness, installer/update flow, and broader production telemetry/crash reporting UX.

## FAZ 8M Responsiveness Update

FAZ 8M addresses the highest-priority architecture and UI responsiveness risks from this audit without adding new product features.

Audit items reduced in priority:

- Large DXF freezes UI: **High → Partially mitigated** with async import/project/nesting/benchmark/export workflows.
- MainViewModel is too large: **High → Partially mitigated** with workflow services; further feature-area ViewModel split remains.
- Benchmark blocks UI: **Medium/High → Mitigated** by `NestingWorkflowService.RunBenchmarkAsync`.
- PDF export blocks UI: **Medium → Mitigated** by `ExportWorkflowService.CreatePdfAsync`.
- Project IO blocks UI: **Medium → Mitigated** by `ProjectWorkflowService`.

Remaining risks after FAZ 8M:

- Canvas rendering still uses full clear/redraw and WPF shape creation.
- MainWindow.xaml.cs still owns render, snap, selection, and viewport math.
- Cancellation tokens exist in service signatures, but algorithm/parser internals are not fully cooperative yet.
- Undo remains memory-heavy for large projects.

## Executive Summary

NestLaser Desktop is beyond prototype in feature breadth: DXF import/export, nesting, project persistence, material/machine profiles, operations, cost estimation, and PDF quotation are all present and integrated into a usable workflow. The main product risk is no longer missing core features; it is reliability, maintainability, verification depth, and production packaging.

The system is best classified as **Beta**. It is commercially promising for controlled use, but not yet v1.0-ready because it lacks automated regression tests, installer/update flow, crash reporting, structured customer/quote management, and deeper validation for CAD edge cases.

No critical build/runtime issue requiring code modification was found during this audit. Code changes should be reserved for the recommended stabilization phases below.

---

## 1. Full System Review

### DXF Import
| Item | Assessment |
|------|------------|
| Current state | Supports LWPOLYLINE, POLYLINE, CIRCLE, ARC, LINE, SPLINE, ELLIPSE, unit detection, scaling, winding normalization, duplicate/collinear cleanup, invalid geometry skipping. |
| Strengths | Practical DXF coverage for laser/CAD workflows; unit pipeline is now explicit; warnings surface import quality issues. |
| Gaps | Parser is custom and line-array based; no formal CAD compatibility matrix; no automated fixture tests; text silently skips some malformed entities. |
| Bug risks | Large DXF files load fully into memory; arc/spline tessellation can distort geometry; open LINE/ARC geometry becomes invalid if treated as closed polygon. |
| Technical debt | Parser, validator, and import reporting are coupled in `DxfService`; unsupported entity data is not preserved. |
| UX issues | Manual scale requires reopening the DXF; skipped geometry count is visible only as text warning. |
| Priority | **High** for test suite and compatibility fixtures; **Medium** for parser refactor. |

### DXF Export
| Item | Assessment |
|------|------------|
| Current state | Exports production DXF with multi-plate offset, layer names/colors, plate borders, unplaced parts, and `export-report.txt`. |
| Strengths | RDWorks/CorelDRAW-oriented simple LWPOLYLINE output; report includes material, machine, operation order, and costs. |
| Gaps | No SVG/PLT/HPGL export; DXF output is limited to simple polylines; no post-export geometry verification. |
| Bug risks | Color mapping falls back for unknown colors; selected-only behavior depends on current selection object identity. |
| Technical debt | Export report writer is text-only and mixed into `DxfService`; export layout logic is service-private and duplicated conceptually in PDF preview. |
| UX issues | Export options are in side panel, not in a clear export dialog; unplaced export choice appears only during export. |
| Priority | **Medium** for v1.0 verification; **Future** for additional formats. |

### PDF Quotation / Production Report
| Item | Assessment |
|------|------------|
| Current state | Native PDF writer creates quotation, production report, and preview PDFs with company header/logo and vector nesting preview. |
| Strengths | No external dependency; customer-visible output now exists; `.nelp` persists company and PDF settings. |
| Gaps | PDF layout is fixed one-page style; no terms, customer record, quote number, line items, pagination, or localization control. |
| Bug risks | Native PDF writer is custom and lightly tested; non-ASCII text is transliterated; very dense nesting may be visually crowded. |
| Technical debt | PDF drawing code is low-level; no visual regression tests. |
| UX issues | Company profile is embedded in Cost/Teklif tab instead of a dedicated settings dialog; logo path is manual text input. |
| Priority | **Medium** before v1.0; **High** if PDF is the main commercial deliverable. |

### Project System (.nelp)
| Item | Assessment |
|------|------------|
| Current state | JSON `.nelp` saves parts, layers, operations, nest result, plate/settings, selected material/machine, cost estimate, company profile, PDF settings. |
| Strengths | Human-readable; old files generally survive missing fields via defaults; reference remapping for `NestResult.Part` is handled. |
| Gaps | No schema version migration layer; AppVersion exists but no migration dispatch; profiles are referenced by ID but not embedded. |
| Bug risks | Loading a project on a different PC may lose selected material/machine if profile IDs are absent; corrupted JSON silently returns null. |
| Technical debt | Serialization uses live model shapes directly; no DTO boundary. |
| UX issues | Failed load only says project could not be read; no repair/import path. |
| Priority | **High** for v1.0 migration and profile portability. |

### Layer System
| Item | Assessment |
|------|------------|
| Current state | CRUD, visibility, lock, layer type, color, power/speed/pass, part assignment, layer-operation cleanup. |
| Strengths | Visibility/lock is respected by selection, transform, nesting, and export in main flows. |
| Gaps | Layer type and operation model overlap; unclear authority between layer speed/power and operation settings. |
| Bug risks | Hidden/locked/reference semantics may diverge between preview, cost, export, and PDF if not tested together. |
| Technical debt | Layer normalization logic lives in ViewModel; needs service-level tests. |
| UX issues | Users may not understand whether layer settings or operation settings drive cost/export. |
| Priority | **Medium**. |

### Operation Manager
| Item | Assessment |
|------|------------|
| Current state | Full CRUD, drag/drop ordering, auto-suggest, inner/outer analysis, operation preview, project persistence, report export. |
| Strengths | Strong production concept; operation order reaches DXF report; material/machine suggestions reduce setup effort. |
| Gaps | No per-operation geometry membership beyond layer; inner/outer detection is candidate marking, not true contour hierarchy. |
| Bug risks | Cost time calculation can double count cut length when multiple cut operations exist on the same layer; disabled operations are skipped but layer settings may still imply work. |
| Technical debt | Drag/drop logic is code-behind; operation validation is mostly UI/status based. |
| UX issues | Applying material settings requires a selected operation; auto-application on material/machine selection can surprise users. |
| Priority | **High** for cost correctness and operation validation. |

### Material Database
| Item | Assessment |
|------|------------|
| Current state | AppData JSON seed materials, CRUD, unit price, unit type, thickness, density, notes. |
| Strengths | Practical default profiles; editable without database dependency. |
| Gaps | No stock quantities, suppliers, sheet sizes per material, price history, or project-level embedded profile snapshot. |
| Bug risks | Corrupted JSON silently falls back to seed data in memory; user edits could appear lost until file is fixed. |
| Technical debt | No atomic file writes or backups for profile JSON. |
| UX issues | Material edit UI is narrow and mixed with selection controls. |
| Priority | **High** for data safety before v1.0; **Future** for stock. |

### Machine Profiles
| Item | Assessment |
|------|------------|
| Current state | AppData JSON seed machines with manufacturer/model/laser type/working area. |
| Strengths | Machine-specific settings are linked into operation suggestions and reports. |
| Gaps | No machine power limits, acceleration, pierce delay, bed origin, controller type, kerf, or postprocessor settings. |
| Bug risks | Working area is informational; plate validation does not clearly enforce machine bed constraints. |
| Technical debt | Machine model is too shallow for toolpath/postprocessor phases. |
| UX issues | Users may assume selected machine constrains plate size, but that is not currently guaranteed. |
| Priority | **Medium** for v1.0, **High** before machine-control/toolpath phases. |

### Cost Estimation
| Item | Assessment |
|------|------------|
| Current state | Calculates material, waste, machine, labor, electricity, consumable, profit, VAT, rounded price; persists settings in AppData. |
| Strengths | Good MVP coverage; supports per sheet, per square meter, per kg; works with nesting efficiency. |
| Gaps | No quote/customer history; no explicit part count field in estimate; no setup time, pierce count, minimum charge, scrap resale, discount, payment terms. |
| Bug risks | Plain text quotation uses `ProjectId` as "Parça Sayısı"; cut time can double count because total cut length is applied per cut operation; mark pass count is not applied in length summary. |
| Technical debt | Cost model mixes project summary and pricing; no unit tests for formula edge cases. |
| UX issues | Zero unit price is allowed with warning, which can create customer-facing zero material cost if missed. |
| Priority | **High** before v1.0. |

### Preview / Zoom / Pan / Fit
| Item | Assessment |
|------|------------|
| Current state | WPF Canvas preview with zoom, pan, grid, rulers, fit, labels, multi-plate/unplaced handling. |
| Strengths | Rich CAD-like workspace; improved Fit includes placed/unplaced/plates. |
| Gaps | No virtualization; render is full clear/redraw; no render profiling. |
| Bug risks | Large DXF or many plates can cause UI lag; `DrawPreview` catches and suppresses all exceptions. |
| Technical debt | Rendering logic lives in `MainWindow.xaml.cs` and duplicates geometry interpretation. |
| UX issues | If drawing fails silently, users may see stale/blank canvas without diagnostic information. |
| Priority | **Medium**. |

### Selection / Multi Selection
| Item | Assessment |
|------|------------|
| Current state | Hit test, Ctrl toggle, Shift range, rectangle selection, list sync, selection highlights. |
| Strengths | Covers normal CAD selection behavior. |
| Gaps | No selection filters by layer/type beyond visibility/lock; no isolate/solo layer. |
| Bug risks | Bounding-box hit testing can select unexpected irregular shapes; zero-size rectangle edge case remains documented. |
| Technical debt | Selection state is maintained in ViewModel and code-behind interactions. |
| UX issues | Dense nests may be hard to select precisely without zoom/filters. |
| Priority | **Low** for v1.0, **Medium** for pro UX. |

### Transform / Properties
| Item | Assessment |
|------|------------|
| Current state | Move, scale selected/all, numeric properties, rotate 90, mirror X/Y, aspect ratio lock. |
| Strengths | Practical edit controls; winding is preserved after mirror. |
| Gaps | No arbitrary rotate input beyond existing rotation fields/engine; no transform constraints by machine bed. |
| Bug risks | Transforming geometry invalidates nesting; clearing/recomputing behavior must stay consistent. |
| Technical debt | Transform commands live in large ViewModel. |
| UX issues | Users need clear indication that nesting should be recalculated after geometry edits. |
| Priority | **Medium**. |

### Snap System
| Item | Assessment |
|------|------------|
| Current state | Grid, vertex, edge, center snap with visual indicator; grid display adapts to zoom. |
| Strengths | Useful CAD workspace foundation. |
| Gaps | No angle, midpoint, intersection, tangent, or snap priority UI. |
| Bug risks | Snap display grid and snap step can differ, intentionally but potentially confusing. |
| Technical debt | Snap code sits in code-behind. |
| UX issues | Users may expect visible grid spacing to match snap increments. |
| Priority | **Low**. |

### Undo / Redo
| Item | Assessment |
|------|------------|
| Current state | 50-level snapshot stack for parts, layers, operations, selection, and nest result. |
| Strengths | Simple and broad; NestResult is preserved and remapped. |
| Gaps | Does not cover all profile/settings changes consistently; snapshots are full clones, not command deltas. |
| Bug risks | Large projects can consume memory quickly; NestResult snapshot is not a deep clone and can be affected by later mutations. |
| Technical debt | Needs dedicated undo service and explicit snapshot boundaries. |
| UX issues | Some actions may dirty project without an intuitive undo path. |
| Priority | **High** for large-project reliability. |

### Nesting Algorithms
| Item | Assessment |
|------|------------|
| Current state | Free Rectangle, Polygon Collision, Irregular Experimental with fallback, anchor points, cache, quality guard. |
| Strengths | Multi-algorithm approach is pragmatic; fallback prevents total failure; final validation demotes invalid placements. |
| Gaps | No true No-Fit Polygon; no common-line optimization; no background thread; no automatic best algorithm selection into production run. |
| Bug risks | Unlimited new plates for oversized/awkward parts; fallback can hide irregular failure; advanced rotations increase compute cost. |
| Technical debt | Engine handles several strategies in one class; test fixtures are missing. |
| UX issues | Users may not know which algorithm to choose; benchmark is manual and blocking. |
| Priority | **High** for background execution/test coverage; **Future** for NFP/common-line. |

### DXF Unit Detection / Manual Scale
| Item | Assessment |
|------|------------|
| Current state | Detects `$INSUNITS`, `$MEASUREMENT`, `$LUNITS`; supports manual override and reference dimension suggestion. |
| Strengths | Correctly addresses a major CAD import failure mode. |
| Gaps | Reference scale is not applied to current geometry; it only prepares settings for reopening. |
| Bug risks | User may believe scale was applied immediately and continue with wrong dimensions. |
| Technical debt | Import verification state is in MainViewModel, not an import workflow object. |
| UX issues | Reopen requirement interrupts workflow. |
| Priority | **High**. |

### Import Verification
| Item | Assessment |
|------|------------|
| Current state | Shows source unit, detection source, scale, bounding box, part count, reference dimension suggestion. |
| Strengths | Gives users a way to catch wrong units early. |
| Gaps | No visual dimension ruler or direct measurement tool on canvas. |
| Bug risks | Bounding box may not equal intended reference dimension for nested/offset source drawings. |
| Technical debt | Verification text is assembled in ViewModel. |
| UX issues | Needs clearer "apply and reimport" action. |
| Priority | **Medium**. |

### Benchmark
| Item | Assessment |
|------|------------|
| Current state | Runs three algorithms sequentially and reports time, efficiency, placed/unplaced, attempts, collisions, recommendation. |
| Strengths | Excellent diagnostic tool for nesting quality. |
| Gaps | Does not auto-apply best result; runs on UI thread; no saved benchmark history. |
| Bug risks | Complex files can freeze UI for multiple algorithm timeouts. |
| Technical debt | Benchmark orchestration belongs in a service. |
| UX issues | Users may expect recommendation to change algorithm automatically. |
| Priority | **Medium**. |

### Analysis Panel
| Item | Assessment |
|------|------------|
| Current state | Shows nesting metrics, warnings, benchmark results. |
| Strengths | Provides useful diagnostic information. |
| Gaps | No severity grouping, exportable audit report, or drill-down to offending parts. |
| Bug risks | Warnings can accumulate without actionable navigation. |
| Technical debt | Analysis formatting lives in ViewModel. |
| UX issues | Users need direct links from warning to part/plate. |
| Priority | **Low/Medium**. |

### CAD Workspace
| Item | Assessment |
|------|------------|
| Current state | Canvas-based drawing workspace with grid, rulers, labels, snapping, selection, transforms. |
| Strengths | Strong foundation for a desktop CAM-style app. |
| Gaps | No measurement tool, no layer isolation, no mini-map, no status overlay for hidden/locked/export filters. |
| Bug risks | Rendering exceptions are swallowed; large projects can degrade responsiveness. |
| Technical debt | UI code-behind is large and owns too much interaction/rendering state. |
| UX issues | Dense workflows compete for sidebar space. |
| Priority | **Medium**. |

---

## 2. User Workflow Audit

### A) Basic Production Flow
**DXF Aç → Ölçüyü doğrula → Plaka ölçüsünü gir → Malzeme seç → Makine seç → Operasyon ayarla → Yerleştir → Maliyet hesapla → PDF teklif oluştur → DXF export al**

| Step | Status | Findings |
|------|--------|----------|
| DXF open | Works | Unit detection and import warnings exist. |
| Verify dimensions | Partly works | Reference scale suggestion requires re-open; no direct canvas measurement. |
| Plate size | Works | Basic validation exists for width/height/margin. |
| Material/machine | Works | Profiles and seed data exist. |
| Operation setup | Works | Auto-suggest and manual edit exist. |
| Nesting | Works | Quality guard and warnings exist. |
| Cost | Works with caveats | Formula needs test coverage; cut time double-count risk. |
| PDF quotation | Works | Customer-ready base PDF exists. |
| DXF export | Works | Production DXF + report generated. |

**Workflow risk:** **High** until cost formulas, unit correction UX, and regression fixtures are validated.

### B) Project Flow
**DXF Aç → Katman ayarla → Operasyon ayarla → Maliyet hesapla → Projeyi Kaydet → Programı kapat → Projeyi Aç → Aynı çalışma alanı geri geliyor mu?**

Current behavior should restore parts, layers, operations, nest result, cost estimate, material/machine selection by ID, company profile, and PDF settings. The largest gap is profile portability: selected material/machine IDs assume the same AppData profile database is present.

**Workflow risk:** **Medium/High** for cross-machine project sharing; **Medium** for corrupted JSON recovery.

### C) Technical Dimension Flow
**DXF Aç → Unit detect → Manual scale → Reference ölçüyle düzelt → Fit → Export sonrası ölçü korunuyor mu?**

The internal model stores scaled-to-mm geometry and export writes current geometry, so export should preserve corrected dimensions once the file is imported with the correct scale. The weak link is the correction UX: reference scaling sets a manual factor and asks user to reopen the DXF, instead of applying/reimporting directly.

**Workflow risk:** **High** due to user confusion around scale application.

### D) Layer / Operation Flow
**Layer gizle/kilit → Seçim/taşıma/nesting/export davranışı doğru mu? → Operasyon sırası rapora doğru gidiyor mu?**

Hidden/locked/reference layers are respected in many paths. Operation order is sorted by priority and included in export report. Main risk is semantic ambiguity: layer power/speed and operation power/speed both exist; cost and reports depend mainly on operations.

**Workflow risk:** **Medium**.

### E) Cost / Quotation Flow
**Material unit price → Machine/operation speed → Cost calculation → PDF quotation → export-report**

The flow is present end to end. However, cost has the highest commercial sensitivity. Current gaps include missing minimum charge/setup time, possible cut-time double count, zero price warnings that can be ignored, and no customer/quote record.

**Workflow risk:** **High** until formula tests and quote controls exist.

---

## 3. Code Quality Review

| Topic | Assessment | Priority |
|-------|------------|----------|
| MVVM separation | ViewModel owns too much business workflow and UI dialog logic. Code-behind owns render/interaction state. Functional, but not clean MVVM. | **High** |
| MainViewModel size | ~3,000 lines. It mixes project IO, import, nesting, layer ops, material ops, cost, PDF, undo, selection. This is the largest maintainability risk. | **High** |
| MainWindow.xaml.cs size | ~1,584 lines. Rendering, mouse tools, snap, drag/drop, fit, and converters are together. | **High** |
| Service layer | Good start (`DxfService`, `CostEstimationService`, `PdfReportService`, `MaterialProfileService`, `ProjectService`), but orchestration remains in ViewModel. | **Medium** |
| Model serialization | Simple and readable, but no explicit schema migration or DTO boundary. | **High** |
| Undo snapshots | Broad but memory-heavy; `NestResult` is not deeply cloned in snapshot. | **High** |
| Rendering performance | Full clear/redraw on every interaction; acceptable for small/medium jobs, risky for large DXFs. | **Medium** |
| Large DXF performance | Parser reads entire file; nesting/benchmark run synchronously; UI blocking likely. | **High** |
| Error handling | Many user-facing catches exist, but several empty catches hide diagnostics. | **Medium** |
| AppData management | Works, but profile writes are not atomic and corrupted JSON may silently fall back to seeds. | **High** |

---

## 4. Production Readiness Score

| Area | Score |
|------|------:|
| DXF Import Accuracy | 78 |
| DXF Export Compatibility | 82 |
| Geometry Engine | 76 |
| Nesting Quality | 72 |
| UI/UX | 68 |
| Layer/Operation Workflow | 74 |
| Cost/Quotation Workflow | 70 |
| Project Save/Load | 76 |
| Stability | 69 |
| Performance | 62 |
| Commercial Readiness | 66 |

**Overall result:** **Beta**

Reasoning: The product has an integrated end-to-end workflow and meaningful professional outputs, but it needs automated testing, data-safety hardening, performance safeguards, and commercial workflow features before v1.0.

---

## 5. Missing Features Report

### A) Must Have Before v1.0
- Automated test suite: geometry, DXF import/export fixtures, project save/load, cost formulas, PDF smoke tests.
- Build/CI check with sample DXF regression set.
- Project schema versioning and migration layer.
- Atomic AppData writes and backup/recovery for profiles/settings.
- Crash reporting/log file with actionable diagnostics.
- Background execution/cancel for nesting and benchmark.
- Cost formula validation: setup time, minimum charge, cut-time double count fix, quote text correctness.
- Installer and versioned release packaging.
- Clear unit correction workflow: apply/reimport from reference scale.
- Profile portability: embed profile snapshot or warn when material/machine IDs are missing.

### B) Should Have After v1.0
- SVG import/export.
- PLT/HPGL import/export.
- Customer/quotation management with quote numbers and history.
- Job order/work order system from approved quotes.
- Material stock with sheet sizes and inventory decrement.
- Dedicated settings screens for company, profiles, cost defaults, PDF templates.
- Export dialogs with presets.
- Measurement tool and canvas dimension annotations.
- Better warning panel with click-to-part navigation.

### C) Professional / Pro Version
- Common Line Cutting.
- Kerf compensation / offset path.
- Toolpath generation with lead-in/lead-out, pierce points, micro-joints.
- Machine-specific postprocessors.
- Batch nesting and quote generation.
- Advanced PDF templates, terms, branded customer documents.
- Role-based settings/profile locks.
- License system and activation.

### D) Long Term R&D
- True No-Fit Polygon nesting.
- Automatic best algorithm selection from benchmark/history.
- AI-assisted power/speed/pass recommendation.
- Predictive pricing from historical jobs.
- Machine utilization analytics.
- Direct machine control integration.
- Cloud sync for profiles, quotes, and jobs.

---

## 6. Risk Register

| Risk | Impact | Probability | Priority | Suggested Fix | Target Phase |
|------|--------|-------------|----------|---------------|--------------|
| Cost calculation over/under-estimates due to operation length aggregation | Customer price can be wrong | Medium | **High** | Add formula unit tests and refactor per-operation/per-layer length calculation | FAZ 8L |
| Manual/reference scale requires DXF reopen | Wrong dimensions may continue downstream | High | **High** | Add "Apply scale and reload" command or direct geometry rescale with confirmation | FAZ 8L |
| Large DXF freezes UI | Poor UX, perceived crash | High | **High** | Move import/nesting/benchmark to background task with progress/cancel | FAZ 8M |
| MainViewModel is too large | Slow development, regression risk | High | **High** | Extract workflow services: ImportWorkflow, ProjectWorkflow, CostWorkflow, ExportWorkflow | FAZ 8M |
| AppData profile corruption silently falls back | User may think custom profiles disappeared | Medium | **High** | Atomic writes, `.bak` recovery, visible error | FAZ 8L |
| No automated regression tests | Regressions likely as features grow | High | **High** | Add xUnit test project and sample DXF fixtures | FAZ 8L |
| Project references external profile IDs only | Projects not portable between machines | Medium | **High** | Store selected profile snapshots inside `.nelp` | FAZ 8N |
| Custom PDF writer lacks visual tests | Customer PDF can regress silently | Medium | **Medium** | Add PDF smoke tests and golden metadata checks | FAZ 8L |
| Full canvas redraw on every interaction | Performance drops on large jobs | Medium | **Medium** | Introduce retained geometry cache or DrawingVisual layer | FAZ 8O |
| DXF parser custom coverage gaps | Some customer files import partially | Medium | **Medium** | Build compatibility matrix and add fixture-driven parser tests | FAZ 8L |
| Undo snapshot memory growth | Large projects consume memory | Medium | **Medium** | Delta-based undo or compressed/deep immutable snapshots | FAZ 8O |
| Export report text lacks structured format | Hard to integrate with external systems | Low | **Low** | Add JSON/CSV report export | Post v1.0 |
| No installer/update | Hard for nontechnical users | High | **High** | Add Windows installer and versioned release process | FAZ 8P |
| No crash reporting/logging | Support is difficult | High | **High** | Add file logging and unhandled exception capture | FAZ 8L |
| Missing customer/quote history | Commercial workflow incomplete | Medium | **Medium** | Add customer and quote database module | Post v1.0 |

---

## 7. Next Phase Recommendation

| Phase | Purpose | Why Important | Estimated Impact | Priority |
|-------|---------|---------------|------------------|----------|
| FAZ 8L — Stabilization & Automated Test Suite | Add tests, logging, crash capture, AppData safety, formula verification | Converts feature-rich beta into a reliable v1.0 candidate | Very high | **Critical** |
| FAZ 8M — Responsiveness & Architecture Cleanup | Background tasks, progress/cancel, split MainViewModel/code-behind responsibilities | Prevents UI freezes and reduces regression risk | Very high | **High** |
| FAZ 8N — Project Portability & Data Migration | `.nelp` schema migration, embedded profile snapshots, load repair/warnings | Makes projects safe to share and reopen long term | High | **High** |
| FAZ 8O — CAD Workspace Performance & Measurement UX | Measurement tool, render optimization, warning navigation, selection filters | Improves daily usability and trust in dimensions | Medium/high | **Medium** |
| FAZ 8P — v1.0 Packaging & Release Readiness | Installer, auto-update plan, release notes, versioning, sample pack | Enables real customer deployment | High | **High** |

Recommended order: **8L → 8M → 8N → 8P → 8O** if release readiness is urgent. Keep NFP/common-line cutting after v1.0 stabilization.

---

## 8. v1.0 Readiness Checklist

- [ ] Automated tests for geometry and cost formulas
- [ ] DXF fixture regression suite
- [ ] Project migration/versioning
- [ ] Profile data backup/recovery
- [ ] Background nesting/import/benchmark
- [ ] Crash logging
- [ ] Installer
- [ ] Cost formula audit and minimum charge/setup time
- [ ] Unit correction workflow improvement
- [ ] Project portability for material/machine profiles
- [ ] PDF smoke/visual verification

---

## 9. Build Verification

Run at end of audit:

```powershell
dotnet build NestLaserDesktop.csproj
```

Expected result:

```text
0 warning
0 error
```

---

## Final Recommendation

Do not start NFP, common-line cutting, or new file formats next. The highest-value next step is **FAZ 8L: Stabilization & Automated Test Suite**. The application already has enough user-visible functionality; the next commercial step is proving that existing behavior remains correct across real DXF files, saved projects, cost formulas, and PDF/DXF outputs.
# Phase 8N Audit Update - Portability Risk Mitigation

- Project portability risk reduced from High to Medium by embedding material, machine, operation, cost, company, and PDF settings snapshots into `.nelp`.
- Data migration readiness improved with `ProjectVersion`, `CreatedWithVersion`, `LastSavedWithVersion`, and `ProjectMigrationService`.
- Silent project corruption risk reduced by `ProjectLoadResult`, `ProjectRecoveryReport`, `.bak` fallback, and explicit lost/recovered section reporting.
- Single-computer AppData dependency reduced by missing profile recovery and temporary recovered profiles.
- Remaining risk: recovery details are currently surfaced in status text; a dedicated recovery dialog is recommended before v1.0.
