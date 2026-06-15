# Teknik Mimari

## 1. Genel Bakış

CafeOrders, .NET 8 tabanlı çok bileşenli bir LAN sipariş yönetim çözümüdür. Sistem, client makinelerde çalışan WPF kiosk uygulaması ile server üzerinde çalışan API, WebUI, AdminAudioAgent ve ServerNotifier bileşenlerini SignalR ve SQL Server üzerinden birleştirir.

Ana prensipler:

- API merkezi veri ve realtime iletişim noktasıdır.
- WebUI admin paneli olarak HTTP API ve SignalR kullanır.
- DesktopApp kiosk ekranıdır; cihaz onayı, katalog, sepet ve sipariş ekranlarını yönetir.
- AdminAudioAgent yeni sipariş sesi için native Windows fallback sağlar.
- ServerNotifier bekleyen siparişleri server ekranında top-most modal ile gösterir.
- SetupWizard Production branch paketini indirip server kurulumunu kullanıcı dostu WPF ekranı ile yönetir.
- Loglar hem yerel dosyaya hem de merkezi `ApplicationLogEntries` tablosuna yazılır.

## 2. Çözüm Yapısı

```text
CafeOrders/
|-- docs/
|-- publishes/
|-- scripts/
|-- src/
|   |-- CafeOrders.API/
|   |-- CafeOrders.Application/
|   |-- CafeOrders.Domain/
|   |-- CafeOrders.Infrastructure/
|   |-- CafeOrders.WebUI/
|   |-- CafeOrders.DesktopApp/
|   |-- CafeOrders.AdminAudioAgent/
|   |-- CafeOrders.ServerNotifier/
|   |-- CafeOrders.SetupWizard/
|-- tests/
|   |-- CafeOrders.Tests/
```

## 3. Katmanlar

### `CafeOrders.Domain`

Entity ve enum katmanıdır.

Öne çıkan entity'ler:

- `AdminUser`
- `AppSetting`
- `ApplicationLogEntry`
- `CafeTable`
- `Category`
- `Device`
- `InfoMessage`
- `Order`
- `OrderLine`
- `Product`

Öne çıkan alanlar:

- `AppSetting.MinimumOrderAmount`: minimum sepet tutarı.
- `AppSetting.NewOrderSoundUrl`: yeni sipariş sesi.
- `Order.IsSoundPlayed`: sipariş sesinin çalınıp çalınmadığı.
- `Order.SoundPlayedAt`: sesin ne zaman çalındığı.
- `ApplicationLogEntry`: merkezi log paneli için kayıt modeli.

### `CafeOrders.Application`

DTO, servis arayüzleri ve realtime kontratları içerir.

Öne çıkan kontratlar:

- `IAdminAuthService`
- `IApplicationLogService`
- `ICatalogService`
- `IDashboardService`
- `IDeviceService`
- `IOrderService`
- `IRealtimeNotifier`
- `ISettingsService`
- `ITableService`

Realtime sabitleri:

- `CafeHubEvents`
- `CafeHubMethods`

### `CafeOrders.Infrastructure`

Teknik implementasyon katmanıdır.

Alt sorumluluklar:

- EF Core `CafeOrdersDbContext`.
- Migration ve seed işlemleri.
- Servis implementasyonları.
- SignalR `CafeHub` ve `SignalRRealtimeNotifier`.
- JWT ve admin auth servisleri.
- Merkezi/yerel logging.
- Cihaz presence izleme hosted service.

### `CafeOrders.API`

REST API ve SignalR hub host uygulamasıdır.

Başlangıçta:

- Infrastructure servislerini kaydeder.
- CORS, auth, controller ve SignalR ayarlarını yapar.
- `Database.MigrateAsync()` çalıştırır.
- `DbSeeder.SeedAsync(...)` ile temel verileri uygular.
- `/hubs/cafe` route'u üzerinden realtime hub açar.

### `CafeOrders.WebUI`

MVC tabanlı admin panelidir.

Sorumluluklar:

- Admin login ve uzun süreli cookie oturumu.
- Dashboard, sipariş, ürün, kategori, cihaz, masa, ayar, bildirim ve log ekranları.
- Ürün görseli ve sipariş sesi upload işlemleri.
- API'ye HTTP üzerinden yazma istekleri.
- SignalR üzerinden realtime admin ekran güncellemeleri.

### `CafeOrders.DesktopApp`

Client makinelerde çalışan WPF kiosk uygulamasıdır.

Sorumluluklar:

- Fullscreen kiosk deneyimi.
- Cihaz kayıt/onay bekleme ekranı.
- Katalog ve kategori gösterimi.
- Sepet ve minimum tutar kontrolü.
- Sipariş oluşturma ve durum ekranları.
- Realtime cihaz/sipariş/katalog/ayar/duyuru güncellemeleri.
- Oturum süresi sonunda otomatik kapanma.
- Yerel dosya loglama.

### `CafeOrders.AdminAudioAgent`

Server PC'de çalışan console/native yardımcı uygulamadır.

Sorumluluklar:

- API ve Hub bağlantısını retry ile kurmak.
- Çalınmamış siparişleri polling ve hub eventleriyle yakalamak.
- Ses dosyasını WebUI shared root, cache veya fallback path üzerinden çözmek.
- Sistem ses seviyesini kontrol etmek ve gerekirse yükseltmek.
- Yeni sipariş sesini queue ile sırayla çalmak.
- Playback durumunu API'ye bildirmek.
- Logları kendi klasörüne yazmak.

### `CafeOrders.ServerNotifier`

Server PC'de çalışan WPF bildirim uygulamasıdır.

Sorumluluklar:

- Bekleyen sipariş snapshot bilgisini API'den almak.
- Hub üzerinden realtime güncellenmek.
- Fallback polling ile kaçan eventleri toparlamak.
- Sağ alt köşede top-most, kapatma butonu olmayan bilgilendirme modalı göstermek.
- "Siparişleri Görüntüle" butonuyla WebUI Orders ekranını açmak.

### `CafeOrders.SetupWizard`

Server kurulumunu otomatikleştiren WPF yardımcı uygulamasıdır.

Sorumluluklar:

- GitHub `Production` branch ZIP paketini indirmek veya local ZIP/klasör paketi kullanmak.
- Server IP, SQL instance, SQL kullanıcı/şifre, IIS root ve port bilgilerini kullanıcıdan almak.
- Kurulum bilgilerini geçici JSON config olarak `Install-CafeOrders.ps1` scriptine aktarmak.
- Kurulum logunu UI üzerinde canlı göstermek.
- Yönetici yetkisi yoksa kullanıcıyı uyarmak.

## 4. Veri Akışları

### Cihaz Akışı

1. DesktopApp cihaz bilgilerini API'ye gönderir.
2. API cihazı kaydeder veya mevcut cihazı günceller.
3. Admin onay verirse cihaz masaya bağlanır.
4. `DeviceApproved`, `DeviceMapped`, `DevicesUpdated` eventleri yayınlanır.
5. DesktopApp menü ekranına geçer ve heartbeat göndermeye devam eder.

### Sipariş Akışı

1. DesktopApp siparişi API'ye yollar.
2. API siparişi `Pending` olarak kaydeder.
3. `OrderCreated` admin grubuna yayınlanır.
4. WebUI ve ServerNotifier bekleyen sipariş görünümünü günceller.
5. AdminAudioAgent/WebUI ses playback sahipliği akışına girer.
6. Admin siparişi kabul, red veya tamamlandı durumuna geçirir.
7. İlgili kiosk cihazına hedefli event gönderilir.

### Katalog Akışı

1. WebUI ürün/kategori değişikliğini API'ye gönderir.
2. API servis katmanı veriyi kaydeder.
3. `CatalogUpdated` event'i tüm clientlara yayınlanır.
4. WebUI ve DesktopApp katalog verisini yeniden yükler.

### Ayar ve Duyuru Akışı

1. Admin ayarları veya aktif bilgi mesajını günceller.
2. API değişikliği kaydeder.
3. `AppSettingsUpdated` veya `InfoMessageUpdated` event'i yayınlanır.
4. DesktopApp banner, renk, ikon, footer, minimum tutar ve ses ayarlarını günceller.

### Log Akışı

1. API, WebUI, DesktopApp, AdminAudioAgent ve ServerNotifier olayları loglar.
2. Yerel log dosyası uygulama klasörüne yazılır.
3. Uygun durumlarda API'ye log kaydı gönderilir.
4. API logları `ApplicationLogEntries` tablosuna yazar.
5. WebUI Logs ekranı bu tabloyu realtime ve filtreli şekilde gösterir.

## 5. Realtime Mimarisi

Hub route:

```text
/hubs/cafe
```

Gruplar:

| Grup | Amaç |
| --- | --- |
| `admin` | WebUI, AdminAudioAgent ve ServerNotifier gibi yönetim tüketicileri |
| `device.{DeviceKey}` | Belirli DesktopApp cihazına hedefli mesaj |

Öne çıkan eventler:

- `DeviceApproved`
- `DeviceRejected`
- `DeviceMapped`
- `DevicesUpdated`
- `OrderCreated`
- `OrderAccepted`
- `OrderRejected`
- `OrderCompleted`
- `OrderSoundPlaybackStarted`
- `OrderSoundPlaybackFailed`
- `OrderSoundAcknowledged`
- `CatalogUpdated`
- `TablesUpdated`
- `AppSettingsUpdated`
- `InfoMessageUpdated`
- `ApplicationLogCreated`

## 6. Konfigürasyon

### API

Dosya:

```text
src/CafeOrders.API/appsettings.json
```

Önemli alanlar:

- `Urls`
- `ConnectionStrings:CafeOrders`
- `Jwt`
- `Branding`
- `Logging:FilePath`
- `Logging:Centralized:Enabled`

### WebUI

Dosya:

```text
src/CafeOrders.WebUI/appsettings.json
```

Önemli alanlar:

- `Urls`
- `ConnectionStrings:CafeOrders`
- `ApiBaseUrl`
- `SessionSettings:AdminCookieDays`
- `SessionSettings:DataProtectionKeysPath`
- `Branding`
- `Logging`

### DesktopApp

Dosya:

```text
src/CafeOrders.DesktopApp/appsettings.json
```

Önemli alanlar:

- `Endpoints:ApiBaseUrl`
- `Endpoints:HubUrl`
- `Media:SharedWebRootPath`
- `Session:AutoCloseAfterSeconds`
- `Startup:RetryCount`
- `Startup:RetryDelaySeconds`

### AdminAudioAgent

Dosya:

```text
src/CafeOrders.AdminAudioAgent/appsettings.json
```

Önemli alanlar:

- `Agent:ApiBaseUrl`
- `Agent:HubUrl`
- `Agent:WebUiBaseUrl`
- `Agent:SharedWebRootPath`
- `Agent:FallbackSoundPath`
- `Agent:CacheDirectory`
- `Agent:LogPath`
- `Agent:FallbackDelayMilliseconds`
- `Agent:PollIntervalMilliseconds`
- `Agent:ApiStartupRetryCount`
- `Agent:ApiStartupRetryDelayMilliseconds`
- `Agent:MaxPlaybackSeconds`
- `Agent:Volume`
- `Agent:UseSystemBeepFallback`

### ServerNotifier

Dosya:

```text
src/CafeOrders.ServerNotifier/appsettings.json
```

Önemli alanlar:

- `Notifier:ApiBaseUrl`
- `Notifier:HubUrl`
- `Notifier:OrdersUrl`
- `Notifier:PollIntervalSeconds`
- `Notifier:StartupRetryCount`
- `Notifier:StartupRetryDelaySeconds`
- `Notifier:LogPath`

## 7. Veritabanı

Veritabanı:

- SQL Server
- EF Core SQL Server provider

Otomatik işlemler:

- API açılışında migration uygulanır.
- WebUI açılışında migration uygulanır.
- Seed işlemi temel kategori, ürün, masa, admin, app settings ve bilgi mesajı verilerini oluşturur.

Önemli tablolar:

- `AdminUsers`
- `AppSettings`
- `ApplicationLogEntries`
- `Categories`
- `Devices`
- `InfoMessages`
- `OrderLines`
- `Orders`
- `Products`
- `Tables`

## 8. Medya Yönetimi

Upload klasörleri:

```text
wwwroot/uploads/products
wwwroot/uploads/sounds
```

Kurallar:

- Ürün görselleri `JPG`, `PNG`, `WEBP`, `GIF` formatlarını destekler.
- Ses dosyaları `MP3`, `WAV`, `OGG`, `M4A`, `AAC`, `FLAC`, `WEBM` formatlarını destekler.
- Dosyalar GUID tabanlı güvenli isimle saklanır.
- Publish sırasında `wwwroot/uploads` klasörü deploy artifact içine dahil edilmez.
- Production update yapılırken mevcut `uploads` klasörü korunmalıdır.

DesktopApp görsel çözümleme sırası:

- HTTP URL.
- SharedWebRootPath üzerinden fiziksel dosya.
- Placeholder/premium kart fallback.

AdminAudioAgent ses çözümleme sırası:

- Sipariş sesi URL'sinden gelen dosya adı.
- `SharedWebRootPath/uploads/sounds`.
- Agent cache klasörü.
- `FallbackSoundPath`.
- `UseSystemBeepFallback=true` ise sistem beep fallback.

## 9. Logging

Yerel loglar:

- API: uygulama klasörü içinde `CafeOrders.API.log`
- WebUI: uygulama klasörü içinde `CafeOrders.WebUI.log`
- DesktopApp: uygulama klasörü içinde DesktopApp log dosyası
- AdminAudioAgent: agent klasörü içinde `AdminAudioAgent.log`
- ServerNotifier: notifier klasörü içinde `ServerNotifier.log`

Merkezi log:

- `ApplicationLogEntries` tablosu.
- `ApplicationLogCreated` event'i ile WebUI log ekranına realtime düşer.

Log alanları:

- kaynak
- seviye
- mesaj
- exception
- kategori
- makine adı
- device key
- masa
- sipariş

## 10. Test ve Kalite

Test projesi:

```text
tests/CafeOrders.Tests
```

Kapsanan ana konular:

- sipariş toplam hesaplama
- minimum sipariş tutarı
- sipariş ses playback takibi
- upload validation
- realtime regression kontrolleri
- ServerNotifier konfigürasyonu
- AdminAudioAgent playback/cache davranışları
- cihaz servis realtime davranışları
- merkezi log servisi

Temel komutlar:

```powershell
dotnet build CafeOrders.slnx -c Release
dotnet test tests\CafeOrders.Tests\CafeOrders.Tests.csproj -c Release
```

## 11. Branch ve Artifact Modeli

| Branch | İçerik |
| --- | --- |
| `master` | Kaynak kod, testler, dokümanlar, DEV appsettings ve geliştirme için son stabil kod |
| `Test` | DEV ortamına göre üretilmiş `publishes/` ve `scripts/` |
| `Production` | Production ortamına göre üretilmiş `publishes/` ve `scripts/` |

Bu ayrım sayesinde production branch'i kaynak kod taşımaz; deploy edilecek artifact ve script seti net kalır.
