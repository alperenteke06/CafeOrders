# Realtime Event Sözlüğü

## 1. Genel

SignalR hub route'u:

```text
/hubs/cafe
```

Event sabitleri:

```text
src/CafeOrders.Application/Contracts/Realtime/CafeHubEvents.cs
```

Client-to-hub metod sabitleri:

```text
src/CafeOrders.Application/Contracts/Realtime/CafeHubMethods.cs
```

## 2. Hub Metodları

| Metod | Çağıran | Amaç |
| --- | --- | --- |
| `JoinDeviceChannel` | DesktopApp | Cihazın kendi `device.{DeviceKey}` grubuna katılması |
| `JoinAdminChannel` | WebUI, AdminAudioAgent, ServerNotifier | Admin grubuna katılma |
| `ReportOrderSoundPlaybackStarted` | WebUI/AdminAudioAgent | Ses playback'in gerçekten başladığını API'ye bildirme |
| `ReportOrderSoundPlaybackFailed` | WebUI/AdminAudioAgent | Ses playback başarısızlığını bildirme |
| `AcknowledgeOrderSound` | WebUI/AdminAudioAgent | Sipariş sesinin çalındığını onaylama |

## 3. Grup Mantığı

| Grup | Kullanım |
| --- | --- |
| `admin` | Admin paneli, AdminAudioAgent, ServerNotifier ve yönetim tüketicileri |
| `device.{DeviceKey}` | Belirli DesktopApp cihazına hedefli mesaj |

## 4. Event Listesi

### `DeviceApproved`

Hedef:

- İlgili cihaz grubu.
- `admin`.

Amaç:

- DesktopApp bekleme ekranından menüye geçer.
- WebUI cihaz durumunu yeniler.

Payload:

- `deviceId`
- `tableId`
- `token`
- `message`

### `DeviceRejected`

Hedef:

- İlgili cihaz grubu.
- `admin`.

Amaç:

- DesktopApp cihaz reddedildi durumuna geçebilir.
- WebUI cihaz listesini günceller.

### `DeviceMapped`

Hedef:

- İlgili cihaz grubu.
- `admin`.

Amaç:

- DesktopApp masa numarasını yeniler.
- Admin paneli cihaz/masa ilişkisini yeniler.

### `DevicesUpdated`

Hedef:

- `admin`.

Amaç:

- Online/offline cihaz durumu.
- Cihaz onay, red, heartbeat ve timeout sonrası liste güncelleme.

### `OrderCreated`

Hedef:

- `admin`.

Amaç:

- WebUI yeni siparişi listeler.
- Header bildirim sayacı güncellenir.
- AdminAudioAgent ses playback queue kontrolüne girer.
- ServerNotifier bekleyen sipariş modalını gösterir.

Payload:

- `OrderDto`

### `OrderAccepted`

Hedef:

- İlgili cihaz grubu.
- `admin`.

Amaç:

- DesktopApp kabul/hazırlanıyor ekranını gösterir.
- WebUI sipariş listesini ve bildirimleri günceller.

Payload:

- `order`
- `message`

### `OrderRejected`

Hedef:

- İlgili cihaz grubu.
- `admin`.

Amaç:

- DesktopApp red/iptal ekranını gösterir.
- WebUI sipariş durumunu günceller.

Payload:

- `order`
- `message`

### `OrderCompleted`

Hedef:

- İlgili cihaz grubu.
- `admin`.

Amaç:

- DesktopApp sipariş tamamlandı/hazır bilgisini gösterir.
- ServerNotifier bekleyen sipariş sayısını düşürür.

Payload:

- `OrderDto`

### `OrderSoundPlaybackStarted`

Hedef:

- `admin`.

Amaç:

- Ses playback sahibinin kim olduğunu görünür kılmak.
- Aynı sipariş için ikinci ses denemesini azaltmak.

Tipik kaynaklar:

- `WebUI`
- `AdminAudioAgent`

### `OrderSoundPlaybackFailed`

Hedef:

- `admin`.

Amaç:

- WebUI veya AdminAudioAgent tarafında ses çalma denemesi başarısızsa log ve fallback akışını tetiklemek.

### `OrderSoundAcknowledged`

Hedef:

- `admin`.

Amaç:

- Sipariş sesinin başarıyla çalındığını ve `IsSoundPlayed=true` olduğunu bildirmek.

### `CatalogUpdated`

Hedef:

- `All`.

Payload:

- realtime version (`long`)

Amaç:

- Ürün/kategori değişimlerinin WebUI ve DesktopApp tarafında canlı yenilenmesi.
- Ürün görseli değiştiğinde kiosk kartlarının yeniden yüklenmesi.

### `TablesUpdated`

Hedef:

- `All`.

Payload:

- realtime version (`long`)

Amaç:

- Masa listesi.
- Cihaz/masa eşleşmesi.
- Dashboard sayısal özetleri.

### `AppSettingsUpdated`

Hedef:

- `All`.

Payload:

- `AppSettingsDto`

Amaç:

- Cafe adı.
- Footer/geliştirici bilgisi.
- Varsayılan kiosk bilgi kutusu.
- Minimum sepet tutarı.
- Yeni sipariş sesi.
- Hızlı onay/canlı duyuru ayarları.

### `InfoMessageUpdated`

Hedef:

- `All`.

Payload:

- `InfoMessageDto`

Amaç:

- Aktif duyuru veya önemli bilgi mesajının canlı yenilenmesi.
- DesktopApp banner rengi, ikon ve metninin güncellenmesi.

### `ApplicationLogCreated`

Hedef:

- `admin`.

Payload:

- `ApplicationLogDto`

Amaç:

- Sistem Logları ekranına yeni log kaydının realtime düşmesi.

## 5. Tüketici Bileşenler

### DesktopApp

Dinlediği ana eventler:

- `DeviceApproved`
- `DeviceRejected`
- `DeviceMapped`
- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`
- `CatalogUpdated`
- `TablesUpdated`
- `AppSettingsUpdated`
- `InfoMessageUpdated`

Davranış:

- Event kaçırılırsa startup/retry ve bazı ekran refresh davranışlarıyla toparlanır.
- Cihaz onayı veya sipariş durumu geldiğinde sepet açık/kapalı fark etmeksizin ekran güncellenir.

### WebUI

Dinlediği ana eventler:

- `OrderCreated`
- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`
- `DeviceApproved`
- `DeviceMapped`
- `DevicesUpdated`
- `CatalogUpdated`
- `TablesUpdated`
- `AppSettingsUpdated`
- `InfoMessageUpdated`
- `ApplicationLogCreated`

Davranış:

- Sayfa shell'i komple reload etmek yerine aktif section partial verisini günceller.
- Bildirim ve arama alanı aktif sayfa ile uyumlu çalışır.

### AdminAudioAgent

Dinlediği ana eventler:

- `OrderCreated`
- `OrderSoundPlaybackStarted`
- `OrderSoundAcknowledged`

Davranış:

- Yeni siparişi queue'ya alır.
- API hazır değilse startup retry yapar.
- Polling ile daha önce çalınmamış siparişleri yakalar.
- Başarılı playback sonrası API'ye `sound-played` bildirir.

### ServerNotifier

Dinlediği ana eventler:

- `OrderCreated`
- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`

Davranış:

- Bekleyen sipariş snapshot'ını günceller.
- Aktif bekleyen sipariş varsa modal gösterir.
- Bekleyen sipariş kalmazsa modal kapanır.

## 6. Dayanıklılık Notları

- SignalR eventleri hızlı UI güncellemesi içindir; kritik akışlarda API snapshot veya polling fallback bulunmalıdır.
- AdminAudioAgent ve ServerNotifier API startup gecikmesine karşı retry ile başlar.
- Cihaz varlığı sadece bağlantı durumuna değil heartbeat zamanına da bakar.
- Ses playback için tek kaynaklı sahiplik hedeflenir; `IsSoundPlayed` ve `SoundPlayedAt` alanları tekrar çalmayı engeller.
