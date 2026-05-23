# Teknik Mimari

## 1. Genel Bakis

CafeOrders, bir LAN cafe / gaming center siparis yonetim sistemi olarak tasarlanmis cok katmanli bir .NET 8 cozumudur.

Ana bilesenler:

- `CafeOrders.WebUI`: yonetim paneli
- `CafeOrders.API`: istemci ve yonetim entegrasyon API'si
- `CafeOrders.DesktopApp`: kiosk / masa istemcisi
- `CafeOrders.Infrastructure`: veri erisim, guvenlik ve SignalR
- `CafeOrders.Application`: servis ve contract arabirimleri
- `CafeOrders.Domain`: entity ve is kurallari

## 2. Klasor Mimarisi

Kok dizin:

```text
CafeOrders/
|-- docs/
|-- publishes/
|-- artifacts/
|-- src/
|   |-- CafeOrders.API/
|   |-- CafeOrders.Application/
|   |-- CafeOrders.DesktopApp/
|   |-- CafeOrders.Domain/
|   |-- CafeOrders.Infrastructure/
|   |-- CafeOrders.WebUI/
|-- tests/
|   |-- CafeOrders.Tests/
```

Kaynak katmanlar:

### `src/CafeOrders.Domain`

Domain entity ve enum tanimlari burada yer alir.

Temel entity dosyalari:

- `AdminUser`
- `AppSetting`
- `CafeTable`
- `Category`
- `Device`
- `InfoMessage`
- `Order`
- `OrderLine`
- `Product`

### `src/CafeOrders.Application`

Uygulama servis contract'lari ve DTO tanimlari burada yer alir.

Temel arabirimler:

- `IAdminAuthService`
- `ICatalogService`
- `IDeviceService`
- `IOrderService`
- `ISettingsService`
- `ITableService`
- `IDashboardService`
- `IRealtimeNotifier`

Bu katman, UI veya persistence detaylarini bilmez.

### `src/CafeOrders.Infrastructure`

Teknik implementasyon katmanidir.

Ana alt klasorler:

- `Persistence/`: `CafeOrdersDbContext`, migration'lar, seed
- `Services/`: katalog, cihaz, siparis, ayarlar, dashboard ve table servisleri
- `Realtime/`: `CafeHub`, `SignalRRealtimeNotifier`
- `Security/`: admin auth ve JWT servisi
- `Options/`: konfigurasyon modelleri

`DependencyInjection.cs` icinde:

- SQL Server `DbContext`
- servis implementasyonlari
- JWT auth
- SignalR
- `DevicePresenceMonitorService`

kayitlari yapilir.

### `src/CafeOrders.API`

REST API ve realtime hub host katmanidir.

Controller seti:

- `CatalogController`
- `DashboardController`
- `DevicesController`
- `OrdersController`
- `SettingsController`
- `TablesController`

Baslangicta:

- infrastructure baglanir
- migration otomatik calistirilir
- seed uygulanir
- `CafeHub` `/hubs/cafe` altinda acilir

### `src/CafeOrders.WebUI`

MVC tabanli admin panelidir.

Ana alanlar:

- cihaz onay / masa esleme
- siparis yonetimi
- urun / kategori yonetimi
- ayarlar / duyuru yonetimi
- realtime dashboard

Alt yapida:

- cookie auth
- MVC view yapisi
- API ile HTTP uzerinden yazma islemleri
- SignalR ile admin tarafi canli guncelleme

### `src/CafeOrders.DesktopApp`

WPF kiosk istemcisidir.

Ana alt klasorler:

- `Assets/`: ikon vb.
- `Models/`: UI modelleri
- `Services/`: API, cihaz kimligi ve SignalR client
- `ViewModels/`: ekran davranisi

`MainViewModel`, kiosk akisinin ana orkestra noktasidir.

## 3. Veri ve Islem Akisi

### Siparis akisi

1. Desktop istemci cihaz olarak kayit olur
2. Admin cihazı onaylar ve masaya baglar
3. Desktop katalogu ceker
4. Kullanici sepete urun ekler ve siparis gonderir
5. API siparisi kaydeder
6. WebUI yeni siparisi realtime gorur
7. Admin siparisi onaylar / reddeder / tamamlar
8. Durum SignalR ile Desktop istemciye iletilir

### Katalog akisi

1. WebUI uzerinden urun veya kategori guncellenir
2. Yazma islemi API uzerinden gerceklesir
3. Infrastructure `NotifyCatalogUpdatedAsync()` yayini yapar
4. DesktopApp ve WebUI gerekli veriyi yeniden yukler

### Duyuru / ayar akisi

1. Admin ayarlari veya aktif bilgi mesajini gunceller
2. API / infrastructure tarafinda ayarlar kaydedilir
3. SignalR uzerinden istemcilere yeni sunum bilgisi yayilir
4. Desktop istemci bilgi kutusunu canli gunceller

## 4. Realtime Mimarisi

Tek hub:

- `CafeHub`

Temel event gruplari:

- cihaz onayi / masa atamasi
- katalog guncellemesi
- table / device snapshot guncellemesi
- siparis kabul / red / tamamlandi
- ayar ve bilgi mesaji guncellemesi

Amaç:

- admin paneli ve kiosk istemcinin ayni veri durumunu goruntulemesi
- polling ihtiyacini azaltmak
- kritik senaryolarda kullaniciya anlik geri bildirim sunmak

Not:

- bazi akislarda guvenlik ve dayaniklilik icin fallback polling de bulunur
- cihaz presence icin hosted service ile offline kontrolu yapilir

## 5. Konfigurasyon

### API

Dosya:

- `src/CafeOrders.API/appsettings.json`

Temel alanlar:

- `Urls`
- `ConnectionStrings:CafeOrders`
- `Jwt`
- `Branding`

Mevcut port:

- API: `5001`

### WebUI

Dosya:

- `src/CafeOrders.WebUI/appsettings.json`

Temel alanlar:

- `Urls`
- `ConnectionStrings:CafeOrders`
- `Jwt`
- `ApiBaseUrl`
- `Branding`

Mevcut port:

- WebUI: `5002`

### DesktopApp

Dosya:

- `src/CafeOrders.DesktopApp/appsettings.json`

Temel alanlar:

- `Endpoints:ApiBaseUrl`
- `Endpoints:HubUrl`
- `Media:SharedWebRootPath`

`SharedWebRootPath`, istemcinin paylasimli `wwwroot` klasorunden urun gorsellerini okuyabilmesi icin kullanilir.

Ornek:

```json
{
  "Endpoints": {
    "ApiBaseUrl": "http://192.168.2.11:5001/",
    "HubUrl": "http://192.168.2.11:5001/hubs/cafe"
  },
  "Media": {
    "SharedWebRootPath": "\\\\192.168.2.11\\inetpub\\wwwroot\\WebUI\\wwwroot"
  }
}
```

## 6. Veritabani ve Migration

Veritabani:

- SQL Server
- EF Core SQL Server provider

Migration davranisi:

- API acilisinda `Database.MigrateAsync()`
- WebUI acilisinda `Database.MigrateAsync()`
- ardindan `DbSeeder.SeedAsync(...)`

Bu sayede kurulum makinesinde Visual Studio olmadan da DB guncellenebilir.

## 7. Dosya ve Medya Depolama

WebUI static upload klasorleri:

- `src/CafeOrders.WebUI/wwwroot/uploads/products`
- `src/CafeOrders.WebUI/wwwroot/uploads/sounds`

Uretimde bunlar publish klasoru altinda ilgili `wwwroot/uploads/...` yollarina tasinir.

DesktopApp tarafi:

- gorsel URL'leri realtime geldikten sonra paylasimli `wwwroot` yoluna map edebilir
- dosya adlari encode edilse bile istemci tarafinda decode edilerek fiziksel dosyaya ulasilir

## 8. Testler

Test projesi:

- `tests/CafeOrders.Tests`

Mevcut ornek test kapsami:

- `Order.RecalculateTotal()`
- `InfoMessage.IsCurrentlyActive(...)`

Not:

- test kapsamı su an sinirli
- servis ve realtime akislari icin ek entegrasyon testleri faydali olur

## 9. Deployment Notlari

### Sunucu

- IIS
- SQL Server
- .NET 8 Hosting Bundle
- WebSocket ozelligi

### Client

- DesktopApp self-contained publish onerilir
- `appsettings.json` icinde API / Hub / Media ayarlari dagitim ortamina gore guncellenmelidir

### Publish klasorleri

- `publishes/API`
- `publishes/WebUI`
- `publishes/DesktopApp`

## 10. Bakim Onerileri

- `bin/`, `obj/` ve `artifacts/` dokumantasyon ve kod incelemelerinde dikkate alinmamali
- `appsettings.Production.json` kullanimi standartlastirilabilir
- SignalR event listesi ayri bir referans dokumani olarak genisletilebilir
- test kapsamı cihaz onayi, siparis akisi ve katalog refresh senaryolari icin buyutulebilir
