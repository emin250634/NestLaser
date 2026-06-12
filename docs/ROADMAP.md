# ROADMAP.md — NestLaser Desktop

## v0.1 — İlk MVP ✅
- [x] Proje yapısı oluşturuldu
- [x] DXF import (LwPolyline, Polyline, Circle)
- [x] Kapalı parça algılama
- [x] Bounding box hesaplama
- [x] Plaka ayarları
- [x] 0/90° rotasyon desteği
- [x] Skyline bottom-left nesting
- [x] Canvas önizleme
- [x] Verimlilik/fire oranı hesaplama
- [x] DXF export

## v0.1.1 — Faz 1: Altyapı Sağlamlaştırma ✅
- [x] README.md
- [x] Geometry klasörü
- [x] Modeller yeniden yapılandırıldı
- [x] Nesting/, Views/, Utilities/ klasörleri

## v0.2 — Faz 2: DXF Import ve Önizleme ✅
- [x] RelayCommand ICommand pattern
- [x] DxfService Arc/Line desteği
- [x] DxfImportResult
- [x] ObservableCollection parça listesi
- [x] Gerçek kontur çizimi

## v0.3 — Faz 3: Bounding Box Nesting ✅
- [x] Overlap kontrolü
- [x] Çoklu plaka desteği
- [x] NestPlacement PlateIndex/Width/Height
- [x] NestResult Plates listesi
- [x] Plaka validasyonu
- [x] Büyük parça uyarısı
- [x] Rapor alanı (toplam, yerleşen, sığmayan, plaka, verimlilik, fire)
- [x] YERLEŞTİR butonu
- [x] Çoklu plaka yan yana önizleme
- [x] Margin gösterimi (kesikli çizgi)

## v0.4 — Faz 4: Geliştirilmiş Import
- [ ] DXF çoklu dosya desteği
- [ ] DXF katman filtreleme
- [ ] Parça sürükleme (drag & drop)
- [ ] Önizleme zoom/pan
- [ ] Parça adı/ID düzenleme
- [ ] PartQuantity (çoklu kopya)
- [ ] Ayarları kaydet/yükle (JSON)

## v0.5 — Faz 5: Profesyonel Özellikler
- [ ] Gerçek kontur nesting (concave polygon)
- [ ] Çarpışma kontrolü (SAT / Minkowski)
- [ ] RDWorks uyumlu DXF formatı
- [ ] Rapor PDF çıktısı

## v1.0 — Tam Sürüm
- [ ] Tam kontur nesting algoritması
- [ ] Çoklu plaka optimizasyonu
- [ ] Otomatik parça sıralama stratejileri
- [ ] Toplu iş processing
- [ ] Preset yönetimi (malzeme/kalınlık)
- [ ] Keyboard shortcuts
- [ ] Auto-update mekanizması
