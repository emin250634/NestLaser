# Project Memory - NestLaser Desktop

## FAZ 8O — CAD Workspace Performance & Measurement UX (2026-06-13)
- **Workspace Audit:** `docs/CAD_WORKSPACE_REVIEW.md` oluşturuldu — render, selection, snap, zoom, layer visibility boğazları raporlandı.
- **Bounding Inspector:** Properties panelinde Width/Height yanında Area, Perimeter, Total Area (multi-selection için) eklendi.
- **Measurement Tool:** Yeni Ölç aracı eklendi. Nokta A → Nokta B seçimi ile mesafe, ΔX, ΔY, açı gösterimi.
- **CAD Marquee Selection:** Sol→sağ = tam içeridekiler (BoundingBox.Contains), sağ→sol = temas edenler (Intersects) standardı uygulandı.
- **Selection UX:** Handle boyutu 6→7px, stroke 1→1.2px, hover outline kalınlığı 1.5→2.0px, selection outline 1.8→2.0px.
- **Render Optimization:** Tüm brush/pen'ler static frozen olarak cache'lendi. Grid, ruler, plate, selection rect, snap, hover tümü cached brush kullanıyor.
- **Snap UX:** Marker 10→12px, stroke 2→2.5px, fill opacity 70→100, görsel daha belirgin.
- **ZoomToSelection:** Ctrl+F / seçili parçalara zoom eklendi.
- **Mini Status Inspector:** Status bar'a ActiveToolText (Araç: Seç/Pan/Ölç) eklendi.
- **Build:** 0 warning, 0 error.
- **Tests:** 28/28 passed.

## Core Purpose
Düzensiz şekilli parçaların (irregular shapes) lazer kesim için plaka üzerine en verimli şekilde yerleştirilmesini (nesting) sağlayan masaüstü uygulaması.

## Architecture
- **Language**: C# / .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **Geometry Core**: Özel Point2D, Polygon ve BoundingBox implementasyonları.
- **Nesting Engine**: Multi-algorithm approach (Free Rectangle, Polygon Collision, Irregular Experimental) with automatic fallback, anchor-point candidate generation, and SAT collision detection.
- **DXF Engine**: DxfService (Import/Export).

## Key Components
- `NestingEngine.cs`: Ana yerleşim algoritması.
- `GeometryUtils.cs`: SAT collision ve transformasyon yardımcıları.
- `MainViewModel.cs`: UI ve logic arasındaki köprü.
- `DxfService.cs`: Gelişmiş DXF Import/Export (RDWorks/CorelDRAW uyumlu).
- `ProjectService.cs`: .nelp proje dosyası yönetimi (JSON tabanlı).
- `PdfReportService.cs`: Teklif PDF, üretim raporu PDF ve yerleşim önizleme PDF çıktıları.

## Project System (.nelp)
- **Format**: JSON tabanlı, insan okunabilir proje dosyası.
- **Content**: Plaka ayarları, parçalar (geometri ve transformasyonlar), katmanlar, nesting sonuçları, şirket profili, son PDF ayarları ve uygulama ayarları.
- **Dirty State**: Kaydedilmemiş değişiklikler için `*` işareti ve çıkışta uyarı mekanizması.
- **Recent Projects**: Son kullanılan 5 projeye hızlı erişim (AppData içinde saklanır).

## Operation Manager (FAZ 8E)
- **LaserOperation Model**: Her bir üretim operasyonu Id, Name, LayerId, OperationType, Power, Speed, PassCount, Priority ve Enabled alanlarını içerir.
- **OperationType Enum**: Engrave, Mark, CutInner, CutOuter, Reference tiplerini tanımlar.
- **Operations Panel**: Operasyonlar sekmesinde liste, drag-drop sıralama, ekle/sil, yukarı/aşağı taşıma ve otomatik öneri butonları bulunur.
- **Auto-Suggestion**: Katman tiplerine göre otomatik operasyon önerisi (Cut→CutOuter, Mark→Mark, Engrave→Engrave).
- **İç/Dış Kesim Analizi**: Polygon geometrisinde point-in-polygon ve polygon containment analizi ile iç kesim/dış kesim ayrımı yapılır (SAT tabanlı).
- **Operation Preview**: Seçili operasyonun katman rengiyle önizleme modu.
- **Drag & Drop Sıralama**: Operasyon listesinde sürükle-bırak ile sıralama desteği.
- **Export Entegrasyonu**: DXF export raporuna operasyon sırası eklenir.
- **Project System**: Operasyonlar .nelp proje dosyasında kaydedilir/yüklenir.

## DXF Export Features
- **Production Mode**: Yerleşim sonrası plakaların koordinatlarını ve transformasyonlarını koruyarak export.
- **RDWorks Compatibility**: Basit LWPOLYLINE kullanımı, ACI (AutoCAD Color Index) mapping, AC1015 header.
- **Multi-Plate Support**: Birden fazla plaka yan yana (offsetli) olarak dışa aktarılır.
- **Export Report**: DXF ile aynı klasöre detaylı üretim raporu (`export-report.txt`) üretilir. Rapora operasyon sırası (Operation Order) eklenmiştir.
- **Unplaced Parts**: Sığmayan parçalar plaka dışına düzenli bir şekilde export edilebilir.

## PDF Report Features (FAZ 8J)
- **Quotation PDF**: Müşteriye gönderilebilir profesyonel teklif PDF'i.
- **Production Report PDF**: Üretim bilgileri ve maliyet özeti içeren rapor.
- **Nesting Preview**: Plaka ve yerleşmiş parçalar PDF içinde vektör olarak çizilir.
- **CompanyProfile**: CompanyName, Address, Phone, Email, Website, LogoPath bilgileri PDF başlığında kullanılır.

## Rules & Conventions
- Parçalar arası mesafe (`GapBetweenParts`) her zaman korunmalıdır.
- Plaka marjini (`PlateMargin`) dışına çıkılmamalıdır.
- SAT collision sistemi ana doğruluk kaynağıdır.
- Bounding box her zaman hızlı bir ön kontrol (pre-check) olarak kullanılmalıdır.
- Kullanıcı arayüzü her zaman responsive kalmalı, uzun işlemler asenkron veya timeout korumalı olmalıdır.
# Phase 8N - Project Portability & Data Migration

- `.nelp` now stores `ProjectVersion`, `CreatedWithVersion`, and `LastSavedWithVersion`.
- Project migration, integrity repair, recovery reporting, dated backups, and `.nelpkg` package import/export were added.
- Profile snapshots now cover material, machine, operation settings, and cost settings so projects can open on clean computers.
