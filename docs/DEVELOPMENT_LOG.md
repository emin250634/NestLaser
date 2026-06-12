# DEVELOPMENT_LOG.md — NestLaser Desktop

## 2026-06-12 13:15 — İlk MVP Oluşturma

**Yapılan işlem:** NestLaser Desktop projesinin ilk MVP sürümü oluşturuldu

**Değişen dosyalar:**
- `NestLaserDesktop.sln` — Yeni
- `NestLaserDesktop.csproj` — .NET 8 + WPF + netDxf.Standard
- `Models/` — Point2D, Part, Plate, NestResult
- `Services/DxfService.cs` — DXF import/export
- `Engine/NestingEngine.cs` — Skyline nesting
- `ViewModels/MainViewModel.cs` — MVVM binding
- `MainWindow.xaml` — WPF arayüzü

**Neden yapıldı:**
CorelDRAW VBA makrosunun nesting kalitesinin düşük olması.

**Sonuç:**
Temel özellikler çalışan bir MVP olarak tamamlandı.

**Sonraki adım:**
Proje iskeletini profesyonel hale getirmek.

---

## 2026-06-12 13:50 — Faz 1: Altyapı Sağlamlaştırma

**Yapılan işlem:** Proje yapısı profesyonel düzeye çıkarıldı, modüler mimari oluşturuldu

**Değişen dosyalar:**
- `README.md` — Yeni oluşturuldu
- `Geometry/` — Yeni klasör (Point2D, Polygon, BoundingBox, GeometryUtils)
- `Models/` — Yeniden yapılandırıldı (PartModel, PlateModel, NestResult, NestPlacement, NestSettings)
- `Nesting/` — Engine/ klasöründen taşındı, yeniden adlandırıldı
- `Views/` — MainWindow.xaml taşındı
- `Utilities/` — Yeni klasör (AppConstants, MathHelper)
- `Services/DxfService.cs` — Yeni modellere uyarlandı
- `ViewModels/MainViewModel.cs` — Yeni modellere uyarlandı, NestSettings eklendi
- `NestLaserDesktop.csproj` — ImplicitUsings eklendi
- `App.xaml` — StartupUri güncellendi

**Eski dosyalar silindi:**
- `Models/Point2D.cs` → `Geometry/Point2D.cs`
- `Models/Part.cs` → `Models/PartModel.cs`
- `Models/Plate.cs` → `Models/PlateModel.cs`
- `Engine/` → `Nesting/`
- `MainWindow.xaml` → `Views/MainWindow.xaml`
- `MainWindow.xaml.cs` → `Views/MainWindow.xaml.cs`

**Neden yapıldı:**
Kodun modüler, bakımı kolay ve genişletilebilir olması için.

**Sonuç:**
Proje yapısı profesyonel düzeye çıkarıldı. Gelecekteki DXF Import, Nesting Engine, DXF Export, RDWorks Export modülleri için altyapı hazır.

**Sonraki adım:**
- Commit ve push
- Faz 2: Parça listesi görünümü
