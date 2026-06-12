# PROJECT_MEMORY.md — NestLaser Desktop

## Projenin Amacı
Lazer kesim parçalarını minimum fireyle plaka üzerine dizen Windows masaüstü uygulaması.
CorelDRAW VBA makrosundan C# .NET 8 WPF uygulamasına geçiş sürecinde.

## Güncel Teknoloji Kararı
| Bileşen | Karar |
|---------|-------|
| Dil | C# .NET 8 |
| UI | WPF (MVVM) |
| DXF Kütüphanesi | netDxf.Standard |
| Nesting Algoritması | Bounding box + Skyline (MVP) |
| Platform | Windows 10/11 |

## Mevcut Dosya Yapısı (v0.1.1 — Faz 1)
```
NestLaserDesktop/
├── README.md
├── NestLaserDesktop.sln
├── docs/
│   ├── PROJECT_MEMORY.md
│   ├── DEVELOPMENT_LOG.md
│   ├── ROADMAP.md
│   ├── DECISIONS.md
│   └── TEST_NOTES.md
└── NestLaserDesktop/
    ├── NestLaserDesktop.csproj
    ├── App.xaml / App.xaml.cs
    ├── Geometry/
    │   ├── Point2D.cs
    │   ├── Polygon.cs
    │   ├── BoundingBox.cs
    │   └── GeometryUtils.cs
    ├── Models/
    │   ├── PartModel.cs
    │   ├── PlateModel.cs
    │   ├── NestResult.cs
    │   ├── NestPlacement.cs
    │   └── NestSettings.cs
    ├── Services/
    │   └── DxfService.cs
    ├── Nesting/
    │   └── NestingEngine.cs
    ├── ViewModels/
    │   └── MainViewModel.cs
    ├── Views/
    │   ├── MainWindow.xaml
    │   └── MainWindow.xaml.cs
    └── Utilities/
        ├── AppConstants.cs
        └── MathHelper.cs
```

## Mevcut Çalışan Özellikler (v0.1)
- [x] DXF dosyası içe aktarma (LwPolyline, Polyline, Circle)
- [x] Kapalı polyline/shape algılama
- [x] Geometry modelleri (Point2D, Polygon, BoundingBox)
- [x] Parça bounding box ve alan hesaplama
- [x] Plaka genişliği/yüksekliği/kenar boşluğu/girilmesi
- [x] 0/90 derece rotasyon desteği
- [x] Skyline tabanlı bottom-left yerleşim
- [x] Önizleme ekranında plaka ve parçaları gösterme
- [x] Verimlilik ve fire oranı hesaplama
- [x] Sonucu DXF olarak dışa aktarma
- [x] NestSettings ile ayarlanabilir parametreler

## Bilinen Hatalar
- Skyline çözümlemesi tam piksel çözünürlüğünde çalışmıyor (1mm adımla)
- DXF export'ta parça kimlik bilgisi kaydedilmiyor (sadece geometri)
- Çarpışma kontrolü yapılmıyor (sadece bounding box)

## Sınırlamalar
- Sadece kapalı polylines ve circle destekleniyor
- DXF blok/katman filtreleme yok
- Gerçek kontur (concave) nesting henüz yok
- Parça listesi.drag-drop veya çoklu dosya desteği yok

## Son Alınan Kararlar
1. Corel VBA yalnızca prototip olarak kaldı
2. Ana ürün C# .NET 8 WPF olacak
3. İlk MVP bounding box nesting ile çalışacak
4. netDxf.Standard kütüphanesi seçildi
5. Skyline algoritması bottom-left yerine tercih edildi
6. Geometry modelleri ayrı klasöre taşındı
7. NestSettings ile ayarlar yapılandırılabilir hale getirildi
8. Proje yapısı profesyonel düzeye çıkarıldı (Faz 1)

## Bir Sonraki Önerilen Adım
- Faz 2: Parça listesi görünümü (DataGrid)
- Faz 2: Çoklu dosya DXF desteği
- Faz 2: Katman filtreleme
- Faz 2: Önizleme zoom/pan
