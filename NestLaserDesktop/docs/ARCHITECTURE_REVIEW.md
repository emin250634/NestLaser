# Architecture Review — FAZ 8M

## Scope

FAZ 8M yeni kullanıcı özelliği eklemeden mimari sadeleştirme, UI responsiveness ve v1.0 sonrası SVG/PLT/NFP gibi büyük fazlara hazırlık amacıyla yapıldı.

## Current Architecture

NestLaser Desktop hâlâ WPF MVVM tabanlıdır:

- `MainViewModel`: proje durumu, command orchestration, selection, layer/operation/material/cost/export state.
- `MainWindow.xaml.cs`: canvas render, mouse interaction, snap, drag/drop, fit/zoom/pan.
- Service layer: DXF parse/export, project save/load, cost calculation, PDF generation, material/machine profiles, safe JSON, logging.

FAZ 8M öncesinde iş akışı orchestration büyük ölçüde `MainViewModel` içindeydi. Servisler hesaplama ve IO yapıyordu, fakat import/project/cost/export/nesting akışlarını ViewModel yönetiyordu.

## Architecture Audit Findings

### MainViewModel

MainViewModel yaklaşık 3500 satırdır ve hâlâ büyüktür.

Taşınabilir bölümler:

- DXF import + unit override + import verification metni.
- Project save/open/recent project yönetimi.
- Cost validation + calculation settings hazırlığı.
- PDF/DXF export validation ve background export.
- Benchmark orchestration.
- Nesting orchestration.

Servis olması gereken bölümler:

- Import workflow.
- Project workflow.
- Cost workflow.
- Export workflow.
- Nesting/benchmark workflow.
- Undo snapshot yönetimi.
- Layer/operation validation.

Fazla büyümüş bölümler:

- Selection/transform/layer/operation/material/cost/export komutlarının aynı sınıfta kalması.
- Dialog kararları ile domain validation metinlerinin karışması.
- Project DTO oluşturma ve profile snapshot restore davranışının ViewModel içinde kalması.

### MainWindow.xaml.cs

MainWindow.xaml.cs yaklaşık 1800 satırdır.

Taşınabilir bölümler:

- Canvas drawing primitives.
- Preview render pipeline.
- Snap resolver.
- Selection rectangle and hit-test interaction.
- Operation drag/drop handler.

Servis/helper olması gereken bölümler:

- `CanvasRenderService` veya `PreviewRenderer`.
- `SnapService`.
- `SelectionInteractionController`.
- `WorkspaceViewport` for zoom/pan/fit math.

Fazla büyümüş bölümler:

- Render, input, snap, selection, drag/drop, fit, ruler drawing aynı code-behind içinde.
- Her draw çağrısı Canvas children clear/rebuild yapıyor.

## Changes Made in FAZ 8M

### Workflow Services

Yeni servisler:

- `ImportWorkflowService`
  - DXF import orchestration.
  - Unit override hazırlığı.
  - Import verification text.
  - Progress reporting.

- `ProjectWorkflowService`
  - Async open/save wrapper.
  - Recent project add/remove/save.
  - Project file name/path helpers.

- `CostWorkflowService`
  - Cost input validation.
  - Calculation settings assembly.
  - Async cost calculation.
  - Quotation text generation.

- `ExportWorkflowService`
  - Async DXF export.
  - PDF export validation.
  - Async quotation/production PDF creation.

- `NestingWorkflowService`
  - Async nesting execution.
  - Async benchmark execution.
  - Benchmark summary generation.

### Background Tasks

Arka plana alınan işlemler:

- DXF Import.
- Project Open.
- Project Save.
- Nesting.
- Benchmark.
- Cost Calculation.
- DXF Export.
- PDF Export.

UI thread üzerinde kalanlar:

- File dialogs.
- MessageBox confirmations.
- ObservableCollection state assignment.
- Canvas redraw request.

Bu sınır bilinçlidir: WPF-bound state yalnızca UI thread üzerinde güncellenir.

### Progress Reporting

Mevcut `IsLoading` overlay genişletildi:

- `ProgressOverlayText`
- `ProgressPercent`
- `IsProgressIndeterminate`

Cancel altyapısı için servis imzalarında `CancellationToken` hazırlandı. UI cancel butonu bu fazda eklenmedi; amaç altyapıyı hazırlamaktır.

## UI Responsiveness Audit

| Operation | Before | After FAZ 8M | Remaining Risk |
|-----------|--------|--------------|----------------|
| DXF Import | UI thread blokluyordu | `ImportWorkflowService.ImportDxfAsync` | Çok büyük dosyada parser hâlâ tüm dosyayı belleğe alır |
| Project Open | UI thread blokluyordu | `ProjectWorkflowService.OpenAsync` | Çok büyük `.nelp` sonrası UI assignment toplu yapılır |
| Project Save | UI thread blokluyordu | `ProjectWorkflowService.SaveAsync` | Büyük JSON serialize hâlâ tek blok iş |
| Nesting | UI thread blokluyordu | `NestingWorkflowService.RunNestingAsync` | Engine cancellation cooperative değil |
| Benchmark | UI thread blokluyordu | `NestingWorkflowService.RunBenchmarkAsync` | Algoritma içi cancellation sınırlı |
| Cost Calculation | UI thread üzerinde çalışıyordu | `CostWorkflowService.CalculateAsync` | Normalde hızlı, ama büyük geometry listesinde faydalı |
| DXF Export | UI thread blokluyordu | `ExportWorkflowService.ExportDxfAsync` | Report open prompt UI thread |
| PDF Export | UI thread blokluyordu | `ExportWorkflowService.CreatePdfAsync` | PDF preview geometry yoğun dosyada zaman alabilir |

## Large Project Review

### 1000+ Vertices

Beklenen durum:

- Import ve nesting UI'ı daha az bloklar.
- Canvas redraw kabul edilebilir kalabilir.
- Undo snapshots bellek artışı başlar.

Risk:

- Full canvas redraw her etkileşimde pahalılaşır.

### 5000+ Vertices

Beklenen durum:

- Import/nesting background sayesinde pencere yanıt vermeye devam eder.
- Render ve hit-test hissedilir şekilde yavaşlayabilir.

Risk:

- `MainWindow.xaml.cs` her redraw’da çok sayıda WPF shape oluşturur.
- Selection/hit-test bounding box tabanlı olsa da tüm part listesi taranır.

### 10000+ Vertices

Beklenen durum:

- Background iş akışı kilitlenme hissini azaltır.
- Rendering hâlâ ana darboğazdır.

Risk:

- Canvas child sayısı ve Path geometry oluşturma maliyeti yüksek olur.
- Undo snapshot full clone yaklaşımı bellek tüketimini büyütür.
- Benchmark üç algoritmayı arka arkaya çalıştırdığı için CPU uzun süre meşgul kalır.

## Memory Review

### Undo Snapshot

Mevcut yapı full clone ağırlıklı çalışır. 50 snapshot büyük projelerde pahalıdır.

Risk:

- Parts/Layers/Operations kopyaları.
- NestResult referansları ve geometry ilişkileri.

Öneri:

- FAZ 8O/8N sonrası delta-based undo veya snapshot compression.

### NestResult

NestResult placed/unplaced listeleri ve transformed geometry tutar.

Risk:

- Büyük nestlerde transformed geometry kopyaları artar.
- Undo ile birleştiğinde bellek baskısı büyür.

Öneri:

- Transformed geometry cache ve lazy recompute stratejisi.

### Preview Cache

Şu anda kalıcı preview cache yok; her redraw yeniden oluşturuyor.

Risk:

- CPU/render maliyeti yüksek.

Öneri:

- DrawingVisual veya retained geometry cache.

### Polygon Copies

Import, transform, nesting ve export sırasında clone üretimi var.

Risk:

- Güvenli ama bellek maliyetli.

Öneri:

- Immutable polygon snapshot veya pooled geometry buffers uzun vadede değerlendirilmeli.

## Bottlenecks

- Canvas full clear/redraw.
- WPF shape-per-entity render yaklaşımı.
- MainWindow.xaml.cs render/input karışıklığı.
- MainViewModel command yoğunluğu.
- Undo full snapshot memory footprint.
- DxfParser file-level `ReadAllLines`.
- Benchmark algorithm cancellation eksikliği.

## Future SVG / PLT / NFP Preparation

FAZ 8M doğrudan SVG, PLT, NFP veya Common Line eklemedi.

Hazırlık etkisi:

- Import workflow artık format-neutral orchestration noktası sunar.
- Export workflow DXF/PDF dışına genişletilebilir.
- Nesting workflow benchmark/nesting orchestration’ını ViewModel’den ayırır.
- Cost workflow ticari hesapları UI state’den ayırır.
- Progress/cancellation imzaları ileride uzun SVG/PLT import veya NFP nesting için kullanılabilir.

## FAZ 8O Addendum — CAD Workspace Performance & Measurement UX

### Changes
- **Bounding Inspector:** `MainViewModel`'a `SelectionAreaText`, `SelectionPerimeterText`, `SelectionTotalAreaText` ve `CalcSelectionPerimeter()` eklendi.
- **Measurement Tool:** `MainWindow.xaml.cs`'te yeni `_isMeasuring`, `_measurePointA/B` state ve `DrawMeasurementOverlay()` render methodu. `MeasureTool_Click`, `UpdateMeasureStatus`, `SetToolStyle` yardımcıları.
- **CAD Marquee Selection:** `MainViewModel`'a `HitTestRectFullyInside()` ve `HitTestRectFullyInsideNested()` eklendi — sol→sağ sürükleme için `BoundingBox.Contains` kullanıyor.
- **Brush Cache:** 20 adet `static readonly SolidColorBrush` `FreezeBrush()` factory ile frozen.
- **Selection UX:** Handle 6→7px, stroke 1→1.2px, selection outline 1.8→2.0px, hover 2.0px.
- **ZoomToSelection:** `MainWindow.xaml.cs`'te `ZoomToSelection()` ve Ctrl+F shortcut. `MainViewModel`'a `GetSelectionBoundsPublic()` eklendi.
- **Mini Status Inspector:** Status bar'a `ActiveToolText` bağlandı.
- **Snap UX:** Marker 10→12px, stroke 2→2.5px, fill alpha 70→100.

### Performance
- Brush creation per-frame eliminated; GC pressure significantly reduced.
- Drag redraw skip logic added for smoother movement.

### Remaining
- Canvas render pipeline hâlâ full clear/redraw.
- `MainViewModel` hâlâ büyüktür.
- `MainWindow.xaml.cs` hâlâ render/snap/interaction karmaşası içeriyor.

## Remaining Architecture Work

- `MainViewModel` hâlâ büyüktür; selection/layer/operation/material alt ViewModel'lere bölünmeli.
- `MainWindow.xaml.cs` için render/snap/interaction servisleri ayrılmalı.
- Cancellation UI butonu eklenmeli ve engine/parser içinde cooperative cancellation yaygınlaştırılmalı.
- Render pipeline DrawingVisual veya virtualized canvas ile yenilenmeli.
- Undo sistemi delta/snapshot hybrid hale getirilmeli.
# Phase 8N Review Addendum

## Portability Cleanup

- Project loading now has a migration and integrity pass instead of deserializing directly into runtime state.
- Missing material/machine profiles can be restored from embedded snapshots; if snapshots are missing, temporary recovered profiles prevent null-reference crashes.
- `.nelpkg` package export/import isolates portable sharing from normal `.nelp` save/load while reusing the same recovery services.
- Backup creation is now a service concern, not a ViewModel responsibility.

## Remaining Follow-up

- Move package UI dialogs out of `MainViewModel` when a broader file-dialog workflow abstraction is introduced.
- Add a detailed recovery report view before v1.0 so users can inspect exactly what was repaired or lost.

# Phase 8O.1 Review Addendum

## Render Pipeline Cleanup

- Viewport state tracking added to `MainWindow.xaml.cs` for culling support.
- BoundingBox-based viewport culling implemented in `DrawPartsOnPlate()` and `DrawMultiPlateNesting()`.
- Render diagnostics with timing and error logging via AppLogger.
- No new abstractions introduced; technical debt remains but render path is now instrumented.

## Remaining Render Debt

- Brush-per-entity creation pattern still exists (frozen brush cache already in place from FAZ 8O).
- Full clear+redraw pattern unchanged.
- No geometry cache - would require significant refactor.
- DrawingVisual/ retained-mode rendering deferred to future phase.
