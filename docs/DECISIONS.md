# DECISIONS.md — NestLaser Desktop

## Karar #1 — Prototip Tech Stack
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** CorelDRAW 2025 VBA macro yalnızca prototip olarak kaldı. Ana ürün C# .NET 8 tabanlı Windows masaüstü uygulaması olacak.
**Gerekçe:** VBA'nın nesting algoritması için yeterli API sunmaması.

## Karar #2 — UI Teknolojisi
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** WPF (Windows Presentation Foundation) kullanılacak.
**Gerekçe:** MVVM deseni için doğal destek, Canvas tabanlı önizleme için uygun.

## Karar #3 — DXF Kütüphanesi
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** netDxf.Standard kütüphanesi kullanılacak.
**Gerekçe:** Açık kaynak, .NET 8 uyumlu, aktif geliştirme.

## Karar #4 — İlk Nesting Algoritması
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** İlk MVP'de bounding box nesting kullanılacak.
**Gerekçe:** Hızlı uygulama, gerçek kontur nesting sonraki fazda.

## Karar #5 — Yerleşim Stratejisi
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** Skyline tabanlı bottom-left yerleşim tercih edildi.
**Gerekçe:** Basit bottom-left'e göre daha iyi alan kullanımı.

## Karar #6 — MVVM Kullanımı
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** MVVM (Model-View-ViewModel) mimarisi kullanılacak.
**Gerekçe:** Kod ayrılabilirliği, test edilebilirlik.

## Karar #7 — Geometry Modelleri Ayrıştırma
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** Geometrik modeller (Point2D, Polygon, BoundingBox) ayrı Geometry klasörüne taşındı.
**Gerekçe:** Geometrik hesaplamaların iş mantığından ayrılması, SAT algoritması için altyapı.

## Karar #8 — Ayarlar Yapılandırması
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** NestSettings sınıfı ile nesting parametreleri yapılandırılabilir hale getirildi.
**Gerekçe:** Farklı senaryolar için kolay ayar değişikliği, gelecekte JSON kaydetme desteği.

## Karar #9 — Proje Yapısı
**Tarih:** 2026-06-12
**Durum:** ✅ Uygulandı
**Karar:** Proje yapısı Geometry, Models, Services, Nesting, ViewModels, Views, Utilities olarak yeniden yapılandırıldı.
**Gerekçe:** Profesyonel mimari, modüler genişletilebilirlik, DXF Import/Export ve RDWorks Export için altyapı.
