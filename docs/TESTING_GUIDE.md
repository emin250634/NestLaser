# NestLaserDesktop Testing Guide

## Amaç

FAZ 8L ile NestLaserDesktop için xUnit tabanlı regresyon test altyapısı eklendi. Hedef, yeni fazlardan sonra temel CAD, DXF, proje, maliyet ve PDF davranışlarının bozulmasını erken yakalamaktır.

## Komutlar

```powershell
dotnet restore NestLaserDesktop.sln
dotnet build NestLaserDesktop.sln
dotnet test NestLaserDesktop.sln
```

Release doğrulaması için:

```powershell
dotnet build NestLaserDesktop.sln --configuration Release
dotnet test NestLaserDesktop.sln --configuration Release
```

## Test Projesi

Test projesi:

```text
NestLaserDesktop.Tests
```

Framework:

```text
xUnit
```

## Test Kategorileri

- `GeometryTests`: polygon alan, perimeter, winding, cleanup, validasyon ve transform regresyonları.
- `DxfImportTests`: `samples/dxf` fixture paketinden DXF import ölçü, unit detect, bounding box ve warning kontrolleri.
- `DxfExportTests`: DXF export smoke ve `export-report.txt` üretimi.
- `ProjectTests`: `.nelp` save/load, profile snapshot, PDF ayarları ve güvenli backup kaydı.
- `CostTests`: material cost modları ve çoklu cut operation zaman hesabı regresyonu.
- `PdfTests`: quotation ve production PDF smoke testi, `%PDF` header doğrulaması.
- `LayerTests`: layer clone ve workflow flag korunumu.
- `OperationTests`: operation clone, speed unit, priority ve enabled korunumu.

## DXF Fixture Pack

Fixture dosyaları:

- `samples/dxf/unitless_lwpolyline.dxf`
- `samples/dxf/millimeter_lwpolyline.dxf`
- `samples/dxf/inch_lwpolyline.dxf`
- `samples/dxf/polyline_rectangle.dxf`
- `samples/dxf/circle.dxf`
- `samples/dxf/arc.dxf`
- `samples/dxf/spline.dxf`
- `samples/dxf/ellipse.dxf`

Fixture beklentileri import davranışının korunması içindir. Dosyalar değiştirilirse test beklentileri de bilinçli olarak güncellenmelidir.

## CI

GitHub Actions workflow:

```text
.github/workflows/dotnet.yml
```

Her push ve pull request için:

- restore
- build
- test

çalıştırır.

## RC Beklentisi

Release Candidate hazırlığında `dotnet restore`, `dotnet build` ve `dotnet test` 0 error ile tamamlanmalıdır. Warning oluşursa `docs/TEST_NOTES.md` içine gerekçesi yazılmalı ve v1.0 öncesi değerlendirilmelidir.
