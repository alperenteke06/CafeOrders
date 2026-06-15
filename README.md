# CafeOrders

CafeOrders; internet kafe, e-spor arena ve LAN oyun salonları için geliştirilmiş gerçek zamanlı masa/kiosk sipariş yönetim sistemidir.

Sistem; client makinelerde çalışan WPF kiosk uygulaması, server üzerinde çalışan ASP.NET Core API, MVC WebUI admin paneli, SQL Server veritabanı, yeni sipariş sesi için AdminAudioAgent ve bekleyen sipariş bildirimi için ServerNotifier bileşenlerinden oluşur.

## Öne Çıkan Özellikler

- Fullscreen WPF kiosk sipariş ekranı.
- Cihaz kayıt, onay ve masa eşleme akışı.
- WebUI üzerinden sipariş, ürün, kategori, masa, cihaz, duyuru, ayar ve log yönetimi.
- SignalR ile realtime sipariş, katalog, cihaz, ayar, duyuru ve log güncellemeleri.
- Minimum sepet tutarı kontrolü.
- Ürün görseli ve yeni sipariş sesi upload desteği.
- AdminAudioAgent ile WebUI ses çalamadığında native Windows ses fallback'i.
- ServerNotifier ile server ekranında sağ alt köşe bekleyen sipariş bildirimi.
- CafeOrders Setup Wizard ile GitHub Production paketinden otomatik server kurulumu.
- WatchDog scriptleri ile IIS AppPool/Site, WebUI health, AdminAudioAgent ve ServerNotifier kontrolü.
- Merkezi sistem logları paneli.

## Projeler

| Proje | Açıklama |
| --- | --- |
| `CafeOrders.API` | REST API, SignalR hub, migration ve seed hostu |
| `CafeOrders.WebUI` | Admin yönetim paneli |
| `CafeOrders.DesktopApp` | Client/kiosk WPF uygulaması |
| `CafeOrders.AdminAudioAgent` | Yeni sipariş sesi için server-side native ajan |
| `CafeOrders.ServerNotifier` | Bekleyen siparişler için server-side WPF bildirim uygulaması |
| `CafeOrders.SetupWizard` | IIS/API/WebUI/Agent/Notifier/Scripts kurulumunu yöneten WPF setup aracı |
| `CafeOrders.Application` | DTO ve servis kontratları |
| `CafeOrders.Domain` | Entity ve domain modelleri |
| `CafeOrders.Infrastructure` | EF Core, servisler, SignalR, logging ve güvenlik altyapısı |
| `CafeOrders.Tests` | Test projesi |

## Branch Yapısı

| Branch | İçerik |
| --- | --- |
| `master` | Kaynak kod, testler, dokümantasyon ve DEV ayarları |
| `Test` | DEV ortamına göre üretilmiş `publishes/` ve `scripts/` |
| `Production` | Production ortamına göre üretilmiş `publishes/` ve `scripts/` |

## Varsayılan DEV Ayarları

| Alan | Değer |
| --- | --- |
| API | `http://192.168.11.24:5001` |
| WebUI | `http://192.168.11.24:5002` |
| SQL | `Server=.\SQLEXPRESS;Database=CafeOrders;User Id=sa;Password=sa@Alperen123!` |

## Hızlı Başlangıç

```powershell
dotnet build CafeOrders.slnx -c Release
dotnet test tests\CafeOrders.Tests\CafeOrders.Tests.csproj -c Release
```

Visual Studio üzerinden birden fazla startup project olarak şu projeler başlatılabilir:

- `CafeOrders.API`
- `CafeOrders.WebUI`
- `CafeOrders.DesktopApp`
- `CafeOrders.AdminAudioAgent`
- `CafeOrders.ServerNotifier`

## Dokümantasyon

Detaylı dokümanlar `docs/` klasöründedir:

- `docs/README.md`
- `docs/technical-architecture.md`
- `docs/functional-overview.md`
- `docs/api-reference.md`
- `docs/realtime-events.md`
- `docs/deployment-guide.md`

## Deployment Notu

Production update sırasında `WebUI/wwwroot/uploads` klasörü korunmalıdır. Ürün görselleri ve ses dosyaları bu klasörde tutulur. Publish çıktıları bu klasörü ezmeyecek şekilde hazırlanır.

Otomatik kurulum için `CafeOrders.SetupWizard` kullanılabilir. Wizard varsayılan olarak GitHub `Production` branch ZIP paketini indirir:

```text
https://github.com/alperenteke06/CafeOrders/archive/refs/heads/Production.zip
```
