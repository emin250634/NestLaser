# TEST_NOTES.md — NestLaser Desktop

Henüz test yapılmadı. İlk test senaryoları aşağıda planlandı.

---

## Test #1 — DXF Import
**Tarih:** —
**Durum:** ⏳ Bekliyor

**Senaryo:** Basit kapalı polyline içeren DXF dosyasını yükleme

**Giriş:** test_simple.dxf (kare ve daire)
**Beklenen:** 2 parça algılanması, bounding box hesaplanması
**Gerçek:** —
**Sorunlar:** —

---

## Test #2 — Skyline Nesting
**Tarih:** —
**Durum:** ⏳ Bekliyor

**Senaryo:** 5 parçanın 1000x2000 plaka üzerine yerleşimi

**Giriş:** 5 farklı boyutta kare parç
**Beklenen:** Tüm parçaların yerleşmesi, verimlilik > %60
**Gerçek:** —
**Sorunlar:** —

---

## Test #3 — DXF Export
**Tarih:** —
**Durum:** ⏳ Bekliyor

**Senaryo:** Nesting sonucunu DXF olarak kaydetme

**Giriş:** Nesting yapılmış sonuç
**Beklenen:** Geçerli DXF dosyası oluşması, plaka + parçaların export edilmesi
**Gerçek:** —
**Sorunlar:** —

---

## Test #4 — Kenar Boşluğu
**Tarih:** —
**Durum:** ⏳ Bekliyor

**Senaryo:** 50mm kenar boşluğu ile yerleşim

**Giriş:** Plaka 1000x2000, margin 50
**Beklenen:** Parçaların plaka sınırları içinde, margin dışında yerleşmesi
**Gerçek:** —
**Sorunlar:** —

---

## Test #5 — Büyük Parça Hatası
**Tarih:** —
**Durum:** ⏳ Bekliyor

**Senaryo:** Plakadan büyük parçanın yerleştirilememesi

**Giriş:** 500x500 parça, 1000x2000 plaka (beklenen: yerleşir)
**Beklenen:** Parçanın yerleşmesi
**Gerçek:** —
**Sorunlar:** —
