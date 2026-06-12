# DECISIONS.md — NestLaser Desktop

## Karar #1 — Prototip Tech Stack
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** CorelDRAW 2025 VBA macro yalnızca prototip olarak kaldı. Ana ürün C# .NET 8 tabanlı Windows masaüstü uygulaması olacak.
**Gerekçe:** VBA'nın nesting algoritması için yeterli API sunmaması, gerçek kontur yerleşimi için sınırlı olması.

## Karar #2 — UI Teknolojisi
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** WPF (Windows Presentation Foundation) kullanılacak.
**Gerekçe:** MVVM deseni için doğal destek, Canvas tabanlı önizleme için uygun, modern UI oluşturma kolaylığı.

## Karar #3 — DXF Kütüphanesi
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** netDxf.Standard kütüphanesi kullanılacak.
**Gerekçe:** Açık kaynak, .NET 8 uyumlu, LwPolyline/Polyline/Circle okuma-yazma desteği, aktif geliştirme.

## Karar #4 — İlk Nesting Algoritması
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** İlk MVP'de bounding box nesting kullanılacak.
**Gerekçe:** Hızlı uygulama, temel yerleşim için yeterli, gerçek kontur nesting sonraki fazda eklenecek.

## Karar #5 — Yerleşim Stratejisi
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** Skyline tabanlı bottom-left yerleşim tercih edildi.
**Gerekçe:** Basit bottom-left'e göre daha iyi alan kullanımı, skyline yükseklik haritası ile daha verimli yerleştirme.

## Karar #6 — MVVM Kullanımı
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** MVVM (Model-View-ViewModel) mimarisi kullanılacak.
**Gerekçe:** Kod ayrılabilirliği, test edilebilirlik, WPF ile doğal entegrasyon.
