# PROJECT_MEMORY.md — NestLaser Desktop

## Projenin Amacı
Lazer kesim parçalarını minimum fireyle plaka üzerine dizen Windows masaüstü uygulaması.

## Güncel Teknoloji Kararı
| Bileşen | Karar |
|---------|-------|
| Dil | C# .NET 8 |
| UI | WPF (MVVM + ICommand) |
| DXF Kütüphanesi | Geçici olarak devre dışı (API uyuşmazlığı) |
| Nesting Algoritması | Bounding box + Skyline + Overlap kontrolü |
| Platform | Windows 10/11 |

## Mevcut Dosya Yapısı (v0.3 — Faz 3)
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
    │   ├── MainViewModel.cs
    │   └── RelayCommand.cs
    ├── Views/
    │   ├── MainWindow.xaml
    │   └── MainWindow.xaml.cs
    └── Utilities/
        ├── AppConstants.cs
        └── MathHelper.cs
```

## Mevcut Çalışan Özellikler (v0.3.1 — Faz 3A)
- [x] DXF dosyası içe aktarma (Manuel parser: LwPolyline, Polyline, Circle, Arc, Line)
- [x] Kapalı polyline/shape algılama
- [x] Geometry modelleri (Point2D, Polygon, BoundingBox)
- [x] Parça bounding box ve alan hesaplama
- [x] Plaka genişliği/yüksekliği/kenar boşluğu
- [x] 0/90 derece rotasyon desteği
- [x] Skyline tabanlı bottom-left yerleşim
- [x] Overlap kontrolü (bounding box bazlı)
- [x] Çoklu plaka desteği
- [x] Canvas önizleme (çoklu plaka yan yana)
- [x] Margin gösterimi (kesikli çizgi)
- [x] Verimlilik ve fire oranı hesaplama
- [x] DXF export (manuel format)
- [x] ICommand pattern (RelayCommand)
- [x] ObservableCollection<PartModel> parça listesi
- [x] Parça seçme ve silme
- [x] Rapor alanı (toplam, yerleşen, sığmayan, plaka, verimlilik)
- [x] Plaka ölçüsü validasyonu
- [x] Büyük parça uyarısı
- [x] Popup uyarılar kaldırıldı, durum çubuğu mesajları

## Bilinen Hatalar
- Skyline çözümlemesi tam piksel çözünürlüğünde çalışmıyor (1mm adımla)
- DXF export'ta parça kimlik bilgisi kaydedilmiyor
- Spline/Insert entity henüz desteklenmiyor
- DXF import'ta katman bilgisi okunmuyor (hepsi "0" olarak atanıyor)
- Overlap kontrolü sadece bounding box ile yapılıyor (gerçek poligon değil)

## Sınırlamalar
- DXF blok/katman filtreleme yok
- Gerçek kontur (concave) nesting henüz yok
- Parça sürükleme (drag & drop) desteği yok
- DXF çoklu dosya desteği yok

## Son Alınan Kararlar
1. Corel VBA yalnızca prototip olarak kaldı
2. Ana ürün C# .NET 8 WPF olacak
3. İlk MVP bounding box nesting ile çalışacak
4. netDxf.Standard kütüphanesi seçildi
5. Skyline algoritması bottom-left yerine tercih edildi
6. Geometry modelleri ayrı klasöre taşındı
7. NestSettings ile ayarlar yapılandırılabilir
8. ICommand pattern (RelayCommand) kullanıldı
9. ObservableCollection ile parça listesi yönetildi
10. DxfImportResult ile detaylı sonuç modeli
11. NestPlacement'e PlateIndex eklendi (çoklu plaka)
12. NestResult'ta Plates listesi eklendi
13. Overlap kontrolü NestingEngine'e eklendi
14. Plaka validasyonu eklendi
15. netDxf.Standard 2.1.1'e düşürüldü (NuGet uyumluluk)
16. Build hataları giderildi (netDxf 2.x API, değişken çakışması, RelayCommand)
17. netDxf.Standard devre dışı, manuel DXF export eklendi
18. Manuel DXF parser yazıldı (DxfParser.cs)
19. DXF import gerçekten çalışıyor (LwPolyline, Polyline, Circle, Arc, Line)
20. Popup uyarılar kaldırıldı, durum çubuğu mesajları

## Bir Sonraki Önerilen Adım
- Faz 4: DXF çoklu dosya desteği
- Faz 4: Katman filtreleme
- Faz 4: Parça sürükleme (drag & drop)
- Faz 4: Önizleme zoom/pan
