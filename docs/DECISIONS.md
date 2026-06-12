# DECISIONS.md — NestLaser Desktop

## Karar #1 — Prototip Tech Stack
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Ana ürün C# .NET 8 WPF olacak.

## Karar #2 — DXF Kütüphanesi
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** netDxf.Standard kullanılacak.

## Karar #3 — İlk Nesting Algoritması
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Bounding box nesting kullanılacak.

## Karar #4 — Yerleşim Stratejisi
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Skyline tabanlı bottom-left yerleşim.

## Karar #5 — MVVM Kullanımı
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** MVVM mimarisi kullanılacak.

## Karar #6 — ICommand Pattern
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** RelayCommand (ICommand) pattern kullanılacak.

## Karar #7 — DXF Entity Desteği
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** LwPolyline, Polyline, Circle, Arc, Line desteklenecek.

## Karar #8 — Overlap Kontrolü
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** NestingEngine'de bounding box bazlı overlap kontrolü yapılacak.
**Gerekçe:** Parçaların üst üste binmemesi için.

## Karar #9 — Çoklu Plaka Desteği
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Bir plaka dolarsa otomatik yeni plaka oluşturulacak.
**Gerekçe:** Büyük parça setlerinde tek plaka yetersiz kalabilir.

## Karar #10 — NestPlacement PlateIndex
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Her yerleşim hangi plakaya ait olduğunu belirtecek.
**Gerekçe:** Çoklu plaka önizlemesi ve raporlama için.

## Karar #11 — Rapor Alanı
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Sol panelde detaylı rapor alanı olacak (toplam, yerleşen, sığmayan, plaka, verimlilik, fire).
**Gerekçe:** Kullanıcıya anında geri bildirim.

## Karar #12 — Plaka Validasyonu
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Plaka boyutları ve kenar boşluğu için validasyon eklenecek.
**Gerekçe:** Geçersiz parametrelerle hatalı sonuçların önlenmesi.

## Karar #13 — Büyük Parça Uyarısı
**Tarih:** 2026-06-12 | **Durum:** ✅
**Karar:** Plakaya sığmayan parçalar için uyarı verilecek.
**Gerekçe:** Kullanıcının beklentisini karşılamak.
