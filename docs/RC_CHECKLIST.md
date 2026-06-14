# Release Candidate Checklist

## Build

- `dotnet restore NestLaserDesktop.sln` 0 error ile tamamlanır.
- `dotnet build NestLaserDesktop.sln` 0 error ile tamamlanır.
- Release build doğrulaması yapılır.

## Tests

- `dotnet test NestLaserDesktop.sln` 0 error ile tamamlanır.
- Geometry, DXF import/export, project, cost, PDF, layer ve operation testleri geçer.
- Yeni faz sonrası regresyon testi tekrar çalıştırılır.

## DXF Import

- Unitless DXF uyarı üretir ve güvenli mm varsayımıyla açılır.
- Millimeter ve inch unit detect/scale korunur.
- LWPOLYLINE, POLYLINE, CIRCLE, ARC, SPLINE, ELLIPSE fixture dosyaları import edilir.
- Bounding box ve part count beklentileri korunur.

## DXF Export

- Export edilen DXF dosyası oluşur.
- Layer visible/reference kuralları korunur.
- Plate border opsiyonu çalışır.
- `export-report.txt` oluşur.

## Project Save/Load

- `.nelp` proje roundtrip çalışır.
- Company profile ve PDF ayarları korunur.
- Material/Machine profile snapshot proje içine kaydedilir.
- Eksik profil durumunda null reference oluşmaz; kullanıcıya profil uyarısı verilir.

## Cost Calculation

- `PerSheet`, `PerSquareMeter`, `PerKg` material cost hesapları korunur.
- Aynı layer üzerinde birden fazla cut operation olduğunda operasyon zamanı global cut length ile şişmez.
- Waste, labor, machine, electricity ve consumable kalemleri toplam üretim maliyetine doğru girer.

## PDF Report

- Quotation PDF oluşur.
- Production PDF oluşur.
- PDF dosyaları boş değildir ve `%PDF` header içerir.
- Nesting preview, fire oranı, süre, maliyet ve satış fiyatı raporda yer alır.

## Material Profiles

- Material JSON güvenli kaydedilir.
- `.bak` backup üretilir.
- JSON parse hataları loglanır.
- Bozuk veri sessiz veri kaybına dönüşmez.

## Machine Profiles

- Machine JSON güvenli kaydedilir.
- `.bak` backup üretilir.
- Eksik proje profili snapshot ile tolere edilir.

## Crash Logging

- `AppDomain.UnhandledException` loglanır.
- `DispatcherUnhandledException` loglanır.
- `TaskScheduler.UnobservedTaskException` loglanır.
- Log hedefi: `%APPDATA%/NestLaser/logs/error-log.txt`.

## Release Packaging (FAZ 8P)

- Assembly metadata mevcut (Product, Company, Copyright, Version).
- AppVersion sınıfı merkezi sürüm bilgisi sağlıyor.
- İlk çalıştırma AppData klasörleri otomatik oluşturuyor.
- Portable publish profili mevcut.
- build-release.ps1 scripti çalışıyor.
- RELEASE_CHECKLIST.md dokümanı mevcut.

## RC Çıkış Kriteri

- Build ve test komutları 0 error verir.
- Veri kaybı riski yüksek JSON kayıtları temp/replace/backup akışıyla yazılır.
- Kritik crash durumları log dosyasına düşer.
- v1.0 öncesi must-have eksikler dokümante edilmiştir.
- FAZ 8P release packaging hazır.
