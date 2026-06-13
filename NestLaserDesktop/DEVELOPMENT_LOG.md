# Development Log

## [2026-06-13] FAZ 8O — CAD Workspace Performance & Measurement UX
- CAD Workspace denetimi yapıldı ve `docs/CAD_WORKSPACE_REVIEW.md` raporu oluşturuldu.
- **Bounding Inspector:** Properties paneli genişletildi — Area, Perimeter, Total Area (Poly) eklendi.
- **Measurement Tool:** Yeni "Ölç" aracı. İki nokta arası mesafe (mm), ΔX, ΔY, açı hesaplar; status bar ve overlay üzerinde gösterir. ESC ile iptal.
- **CAD Marquee Selection:** Sol→sağ sürükleme = bounding box tamamen içeridekiler (full containment). Sağ→sol sürükleme = temas edenler (intersection).
- **Selection UX:** Selection outline 1.8→2.0px, hover 1.5→2.0px, corner handles 6→7px + stroke 1→1.2px.
- **Render Optimization:** Tüm SolidColorBrush'lar static frozen olarak cache'lendi. Grid, ruler, plate, selection, snap, hover hesaplı brush kullanıyor. GC baskısı azaltıldı.
- **Drag Performance:** Drag sırasında gereksiz redraw atlaması eklendi.
- **Snap UX:** Snap marker 10→12px, stroke 2→2.5px, fill alpha 70→100. Görünürlük arttı.
- **ZoomToSelection:** Ctrl+F tuşu veya programatik olarak seçili parçalara zoom.
- **Mini Status Inspector:** Status bar'a araç bilgisi eklendi: `Araç: Seç | Pan | Ölç`.
- **Build:** 0 warning, 0 error.
- **Tests:** 28/28 tüm testler geçiyor.

## [2026-06-13] FAZ 8J - PDF Quotation & Production Report System
- PDF teklif ve üretim raporu sistemi eklendi.
- `PdfReportService` ile dış NuGet bağımlılığı olmadan native PDF çıktısı üretiliyor.
- PDF içinde şirket bilgileri, opsiyonel logo, proje/malzeme/makine bilgileri, yerleşim önizlemesi, verimlilik, fire, üretim süreleri, maliyet kırılımı ve satış/KDV toplamları yer alıyor.
- Yeni modeller: `CompanyProfile`, `PdfReportSettings`.
- `.nelp` içine CompanyProfile ve son PDF ayarları kaydediliyor.
- Dosya menüsüne ve Cost/Teklif sekmesine `PDF Teklif Oluştur` ve `PDF Üretim Raporu Oluştur` eklendi.
- Validasyon: proje/DXF yoksa, yerleşim yoksa, malzeme/makine seçilmediyse veya maliyet hesaplanmadıysa PDF oluşturma uyarı veriyor.
- Build: 0 warning, 0 error.

## [2026-06-13] FAZ 8B - Irregular Shape Nesting Engine
- Bounding Box tabanlı yerleşimden Geometry/Anchor tabanlı yerleşime geçildi.
- Aday pozisyon üretimi geliştirildi: Plaka köşeleri, Free rectangle köşeleri, mevcut parça köşeleri ve polygon vertexleri artık aday olarak kullanılıyor.
- Advanced Rotation desteği eklendi: 15 derecelik adımlarla 24 farklı açı denenebiliyor.
- Collision sistemi optimize edildi:
    - Bounding Box pre-check.
    - SAT (Separating Axis Theorem) exact check.
    - Collision Cache (aynı parçanın aynı konum ve açıdaki çakışma durumları saklanıyor).
- Yerleşim puanlama (Scoring) sistemi güncellendi:
    1. En düşük Y (MinY)
    2. En düşük X (MinX)
    3. En düşük toplam yükseklik (MaxY)
    4. En düşük boş alan (MaxX)
    5. En yüksek sıkışıklık (BBox Area)
- Nesting istatistikleri eklendi: Deneme sayısı, aday pozisyon sayısı, cache hit oranı vb.
- Analysis paneli güncellenen istatistikleri gösterecek şekilde revize edildi.
- 60 saniyelik timeout koruması eklendi.
- Kararlılık ve exception koruması sağlandı.

## [2026-06-13] FAZ 8B.1 - Stabilization & Fallback
- Nesting Engine stabilize edildi:
    - Üç farklı algoritma seçeneği eklendi: Free Rectangle, Polygon Collision, Irregular Experimental.
    - Irregular Experimental için 10 saniyelik timeout ve otomatik "Free Rectangle" fallback mekanizması eklendi.
    - Aday pozisyon (candidate) üretimi her parça için 200 ile sınırlandırılarak performans artırıldı.
- UI Regresyonları düzeltildi:
    - Taşıma (move) ve ölçekleme (scale) işlemleri sonrası çizimin yenilenmemesi ve NestResult çakışmaları giderildi.
    - Ölçekleme sonrası alan hesaplamaları ve NestResult temizleme mantığı düzeltildi.
- Parça Görünürlüğü:
    - Yerleşemeyen parçaların (Unplaced) sistemden silinmesi engellendi, listede kalmaları sağlandı.
- Analiz Paneli Geliştirmeleri:
    - Kullanılan gerçek algoritma, fallback durumu ve timeout bilgisi eklendi.
- Varsayılan algoritma "Free Rectangle" olarak güncellendi.

## [2026-06-13] FAZ 8C - Production DXF Export
- Üretim odaklı DXF export sistemi tamamlandı.
- RDWorks ve CorelDRAW uyumluluğu için LWPOLYLINE ve AutoCAD 2000 (AC1015) formatı standartlaştırıldı.
- Katman (Layer) bazlı renk ve özellik koruması sağlandı:
    - Hex renk kodlarından ACI (AutoCAD Color Index) mapping eklendi.
    - Cut, Engrave, Mark gibi katmanlar standart renklerle eşleştirildi.
- Çoklu plaka desteği: Plakalar arası otomatik offset ile yan yana export.
- Sığmayan parça yönetimi: Kullanıcı onayıyla sığmayan parçaların plaka dışına düzenli dizilerek export edilmesi sağlandı.
- Üretim Raporu: `export-report.txt` dosyası ile verimlilik, fire, katman ayarları ve parça istatistikleri sunuldu.
- UI Geliştirmeleri: Export seçenekleri eklendi ve işlem sonrası rapor görüntüleme opsiyonu sunuldu.

## [2026-06-13] FAZ 8F - Technical Audit & Bug Fixes
- **Kapsam:** Tüm FAZ 8A–8E sistemlerinin teknik denetimi.
- **Rapor:** `docs/TECHNICAL_AUDIT.md` oluşturuldu — 11 sistem, 14 bulgu, 3 düzeltme.

### Critical Fix: NestPlacement.Part deserialization duplication
- **Sorun:** .nelp yüklemesi sonrası `NestResult.Placed[].Part`, `Parts[]` listesindeki asıl nesnelerden farklı birer kopya olarak deserileze ediliyordu. Katman değişiklikleri nesting preview'de yansımıyor, seçim vurgulama hatalı eşleşiyordu.
- **Düzeltme:** `RemapNestResultPartReferences()` metodu eklendi. Proje yükleme sonrası NestPlacement.Part ve Unplaced referansları ana Parts listesindeki nesnelere yeniden bağlanıyor.
- **Etkilenen dosyalar:** `ViewModels/MainViewModel.cs`

### High Fix: DeleteSelectedLayer operasyon referanslarını güncellemiyor
- **Sorun:** Katman silindiğinde o katmanın LayerId'sine bağlı LaserOperation'lar güncellenmiyor, geçersiz referans kalıyordu.
- **Düzeltme:** Silme öncesi operasyonlar taranıp fallback katmanının ID'si atanıyor.
- **Etkilenen dosyalar:** `ViewModels/MainViewModel.cs`

### High Fix: Export raporu yazma hatası
- **Sorun:** `Path.GetDirectoryName` root-relatif yollarda null dönebiliyordu. Dizin yoksa `File.WriteAllText` hata fırlatıyordu.
- **Düzeltme:** Geçerli dizin fallback'i, otomatik dizin oluşturma ve try-catch ile rapor yazma koruması eklendi.
- **Etkilenen dosyalar:** `Services/DxfService.cs`

## [2026-06-13] FAZ 8E - Operation Manager & Laser Process Pipeline
- **LaserOperation Modeli**: Yeni model, Id, Name, LayerId, OperationType, Power, Speed, PassCount, Priority ve Enabled alanlarını içeriyor. INotifyPropertyChanged desteği ile UI binding.
- **OperationType Enum**: Engrave, Mark, CutInner, CutOuter, Reference tipleri tanımlandı.
- **Operation Manager Panel**: Operasyonlar sekmesi eklendi - operasyon listesi, sürükle-bırak sıralama, ekle/sil, yukarı/aşağı taşıma butonları, otomatik öneri ve iç/dış kesim analizi.
- **Drag & Drop Sıralama**: ListBox üzerinde PreviewMouseLeftButtonDown/Move/Drop eventleri ile sürükle-bırak sıralama desteği.
- **Otomatik Öneri**: Katman tiplerine göre operasyon önerisi: Cut→CutOuter, Mark→Mark, Engrave→Engrave.
- **İç/Dış Kesim Analizi**: Point-in-polygon (ray casting) ve polygon containment algoritmaları ile iç kesim/dış kesim tespiti. Bounding box ön kontrolü + vertex containment doğrulaması.
- **Operation Preview**: "Önizleme" checkbox'ı ile seçili operasyonun katman renginde canvas üzerinde vurgulanması.
- **Export Entegrasyonu**: DxfService.Export() parametrelerine operations listesi eklendi. Export raporuna "OPERASYON SIRASI (ÜRETİM AKIŞI)" bölümü eklendi.
- **Project System Entegrasyonu**: NestLaserProject modeline Operations listesi, UndoSnapshot'a Operations desteği, CreateProjectFromState/ApplyProject'te operasyon kaydetme/yükleme.
- **GeometryUtils Geliştirmeleri**: PointInPolygon (ray casting), PolygonContainsPolygon (containment test) ve BoundingBox.Contains(BoundingBox) eklendi.
- **Build**: 0 warning, 0 error.

## [2026-06-13] FAZ 8D - Project System (.nelp)
- Yeni `.nelp` (NestLaser Project) dosya formatı geliştirildi.
- `ProjectService` ile JSON tabanlı serileştirme altyapısı kuruldu.
- Çalışma Alanı Saklama:
    - Plaka ayarları, tüm parçalar (geometri, transformasyonlar), katman ayarları ve nesting sonuçları proje dosyasına kaydediliyor.
    - Proje açıldığında tüm durum (nesting sonucu dahil) geri yükleniyor.
- Kirli Durum (Dirty State) Yönetimi:
    - Kaydedilmemiş değişiklikler için pencere başlığında `*` işareti gösterimi eklendi.
    - Uygulama kapatılırken veya yeni proje açılırken kaydedilmemiş değişiklikler için uyarı mekanizması eklendi.
- Son Projeler (Recent Projects):
    - AppData/NestLaser altında saklanan son 5 proje listesi menüye eklendi.
- UI Entegrasyonu: Menü ve Toolbar'a Yeni, Aç, Kaydet, Farklı Kaydet komutları eklendi.
# Phase 8N - Project Portability & Data Migration

- Added project version metadata and `ProjectMigrationService`.
- Added `ProjectIntegrityService`, `ProjectRecoveryReport`, and `.bak` recovery path.
- Added latest-10 dated project backups under `Backups`.
- Added `.nelpkg` package export/import and File menu commands.
- Added portability regression tests; `dotnet test` passes with 28 tests.
