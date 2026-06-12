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

## Mevcut Dosya Yapısı
```
NestLaserDesktop/
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
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── Models/
    │   ├── Point2D.cs
    │   ├── Part.cs
    │   ├── Plate.cs
    │   └── NestResult.cs
    ├── Services/
    │   └── DxfService.cs
    ├── Engine/
    │   └── NestingEngine.cs
    └── ViewModels/
        └── MainViewModel.cs
```

## Mevcut Çalışan Özellikler (v0.1)
- [x] DXF dosyası içe aktarma (LwPolyline, Polyline, Circle)
- [x] Kapalı polyline/shape algılama
- [x] Parça bounding box ve alan hesaplama
- [x] Plaka genişliği/yüksekliği/kenar boşluğu/girilmesi
- [x] 0/90 derece rotasyon desteği
- [x] Skyline tabanlı bottom-left yerleşim
- [x] Önizleme ekranında plaka ve parçaları gösterme
- [x] Verimlilik ve fire oranı hesaplama
- [x] Sonucu DXF olarak dışa aktarma

## Bilinen Hatalar
- Skyline çözümlemesi tam piksel çözünürlüğünde çalışmıyor (1mm adımla)
- DXF export'ta parça kimlik bilgisi kaydedilmiyor (sadece geometri)
- WinForms/MVVM dönüşümü henüz yapılmadı, test edilmedi

## Sınırlamalar
- Sadece kapalı polylines ve circle destekleniyor
- DXF blok/katman filtreleme yok
- Gerçek kontur (concave) nesting henüz yok
- Çarpışma kontrolü yapılmıyor (sadece bounding box)
- Parça listesi.drag-drop veya çoklu dosya desteği yok

## Son Alınan Kararlar
1. Corel VBA yalnızca prototip olarak kaldı
2. Ana ürün C# .NET 8 WPF olacak
3. İlk MVP bounding box nesting ile çalışacak
4. netDxf.Standard kütüphanesi seçildi
5. Skyline algoritması bottom-left yerine tercih edildi

## Bir Sonraki Önerilen Adım
- Parçaları Canvas üzerinde sürükleme (drag & drop)
- DXF çoklu dosya desteği
- Parça listesi görünümü (DataGrid)
- Katman bazlı filtreleme
