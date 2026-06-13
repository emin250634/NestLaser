# Test Notes

## FAZ 8O Test Doğrulaması

### Build & Tests
- `dotnet build` → 0 warning, 0 error.
- `dotnet test` → 28 passed, 0 failed, 0 skipped.

### Bounding Inspector
- Tek parça seçili iken Properties panelinde Width, Height, Area, Perimeter görünmeli.
- Çoklu seçimde Selection Bounds ve Total Area (Poly) görünmeli.

### Measurement Tool
- Toolbar'daki "Ölç" butonuna tıklayınca araç aktif olmalı.
- Canvas'ta ilk noktaya tıklayınca "A" marker çıkmalı.
- İkinci noktaya tıklayınca ölçüm çizgisi ve bilgi paneli çıkmalı (L, ΔX, ΔY, θ).
- ESC ile ölçüm iptal edilebilmeli.

### CAD Marquee Selection
- Sol→sağ sürükleme: Tamamen sürükleme dikdörtgeni içindeki parçalar seçilmeli.
- Sağ→sol sürükleme: Sürükleme dikdörtgenine temas eden tüm parçalar seçilmeli.

### Status Bar
- Zoom %, X/Y koordinatları, snap durumu ve araç bilgisi status bar'da görünmeli.

## Faz 8J Test Senaryoları

### 1. PDF Teklif Akışı
- DXF Aç → Yerleştir → Malzeme Seç → Makine Seç → Maliyeti Hesapla → PDF Teklif Oluştur.
- Sonuç: PDF içinde yerleşim önizlemesi, fire oranı, süre tahmini, maliyet ve satış fiyatı bulunmalı.

### 2. PDF Üretim Raporu
- Aynı proje ile PDF Üretim Raporu Oluştur çalıştırılmalı.
- Sonuç: Üretim raporu başlığı, malzeme/üretim/maliyet/satış özetleri ve nesting preview görünmeli.

### 3. Company Profile
- Firma adı, adres, telefon, e-posta, web ve logo yolu girilip proje kaydedilmeli.
- Proje tekrar açıldığında bilgiler geri gelmeli ve PDF başlığında kullanılmalı.

### 4. Validasyon
- Proje/DXF yokken, yerleşim yokken, malzeme/makine seçilmemişken veya maliyet hesaplanmamışken PDF oluşturma uyarı vermeli.

### 5. Build
- `dotnet build NestLaserDesktop.csproj`
- Sonuç: 0 warning, 0 error.

## Faz 8B Test Senaryoları

### 1. Basit Şekiller
- Kare ve Dikdörtgen parçaların yerleşimi.
- Sonuç: Eskisi gibi verimli çalışmalı.

### 2. İrregüler Şekiller (L-Shape, C-Shape)
- İç boşluklara diğer parçaların girmesi kontrolü.
- Sonuç: Bounding box yerleşimine göre %15-30 daha fazla verimlilik bekleniyor.

### 3. Advanced Rotation
- Parçaların 15, 30, 45 vb. derecelerle yerleşmesi.
- Sonuç: Daha sıkı paketleme sağlanmalı.

### 4. Büyük Dosya Performansı
- 50+ parça içeren DXF dosyaları.
- Sonuç: Uygulama donmamalı, 60sn içinde sonuç üretmeli.

### 5. Collision Cache Doğrulaması
- Çok sayıda aday pozisyonun hızla elenmesi.
- Sonuç: Cache hit oranının %50+ olması bekleniyor.

### 6. Score Önceliği
- Parçaların plakanın altına (Low Y) ve soluna (Low X) yığılması.
- Sonuç: Düzenli bir paketleme görüntüsü.

## Faz 8B.1 Test Senaryoları

### 1. Algorithm Selection
- Free Rectangle seçildiğinde SAT kontrolü yapılmadan hızlı yerleşim.
- Irregular Experimental seçildiğinde detaylı geometri yerleşimi.

### 2. Fallback Mekanizması
- Karmaşık bir dosya ile Irregular seçilip 10sn beklenmesi.
- Sonuç: "Fallback Used" uyarısı ve Free Rectangle sonuçlarının gösterilmesi.

### 3. Move/Scale Kararlılığı
- Parçaları taşıdıktan veya ölçekledikten sonra "Yerleştir" butonuna basılması.
- Sonuç: Önceki yerleşim sonuçlarının temizlenmesi ve yeni duruma göre nesting yapılması.

### 4. Unplaced Parts
- Plakaya sığmayan parçaların listede kalması ve preview üzerinde (mümkünse) belirtilmesi.

## Faz 8C Test Senaryoları

### 1. Production Export (Nested)
- Birden fazla plaka içeren bir yerleşim yapın.
- DXF Dışa Aktar deyin.
- Sonuç: Tüm plakaların yan yana (offsetli) ve parçaların yerleşmiş halleriyle export edilmesi.

### 2. RDWorks / CorelDRAW Uyumluluğu
- Export edilen DXF'i RDWorks veya CorelDRAW ile açın.
- Sonuç: Katman renklerinin doğru gelmesi (ACI mapping) ve geometrilerin kapalı LWPOLYLINE olarak tanınması.

### 3. Sığmayan Parça Export
- Bazı parçaların sığmadığı bir senaryoda export yapın.
- Sığmayan parçalar için "Evet" deyin.
- Sonuç: Sığmayan parçaların en sağdaki plakanın sağ tarafına düzenli bir şekilde dizilmesi.

### 4. Üretim Raporu Doğrulaması
- Export sonrası `export-report.txt` dosyasını açın.
- Sonuç: Verimlilik, fire ve katman bazlı parça sayılarının doğru raporlanması.

### 5. Katman Filtreleme
- Bir katmanı gizleyip veya "Reference layer export" seçeneğini kapatıp export yapın.
- Sonuç: İlgili katmanların DXF içinde yer almaması.

## Faz 8E Test Senaryoları

### 1. Operation Model Doğrulama
- Yeni proje açıldığında otomatik olarak varsayılan operasyonlar oluşturulmalı.
- Cut katmanı → CutOuter, Mark katmanı → Mark, Engrave katmanı → Engrave eşlemesi doğrulanmalı.

### 2. Operation Sıralama (Drag & Drop)
- Operasyon listesinde sürükle-bırak ile sıralama yapılabilmeli.
- Yukarı/Aşağı butonları ile sıralama değiştirilebilmeli.
- Sıralama değişikliği sonrası öncelik numaraları otomatik güncellenmeli.

### 3. Otomatik Öneri
- "Öner" butonuna basıldığında katman tiplerine göre uygun operasyonlar oluşturulmalı.
- Cut katmanı için CutOuter (ve varsa iç kesim adayları için CutInner) oluşturulmalı.

### 4. İç/Dış Kesim Analizi
- İç boşluklu parçalar (delikli geometriler) için "İç/Dış Analiz" butonu çalıştırılmalı.
- İç boşluk içeren parçalar tespit edildiğinde CutInner operasyonu otomatik eklenmeli.
- Sonuç: "X iç kesim, Y dış kesim adayı bulundu" şeklinde durum mesajı görüntülenmeli.

### 5. Operation Preview
- "Önizleme" checkbox'ı aktif edildiğinde canvas üzerinde seçili operasyonun katmanındaki parçalar vurgulanmalı.
- Vurgulama rengi katman rengine uygun olmalı.

### 6. Export Entegrasyonu
- DXF export yapıldığında export-report.txt içinde "OPERASYON SIRASI (ÜRETİM AKIŞI)" bölümü bulunmalı.
- Operasyon sırası, öncelik numarasına göre sıralanmış ve aktif (Enabled) operasyonları içermeli.

### 7. Proje Kaydetme/Açma (Operations)
- Operasyonlar eklenip proje kaydedildikten sonra proje tekrar açıldığında tüm operasyonlar geri gelmeli.
- Undo/Redo işlemleri operasyon değişikliklerini de kapsamalı.

### 8. Operation Properties
- Operasyon seçildiğinde özellik alanları (ad, tip, güç, hız, pas, öncelik) otomatik doldurulmalı.
- Değişiklikler "Uygula" butonu ile kaydedilmeli.

### 9. Enable/Disable
- Operasyon listesindeki checkbox ile operasyon aktif/pasif yapılabilmeli.
- Pasif operasyonlar export raporunda yer almamalı.

## Faz 8D Test Senaryoları

### 1. Proje Kaydetme ve Açma
- Bir DXF açın, nesting yapın ve katman ayarlarını değiştirin.
- Projeyi Kaydet (Ctrl+S) deyin.
- Programı kapatıp tekrar açın.
- Proje Aç deyin ve kaydettiğiniz dosyayı seçin.
- Sonuç: Tüm parçalar, nesting yerleşimi, katman ayarları ve plaka ayarları birebir geri gelmelidir.

### 2. Kirli Durum (Dirty State) Kontrolü
- Mevcut bir projede bir parçayı kaydırın veya ayarı değiştirin.
- Sonuç: Pencere başlığındaki proje adının yanında `*` işareti çıkmalıdır.
- Projeyi kaydedin.
- Sonuç: `*` işareti kaybolmalıdır.

### 3. Kaydedilmemiş Değişiklik Uyarısı
- Değişiklik yapılmış ama kaydedilmemiş bir projeyken "Yeni Proje" deyin veya programı kapatın.
- Sonuç: "Kaydedilmemiş değişiklikler var. Kaydetmek ister misiniz?" uyarısı çıkmalıdır.

### 4. Son Projeler Listesi
- Birden fazla projeyi farklı isimlerle kaydedin.
- Dosya -> Son Kullanılanlar menüsünü kontrol edin.
- Sonuç: Kaydettiğiniz projeler en yeniden eskiye doğru listelenmelidir.

### 5. Undo/Redo Temizliği
- Bir projeyi açın.
- Sonuç: Önceki projeden kalan undo/redo stack'i temizlenmiş olmalıdır (Geri al butonu pasif olmalı).
# Phase 8N Test Notes

- Added `ProjectPortabilityTests` for migration, `.bak` recovery, `.nelpkg` package import/export, integrity repair, and backup retention.
- Validation result: `dotnet build` 0 warning/0 error; `dotnet test` 28 passed.
