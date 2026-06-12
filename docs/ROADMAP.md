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
- [x] README.md oluşturuldu
- [x] Geometry klasörü (Point2D, Polygon, BoundingBox, GeometryUtils)
- [x] Modeller yeniden yapılandırıldı (PartModel, PlateModel, NestResult, NestSettings)
- [x] Nesting/ klasörü (Engine→Nesting yeniden adlandırma)
- [x] Views klasörü
- [x] Utilities klasörü (AppConstants, MathHelper)
- [x] Eski dosyalar temizlendi
- [x] Namespace güncellemeleri

## v0.2 — Geliştirilmiş MVP
- [ ] Parça listesi görünümü (DataGrid)
- [ ] Parça seçme ve silme
- [ ] Çoklu dosya DXF yükleme
- [ ] DXF katman filtreleme
- [ ] Parça adı/ID gösterimi
- [ ] Önizleme zoom/pan
- [ ] Hata mesajları ve uyarılar
- [ ] Ayarları kaydet/yükle (JSON)

## v0.3 — Profesyonel Özellikler
- [ ] Parçaları sürükleme (drag & drop)
- [ ] Gerçek kontur nesting (concave polygon)
- [ ] Çarpışma kontrolü (SAT / Minkowski)
- [ ] Birden fazla plaka desteği
- [ ] Plaka seçimi ve yönetimi
- [ ] Rapor PDF çıktısı
- [ ] RDWorks uyumlu DXF formatı

## v1.0 — Tam Sürüm
- [ ] Tam kontur nesting algoritması
- [ ] Çoklu plaka optimizasyonu
- [ ] Otomatik parça sıralama stratejileri
- [ ] Toplu iş.processing
- [ ] Preset yönetimi (malzeme/kalınlık)
- [ ] Keyboard shortcuts
- [ ] Auto-update mekanizması
