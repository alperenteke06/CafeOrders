# İşlevsel Doküman

## 1. Sistem Amacı

CafeOrders, internet kafe ve e-spor salonlarında müşteri masalarından sipariş alınmasını, admin panelinden sipariş/cihaz/katalog/duyuru yönetimini ve server tarafı operasyon bildirimlerini tek ekosistemde toplar.

Sistemin ana hedefleri:

- Client makinelerde fullscreen kiosk sipariş ekranı sunmak.
- Admin kullanıcısına WebUI üzerinden cihaz, masa, ürün, kategori, sipariş, ayar, bildirim ve log yönetimi sağlamak.
- Sipariş, katalog, cihaz, duyuru, ayar ve log değişikliklerini realtime yansıtmak.
- Web tarayıcısı sesi çalamadığında AdminAudioAgent ile yeni sipariş sesini garantiye almak.
- ServerNotifier ile bekleyen siparişleri Windows sağ alt köşesinde görünür tutmak.

## 2. Kullanıcı Rolleri

### Kiosk Kullanıcısı

- Ürünleri ve kategorileri görür.
- Sepete ürün ekler, miktar değiştirir ve sepeti temizler.
- Minimum sepet tutarı tanımlıysa bu tutarın altında sipariş gönderemez.
- Sipariş oluşturulduktan sonra durum ekranında ilerlemeyi takip eder.
- Admin onay, red ve tamamlandı durumlarını realtime ekran geçişi veya popup olarak alır.

### Admin / Operatör

- Cihaz kayıtlarını onaylar, reddeder ve masalara bağlar.
- Siparişleri bekleyen, onaylanan, reddedilen ve tamamlanan durumlarıyla yönetir.
- Ürün ve kategori CRUD işlemlerini yapar.
- Hızlı fiyat girişi ile filtrelenen ürünlerin fiyatlarını toplu günceller.
- Kiosk bilgi metni, duyuru tipi, ikon, minimum sepet tutarı, yeni sipariş sesi, marka ve footer bilgilerini yönetir.
- Sistem loglarını kaynak ve seviye bazında izler.

### Server Operatörü

- API, WebUI, AdminAudioAgent ve ServerNotifier süreçlerini WatchDog ile ayakta tutar.
- ServerNotifier üzerinden bekleyen sipariş sayısını görür.
- AdminAudioAgent loglarından sesin kim tarafından çalındığını takip eder.

## 3. Cihaz Kayıt ve Onay Akışı

1. DesktopApp açılır ve cihaz kimliğini üretir.
2. API'ye `POST /api/v1/devices/register` isteği gönderir.
3. Cihaz daha önce onaylanmamışsa kilit/bekleme ekranında kalır.
4. Admin WebUI Devices ekranından cihazı onaylar ve masaya bağlar.
5. API `DeviceApproved`, `DeviceMapped` ve `DevicesUpdated` eventlerini yayınlar.
6. DesktopApp realtime event ile menü ekranına geçer.
7. Cihaz heartbeat göndermeye başlar ve online/offline durumu WebUI'da realtime izlenir.

DesktopApp başlangıçta API henüz hazır değilse retry mekanizması ile kayıt/heartbeat akışını tekrar dener.

## 4. Katalog ve Ürün Yönetimi

### Kiosk Tarafı

- Kategori pills ile filtreleme yapılır.
- `Tümü` seçiliyken ürünler kategori sırası ve ürün bilgilerine göre düzenli listelenir.
- Ürün kartında görsel, ad, açıklama, fiyat ve ekleme aksiyonları bulunur.
- Ürün görselleri kırpılmadan premium kart görünümünde gösterilir.

### WebUI Tarafı

- Ürün ekleme/düzenleme popup'ı; local dosya, sürükle-bırak ve URL üzerinden görsel kullanımını destekler.
- Kategori seçim dropdown'u tema uyumlu ve scrollable çalışır.
- Hızlı Fiyat Girişi popup'ı aktif filtredeki ürünleri tablo halinde getirir.
- Ürün/kategori değişimi sonrası `CatalogUpdated` event'i yayınlanır.
- DesktopApp ve WebUI katalog bilgisini realtime yeniler.

## 5. Sepet ve Minimum Tutar Akışı

Kiosk sepetinde:

- Ürün kalemleri görsel, ürün adı, adet kontrolü, silme butonu ve satır tutarı ile gösterilir.
- Miktar arttırma/azaltma kontrolleri custom WPF tasarımla gösterilir.
- Sepet toplamı, minimum sepet tutarı ve eksik tutar bilgisi gösterilir.

Minimum tutar davranışı:

- `AppSettings.MinimumOrderAmount` boşsa her tutardaki sepet siparişe gönderilebilir.
- Örneğin minimum tutar `100 TL` ise, `100 TL` altındaki siparişlerde onay butonu pasif kalır.
- Ayar WebUI'dan değiştirildiğinde `AppSettingsUpdated` event'i ile DesktopApp'e realtime yansır.

## 6. Sipariş Akışı

1. Kiosk kullanıcısı sepeti onaylar.
2. DesktopApp `POST /api/v1/orders` ile siparişi API'ye gönderir.
3. API siparişi `Pending` olarak kaydeder.
4. WebUI admin paneline `OrderCreated` event'i gider.
5. ServerNotifier bekleyen sipariş sayısını gösterir.
6. AdminAudioAgent yeni sipariş sesi için playback sahipliği akışına girer.
7. DesktopApp "Siparişiniz Alındı" ekranına geçer.

Sipariş durumları:

| Durum | Anlam |
| --- | --- |
| `Pending` | Sipariş admin onayı bekliyor |
| `Accepted` | Sipariş kabul edildi, hazırlanıyor |
| `Rejected` | Sipariş reddedildi/iptal edildi |
| `Completed` | Sipariş tamamlandı |

## 7. Admin Sipariş Yönetimi

WebUI Orders ekranı tüm siparişleri gösterir:

- Bekleyen siparişler en üstte, kendi içinde oluşturulma tarihine göre sıralanır.
- Tamamlanan/reddedilen/onaylanan siparişler kendi tarih sırasıyla listelenir.
- Global arama kutusu aktif sayfadaki kayıtları filtreler.

Admin aksiyonları:

- Siparişi onayla: `OrderAccepted` event'i yayınlanır.
- Siparişi reddet: `OrderRejected` event'i yayınlanır.
- Siparişi tamamla: `OrderCompleted` event'i yayınlanır.

DesktopApp, menü ekranında veya sipariş durum ekranında olsa bile bu eventleri almalı ve ilgili ekranı göstermelidir.

## 8. Yeni Sipariş Sesi

Ses playback güvence modeli:

- WebUI odakta ve tarayıcı izinleri uygunsa yeni sipariş sesini çalabilir.
- WebUI sesi gerçekten başlatırsa `ReportOrderSoundPlaybackStarted` ve `AcknowledgeOrderSound` akışına katılır.
- WebUI çalamazsa AdminAudioAgent siparişi queue'ya alır.
- AdminAudioAgent sistem ses seviyesini kontrol eder, gerekirse yükseltir ve sesi native olarak çalar.
- Başarılı playback sonrası `POST /api/v1/orders/{orderId}/sound-played` ile sipariş `IsSoundPlayed=true` olur.

Amaç aynı sipariş için çift ses çalmasını engellemek ve hiç ses çalmama riskini azaltmaktır.

## 9. Duyuru ve Kiosk Bilgi Mesajları

WebUI Settings ekranından yönetilen alanlar:

- Varsayılan kiosk bilgi metni.
- Bilgi tipi: `Önemli`, `Duyuru`, `Genel`.
- İkon anahtarı.
- Aktif duyuru mesajı.

Renk davranışı:

| Tip | Sunum |
| --- | --- |
| Önemli | Kırmızı tonlar |
| Duyuru | Tema altın/sarı tonu |
| Genel | Açık mavi tonlar |

Bu ayarlar `AppSettingsUpdated` ve `InfoMessageUpdated` eventleriyle DesktopApp ve WebUI'a realtime yansır.

## 10. Bildirimler ve Loglar

WebUI'da:

- Header notification alanı yeni sipariş ve durum değişikliklerini gösterir.
- Bildirimlerde ilgili kayda git aksiyonu bulunur.
- Sistem Logları ekranı API, WebUI, DesktopApp, AdminAudioAgent ve ServerNotifier kaynaklarını izler.
- Loglar seviye, kaynak ve arama filtresiyle incelenebilir.

Log kaynakları:

- `API`
- `WebUI`
- `DesktopApp`
- `AdminAudioAgent`
- `ServerNotifier`

## 11. ServerNotifier Akışı

ServerNotifier server PC'de çalışan küçük WPF bildirim uygulamasıdır.

Davranış:

- API ve Hub'a bağlanır.
- Bekleyen siparişleri realtime ve polling fallback ile izler.
- Bekleyen sipariş varsa ekranın sağ alt köşesinde taskbar üstünde top-most modal gösterir.
- Modalda sipariş adedi, masa listesi ve "Siparişleri Görüntüle" butonu bulunur.
- Bekleyen sipariş kalmadığında modal otomatik kapanır.

## 12. Operasyon Senaryoları

### Yeni Cihaz Devreye Alma

1. DesktopApp açılır.
2. Cihaz bekleme ekranına düşer.
3. Admin cihazı onaylar ve masaya bağlar.
4. DesktopApp otomatik menüye geçer.

### Yeni Sipariş

1. Müşteri ürünü sepete ekler.
2. Minimum tutar sağlanıyorsa siparişi gönderir.
3. Admin paneli, ServerNotifier ve ses ajanı bilgilendirilir.
4. Admin siparişi kabul eder veya reddeder.
5. DesktopApp ilgili durum ekranını realtime gösterir.

### Ürün Görseli Güncelleme

1. Admin ürün görselini WebUI üzerinden yükler.
2. Görsel `wwwroot/uploads/products` altına kaydedilir.
3. Ürün kaydı güncellenir.
4. `CatalogUpdated` event'i yayınlanır.
5. DesktopApp görseli HTTP veya shared path üzerinden yeniden yükler.

### Acil Duyuru

1. Admin duyuru metnini yazar.
2. Tipi `Önemli` seçer.
3. İkon anahtarını belirler.
4. DesktopApp kırmızı uyarı sunumuna realtime geçer.

## 13. Gelecek Gelişim Alanları

- Stok yönetimi.
- Rol bazlı admin yetkilendirme.
- Mutfak ekranı.
- Gün sonu raporu ve satış analitiği.
- Ürün bazlı kampanya tanımları.
- Admin aksiyon geçmişi için daha detaylı audit raporu.
