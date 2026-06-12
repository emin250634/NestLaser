# DEVELOPMENT_LOG.md — NestLaser Desktop

## 2026-06-12 13:15 — İlk MVP Oluşturma

**Yapılan işlem:** NestLaser Desktop projesinin ilk MVP sürümü oluşturuldu

**Değişen dosyalar:**
- `NestLaserDesktop.sln` — Yeni oluşturuldu
- `NestLaserDesktop.csproj` — .NET 8 + WPF + netDxf.Standard
- `Models/Point2D.cs` — Koordinat yapısı
- `Models/Part.cs` — Parça modeli (bounding box, alan)
- `Models/Plate.cs` — Plaka ayarları
- `Models/NestResult.cs` — Yerleşme sonucu, verimlilik
- `Services/DxfService.cs` — DXF import/export
- `Engine/NestingEngine.cs` — Skyline bottom-left nesting
- `ViewModels/MainViewModel.cs` — MVVM binding
- `MainWindow.xaml` — WPF arayüzü
- `MainWindow.xaml.cs` — Canvas önizleme mantığı
- `App.xaml` / `App.xaml.cs` — Uygulama giriş noktası

**Neden yapıldı:**
CorelDRAW VBA makrosunun nesting kalitesinin düşük olması ve gerçek kontur yerleşimi için yetersiz kalması nedeniyle C# .NET 8 tabanlı yeni bir masaüstü uygulamasına geçiş kararı alındı.

**Sonuç:**
Temel özellikler çalışan bir MVP olarak tamamlandı: DXF yükleme, parçaları algılama, plaka ayarları, skyline nesting, önizleme ve DXF export.

**Sonraki adım:**
- Test dosyası ile doğrulama
- Parça listesi görünümü eklenmesi
- Drag & drop desteği
