# Realtime Event Sozlugu

## 1. Genel

SignalR hub route'u:

- `/hubs/cafe`

Event sabitleri:

- `DeviceApproved`
- `DeviceRejected`
- `DeviceMapped`
- `OrderCreated`
- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`
- `CatalogUpdated`
- `TablesUpdated`
- `AppSettingsUpdated`
- `InfoMessageUpdated`

Kaynak:

- `src/CafeManagement.Application/Contracts/Realtime/CafeHubEvents.cs`

## 2. Grup Mantigi

Ana gruplar:

- `admin`
- `device.DeviceKey`

Amac:

- admin paneline toplu operasyon bildirimi
- ilgili kiosk cihaza hedefli bildirim

## 3. Event Listesi

### `DeviceApproved`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyDeviceApprovedAsync`

Hedef:

- ilgili cihaz grubu
- `admin`

Payload:

- `deviceId`
- `tableId`
- `token`
- `message`

Kullanim:

- kiosk bekleme ekranindan cikis
- admin paneli cihaz durumunun guncellenmesi

### `DeviceRejected`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyDeviceRejectedAsync`

Hedef:

- `admin`

Kullanim:

- admin tarafinda cihaz listesi tazeleme

### `DeviceMapped`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyDeviceMappedAsync`

Hedef:

- `admin`
- ilgili cihaz grubu

Payload:

- `deviceId`
- `tableId`
- `hostName`

Kullanim:

- kiosk masa bilgisinin yenilenmesi
- admin tablo / cihaz iliskisinin tazelenmesi

### `OrderCreated`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyOrderCreatedAsync`

Hedef:

- `admin`

Payload:

- `OrderDto`

Kullanim:

- admin panelinde yeni siparis bildirimi

### `OrderAccepted`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyOrderAcceptedAsync`

Hedef:

- ilgili cihaz grubu
- `admin`

Payload:

- `order`
- `message`

Kullanim:

- kioskta `Siparisiniz Hazirlaniyor`
- admin siparis listesinin guncellenmesi

### `OrderRejected`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyOrderRejectedAsync`

Hedef:

- ilgili cihaz grubu
- `admin`

Payload:

- `order`
- `message`

Kullanim:

- kioskta red / iptal ekraninin acilmasi
- admin bildirimlerinin guncellenmesi

### `OrderCompleted`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyOrderCompletedAsync`

Hedef:

- ilgili cihaz grubu
- `admin`

Payload:

- `OrderDto`

Kullanim:

- kioskta hazirlandi / tamamlandi durumunun gosterilmesi

### `CatalogUpdated`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyCatalogUpdatedAsync`

Hedef:

- `All`

Payload:

- `long` realtime version

Kullanim:

- WPF katalog ve gorsel yenileme
- WebUI urun / kategori bolumu tazeleme

### `TablesUpdated`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyTablesUpdatedAsync`

Hedef:

- `All`

Payload:

- `long` realtime version

Kullanim:

- cihaz online/offline
- masa snapshot
- admin dashboard sayisal ozet guncellemeleri

### `AppSettingsUpdated`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyAppSettingsUpdatedAsync`

Hedef:

- `All`

Payload:

- `AppSettingsDto`

Kullanim:

- footer
- cafe adi
- kiosk bilgi kutusu varsayilan sunumu
- geliştirici bilgileri

### `InfoMessageUpdated`

Yayinlayan:

- `SignalRRealtimeNotifier.NotifyInfoMessageUpdatedAsync`

Hedef:

- `All`

Payload:

- `InfoMessageDto`

Kullanim:

- kiosk aktif duyuru / onemli mesaj kutusu
- admin panel sunum durumu

## 4. Tuketici Tarafi

### DesktopApp

`RealtimeClient` tarafinda dinlenen temel eventler:

- `DeviceApproved`
- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`
- `CatalogUpdated`
- `TablesUpdated`
- `DeviceMapped`
- `AppSettingsUpdated`
- `InfoMessageUpdated`

### WebUI

Admin dashboard script tarafinda dinlenen temel eventler:

- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`
- `DeviceApproved`
- `DeviceMapped`
- `CatalogUpdated`
- `TablesUpdated`
- `AppSettingsUpdated`
- `InfoMessageUpdated`

## 5. Operasyonel Notlar

- katalog ve table event'leri `version` payload ile gonderilir
- kiosk istemci event uzerinden tekrar veri ceker
- event bir noktada kacirilsa bile bazi akislar fallback refresh / polling ile toparlanir
- hedefli cihaz event'lerinde ana anahtar `DeviceKey` grubudur
