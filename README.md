# NestLaser Desktop

Lazer kesim parçalarını minimum fireyle plaka üzerine dizen Windows masaüstü uygulaması.

## Amaç

CNC lazer kesim makineleri için parça yerleşimi (nesting) yaparak malzeme israfını minimuma indirmek. CorelDRAW VBA makrosundan C# .NET 8 WPF uygulamasına geçiş sürecinde.

## MVP Hedefleri

- [x] DXF dosyası içe aktarma (LwPolyline, Polyline, Circle)
- [x] Kapalı polyline/shape objelerini parça olarak algılama
- [x] Plaka genişliği/yüksekliği/girilmesi
- [x] 0/90 derece rotasyon desteği
- [x] Skyline tabanlı bottom-left yerleşim
- [x] Önizleme ekranında plaka ve parçaları gösterme
- [x] Verimlilik ve fire oranı hesaplama
- [x] Sonucu DXF olarak dışa aktarma

## Teknoloji Yığını

| Bileşen | Teknoloji |
|---------|-----------|
| Dil | C# .NET 8 |
| UI Framework | WPF (MVVM) |
| DXF Kütüphanesi | netDxf.Standard |
| Nesting Algoritması | Bounding Box + Skyline |
| IDE | Visual Studio / VS Code |
| Platform | Windows 10/11 |

## Kurulum Adımları

```bash
# 1. Depoyu klonlayın
git clone https://github.com/nizamiyebilgiislem-pixel/netlast.git
cd netlast/NestLaserDesktop

# 2. Bağımlılıkları yükleyin
dotnet restore

# 3. Uygulamayı çalıştırın
dotnet run --project NestLaserDesktop
```

## Proje Yapısı

```
NestLaserDesktop/
├── Geometry/           # Geometrik modeller (Point2D, Polygon, BoundingBox)
├── Models/             # İş modelleri (PartModel, PlateModel, NestResult, NestSettings)
├── Services/           # DXF Import/Export servisleri
├── Nesting/            # Nesting algoritmaları
├── ViewModels/         # MVVM ViewModeller
├── Views/              # WPF pencereleri ve kontroller
├── Utilities/          # Yardımcı sınıflar (sabitler, matematik)
└── docs/               # Proje dokümantasyonu
```

## Yol Haritası

### v0.1 — İlk MVP ✅
- Temel proje yapısı
- DXF import/export
- Bounding box nesting
- Canvas önizleme

### v0.2 — Geliştirilmiş MVP
- Parça listesi görünümü
- Çoklu dosya desteği
- Katman filtreleme
- Önizleme zoom/pan

### v0.3 — Profesyonel Özellikler
- Gerçek kontur nesting (concave polygon)
- Çarpışma kontrolü (SAT)
- RDWorks uyumlu export
- Rapor PDF çıktısı

### v1.0 — Tam Sürüm
- Tam kontur nesting
- Çoky/plaka optimizasyonu
- Toplu iş processing
- Otomatik güncelleme

## Lisans

Proje özel kullanım amaçlıdır.
