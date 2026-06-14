# NestLaser Desktop

NestLaser Desktop, lazer kesim parçalarını DXF dosyalarından içe aktararak gerçek kontur tabanlı nesting yapan C# .NET 8 WPF masaüstü uygulamasıdır.

Amaç; CNC lazer kesim üretiminde parçaları plaka üzerine daha verimli yerleştirmek, fire oranını azaltmak, DXF çıktısı almak ve üretim öncesi maliyet/teklif sürecini tek uygulama içinde yönetmektir.

## Mevcut Durum

Proje prototip aşamasını geçmiş, beta öncesi stabilizasyon ve gerçek DXF uyumluluğu aşamasındadır.

Şu anda çalışan ana yetenekler:

- DXF içe aktarma
- Kapalı şekilleri parça olarak algılama
- Plaka ölçüsü, kenar boşluğu ve parça aralığı ile nesting çalıştırma
- Free Rectangle / Polygon Collision / Shape-Aware / True Shape Nesting algoritmaları
- SAT tabanlı gerçek geometri çakışma kontrolü
- Gap-fill ve boşluk adayları ile küçük parçaları boş alanlara yerleştirme
- Çoklu plaka desteği
- DXF export
- Katman ve operasyon yönetimi
- Malzeme ve makine profilleri
- Cost / teklif hesaplama altyapısı
- PDF rapor altyapısı
- Test projesi ve regression testleri

## Öne Çıkan Teknik Özellikler

### True Shape Nesting

Gerçek parça geometrisi üzerinden çalışan nesting modudur. Parçaları sadece dikdörtgen bounding box gibi değil, polygon konturu üzerinden değerlendirir.

Desteklenen temel yaklaşımlar:

- SAT collision check
- Boundary check
- Vertex / edge anchor adayları
- Empty-space candidate noktaları
- Plate free-space adayları
- Gap-fill denemeleri
- Local refinement
- Adaptive candidate budget
- Multi-plate continuation
- Fallback kontrolü

### Debug ve Analiz

Nesting sonuçlarında şu metrikler izlenebilir:

- Candidate sayısı
- Çakışma kontrol sayısı
- Boundary reject
- Bounding box reject
- SAT reject
- Accepted true-shape count
- Gap-fill başarı sayısı
- Empty-space candidate sayısı
- Plate free-space candidate sayısı
- Timeout / fallback durumu

Bu sayaçlar gerçek DXF dosyalarında algoritmanın neden başarılı veya başarısız olduğunu anlamak için kullanılır.

## Teknoloji Yığını

| Bileşen | Teknoloji |
|---|---|
| Dil | C# |
| Runtime | .NET 8 |
| UI | WPF |
| Mimari | MVVM |
| Platform | Windows 10 / Windows 11 |
| Test | .NET Test Project |
| Sürüm Kontrol | Git / GitHub |

## Kurulum

```bash
git clone https://github.com/emin250634/NestLaser.git
cd NestLaser
dotnet restore
dotnet build -c Release
```

Uygulamayı geliştirme modunda çalıştırmak için:

```bash
dotnet run --project NestLaserDesktop.csproj
```

Release build almak için:

```bash
dotnet publish -c Release
```

## Test

Tüm testleri çalıştırmak için:

```bash
dotnet test -c Release
```

TrueShape testleri bazı gerçek geometri senaryolarında uzun sürebilir. Gerekirse hızlı test için TrueShape testleri hariç çalıştırılabilir:

```bash
dotnet test -c Release --filter "FullyQualifiedName!~TrueShape"
```

## Proje Yapısı

```text
NestLaser/
├── .github/workflows/        # GitHub Actions
├── Geometry/                 # Point2D, Polygon, BoundingBox, GeometryUtils
├── Models/                   # PartModel, PlateModel, NestResult, NestSettings vb.
├── Nesting/                  # NestingEngine, TrueShapeTrace
├── Services/                 # DXF, proje, export, cost, material servisleri
├── Utilities/                # Yardımcı sınıflar
├── ViewModels/               # MVVM ViewModel katmanı
├── Views/                    # WPF ekranları
├── NestLaserDesktop.Tests/   # Test projesi
├── docs/                     # Teknik dokümantasyon
├── samples/dxf/              # Örnek DXF dosyaları
├── scripts/                  # Build / release scriptleri
├── App.xaml
├── App.xaml.cs
├── NestLaserDesktop.csproj
├── NestLaserDesktop.sln
└── README.md
```

## Kullanım Akışı

1. Uygulamayı aç.
2. DXF dosyasını içe aktar.
3. Plaka ölçüsünü gir.
4. Kenar boşluğu ve parça aralığını ayarla.
5. Algoritma olarak gerçek şekilli işler için `True Shape Nesting` seç.
6. Yerleştirmeyi başlat.
7. Analiz sekmesinden verimlilik, fire, debug ve performans metriklerini kontrol et.
8. Sonucu DXF olarak dışa aktar.

## Geliştirme Notları

Gerçek DXF dosyalarında `True Shape Nesting` tercih edilmelidir. `Free Rectangle` modu hızlıdır ancak parçaları dikdörtgen kutu gibi değerlendirdiği için altıgen, daire, spline veya düzensiz şekillerde boşlukları verimli kullanamaz.

Yakın vadeli geliştirme başlıkları:

- DXF BLOCK / INSERT / gruplanmış şekil desteği
- TrueShape performans optimizasyonu
- Ağır testlerin kategoriye ayrılması
- Default algoritmanın TrueShape olarak ayarlanması
- README ve dokümantasyonun sürekli güncel tutulması
- İlk beta release hazırlığı

## Yol Haritası

### v0.9 Beta Öncesi

- TrueShape nesting stabilizasyonu
- Gerçek DXF testleri
- Group / Block / Insert davranışlarının doğrulanması
- Test suite düzenleme
- GitHub repo temizliği

### v1.0 İlk Stabil Sürüm

- Güvenilir DXF import/export
- Stabil true-shape nesting
- Çoklu plaka üretim senaryoları
- Maliyet / teklif raporu
- Release paketleme

## Lisans

Proje özel kullanım ve geliştirme amaçlıdır.
