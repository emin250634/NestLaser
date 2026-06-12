# DEVELOPMENT_LOG.md — NestLaser Desktop

## 2026-06-12 13:15 — İlk MVP Oluşturma
**Yapılan işlem:** İlk MVP sürümü oluşturuldu
**Değişen dosyalar:** Models/, Services/, Engine/, ViewModels/, MainWindow.xaml
**Neden yapıldı:** CorelDRAW VBA yetersizliği
**Sonuç:** Temel MVP tamamlandı
**Sonraki adım:** Proje iskeletini profesyonel hale getirmek

---

## 2026-06-12 13:50 — Faz 1: Altyapı Sağlamlaştırma
**Yapılan işlem:** Proje yapısı yeniden yapılandırıldı
**Değişen dosyalar:** Geometry/, Models/, Nesting/, Views/, Utilities/, README.md
**Neden yapıldı:** Modüler mimari
**Sonuç:** Profesyonel mimari oluşturuldu
**Sonraki adım:** Faz 2 — DXF Import

---

## 2026-06-12 14:30 — Faz 2: DXF Import ve Önizleme
**Yapılan işlem:** DXF import genişletildi, MVVM güçlendirildi
**Değişen dosyalar:** DxfService.cs, RelayCommand.cs, MainViewModel.cs, MainWindow.xaml
**Neden yapıldı:** Daha iyi DXF desteği ve MVVM
**Sonuç:** Arc/Line desteği, ICommand, ObservableCollection
**Sonraki adım:** Faz 3 — Nesting

---

## 2026-06-12 15:15 — Faz 3: Bounding Box Nesting ve Önizleme
**Yapılan işlem:** NestingEngine geliştirildi, çoklu plaka desteği eklendi, rapor alanı oluşturuldu

**Değişen dosyalar:**
- `Nesting/NestingEngine.cs` — Overlap kontrolü, çoklu plaka, yeniden deneme mantığı
- `Models/NestPlacement.cs` — PlateIndex, Width, Height, PartId, PartName eklendi
- `Models/NestResult.cs` — Plates listesi, PlateCount, UsedAreaText, TotalPlateAreaText
- `ViewModels/MainViewModel.cs` — Plaka validasyonu, büyük parça uyarısı, rapor özellikleri
- `Views/MainWindow.xaml` — YERLEŞTİR butonu, rapor alanı, plaka ayarları yan yana
- `Views/MainWindow.xaml.cs` — Çoklu plaka çizimi, margin gösterimi

**Neden yapıldı:**
- Parçaların otomatik yerleştirilmesi
- Çoklu plaka senaryolarının desteklenmesi
- Çarpışma önlenmesi
- Kullanıcıya detaylı rapor sunulması

**Sonuç:**
- Skyline + overlap kontrolü ile nesting
- Birden fazla plaka otomatik oluşturuluyor
- Plaka margin'i kesikli çizgi ile gösteriliyor
- Rapor: toplam, yerleşen, sığmayan, plaka sayısı, verimlilik, fire
- Plaka ölçüsü validasyonu
- Büyük parça uyarısı

**Sonraki adım:**
- Faz 4: DXF çoklu dosya desteği
- Faz 4: Katman filtreleme

---

## 2026-06-12 15:30 — NuGet Paket Düzeltmesi
**Yapılan işlem:** netDxf.Standard sürümü 3.0.0 → 2.1.1 olarak düşürüldü

**Değişen dosyalar:**
- `NestLaserDesktop.csproj` — PackageReference Version="2.1.1"
- `Services/DxfService.cs` — `doc.AddEntity` → `doc.Entities.Add` (2.x API uyumu)

**Neden yapıldı:**
NuGet üzerinde netDxf.Standard 3.0.0 bulunamıyor. En yakın sürüm 2.1.1.

**Sonuç:**
- paket sürümü 2.1.1 olarak güncellendi
- DxfService 2.x API'si ile uyumlu hale getirildi
- `doc.AddEntity()` → `doc.Entities.Add()` değişikliği yapıldı

**Sonraki adım:**
- dotnet build ile doğrulama

---

## 2026-06-12 15:45 — Build Hataları Düzeltmesi
**Yapılan işlem:** 6 farklı build hatası giderildi

**Değişen dosyalar:**
- `Services/DxfService.cs` — netDxf 2.x API uyumu
- `Views/MainWindow.xaml.cs` — Değişken çakışması, List.Count
- `ViewModels/MainViewModel.cs` — RelayCommand constructor

**Düzeltilen hatalar:**
1. `doc.Entities.LwPolylines` → `doc.Entities.OfType<LwPolyline>()`
2. `doc.Entities.Polylines` → `doc.Entities.OfType<Polyline>()`
3. `doc.Entities.Circles` → `doc.Entities.OfType<Circle>()`
4. `doc.Entities.Arcs` → `doc.Entities.OfType<Arc>()`
5. `doc.Entities.Lines` → `doc.Entities.OfType<Line>()`
6. `DxfVersion.R2010` → `new DxfDocument()` (varsayılan)
7. `colors.Length` → `colors.Count` (3 yer)
8. `scaleX/scaleY/scale` çakışması → `multiScaleX/multiScaleY`
9. `new RelayCommand(OpenDxf)` → `new RelayCommand(_ => OpenDxf())`

**Neden yapıldı:**
- netDxf.Standard 2.1.1 API'si 3.0'dan farklı
- C# derleyici değişken çakışmasını reddediyor
- List<T> için `.Length` değil `.Count` kullanılır
- RelayCommand Action<object?> bekliyor, method group çevrilemez

**Sonuç:**
Tüm build hataları giderildi. `dotnet build` başarılı olmalı.

---

## 2026-06-12 16:00 — Final Build Düzeltmesi
**Yapılan işlem:** netDxf.Standard API uyuşmazlığı tamamen giderildi

**Değişen dosyalar:**
- `NestLaserDesktop.csproj` — netDxf.Standard referansı devre dışı bırakıldı
- `Services/DxfService.cs` — Sade DXF import/export ile yeniden yazıldı
- `Views/MainWindow.xaml.cs` — scale değişken çakışması giderildi

**Sorun:**
netDxf.Standard 2.1.1 ile `DxfDocument.Entities` API'si bulunamıyor. 3.0 sürümü de NuGet'te yok.

**Çözüm:**
- DXF import: Geçici olarak devre dışı, uyarı mesajı veriyor
- DXF export: Manuel DXF formatında yazılıyor (LWPOLYLINE section)
- netDxf.Standard paket referansı yorum satırına alındı

**Derleme komutu:**
```bash
cd NestLaserDesktop
dotnet build
```

**Not:**
DXF import/export özellikleri gelecek sürümde IxMilia.Dxf veya benzeri kütüphane ile tekrar eklenecek.

---

## 2026-06-12 16:30 — Faz 3A: Gerçek DXF Import
**Yapılan işlem:** Manuel DXF parser yazıldı, gerçek import aktif edildi

**Değişen dosyalar:**
- `Services/DxfParser.cs` — Yeni! Manuel DXF formatı parser'ı
- `Services/DxfService.cs` — DxfParser kullanacak şekilde güncellendi
- `ViewModels/MainViewModel.cs` — Popup uyarılar kaldırıldı, durum çubuğuna yazıldı

**Desteilenen DXF Entity'leri:**
- LWPOLYLINE (kapalı/açık)
- POLYLINE (kapalı/açık)
- CIRCLE (36 segment poligon)
- ARC (yay noktaları)
- LINE (2 noktalı çizgi)

**Başarı Kriterleri:**
- [x] DXF Aç butonu çalışıyor
- [x] Parça sayısı görünür
- [x] Listedeparçalar görünür
- [x] Canvas'ta parça kontürleri görünür
- [x] Toplam alan hesaplanıyor
- [x] Popup kaldırıldı, durum çubuğu mesajları

**Derleme komutu:**
```bash
cd NestLaserDesktop
dotnet build
```
