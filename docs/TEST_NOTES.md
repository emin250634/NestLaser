# TEST_NOTES.md — NestLaser Desktop

## Test #1 — Tek Plaka Nesting
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** 5 parçanın tek plaka üzerine yerleşimi
**Giriş:** 5 kare parça (50-200mm), 1000x2000 plaka
**Beklenen:** Tüm parçaların yerleşmesi, verimlilik > %50
**Gerçek:** —

## Test #2 — Çoklu Plaka Nesting
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Bir plakaya sığmayan parça seti
**Giriş:** 20 büyük parça (300x400mm), 1000x2000 plaka
**Beklenen:** 2+ plaka kullanılması, yan yana gösterim
**Gerçek:** —

## Test #3 — Overlap Kontrolü
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Parçaların üst üste binmemesi
**Giriş:** 10 farklı boyutta parça
**Beklenen:** Hiçbir yerleşimin overlap etmemesi
**Gerçek:** —

## Test #4 — 90° Rotasyon
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Dikdörtgen parçanın döndürülmesi
**Giriş:** 100x300mm dikdörtgen, 200x200 plaka
**Beklenen:** Parçanın 300x100 olarak yerleşmesi
**Gerçek:** —

## Test #5 — Büyük Parça Uyarısı
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Plakaya sığmayan parça
**Giriş:** 1500x1500mm parça, 1000x2000 plaka
**Beklenen:** Uyarı mesajı, parçanın sığmayanlara eklenmesi
**Gerçek:** —

## Test #6 — Plaka Validasyonu
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Geçersiz plaka ölçüsü
**Giriş:** Genişlik=0, Yükseklik=-100
**Beklenen:** Hata mesajı gösterilmesi
**Gerçek:** —

## Test #7 — Margin Gösterimi
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Kenar boşluğunun kesikli çizgi ile gösterilmesi
**Giriş:** 50mm margin
**Beklenen:** Plaka içinde kesikli dikdörtgen görünmesi
**Gerçek:** —

## Test #8 — Rapor Doğruluğu
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Rapor değerlerinin doğru hesaplanması
**Giriş:** 5 parça, 3 yerleşti, 2 yerleşemedi
**Beklenen:** Toplam=5, Yerleşen=3, Sığmayan=2, Verimlilik hesaplanmış
**Gerçek:** —

## Test #9 — YERLEŞTİR Butonu
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Butonun nesting çalıştırması
**Giriş:** DXF yüklü, plaka ayarları tanımlı
**Beklenen:** Nesting çalışması, önizleme güncellenmesi
**Gerçek:** —

## Test #10 — Temizle Butonu
**Tarih:** 2026-06-12 | **Durum:** ⏳
**Senaryo:** Tüm verilerin temizlenmesi
**Giriş:** Parça listesi ve nesting sonucu var
**Beklenen:** Her şeyin sıfırlanması, durum mesajı
**Gerçek:** —
