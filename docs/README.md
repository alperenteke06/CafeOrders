# CafeOrders Dokümantasyonu

Bu klasör, CafeOrders çözümünün teknik mimarisini, işlevsel akışlarını, API yüzeyini, realtime event sözlüğünü ve deployment adımlarını merkezi olarak açıklar.

CafeOrders; internet kafe, e-spor arena ve LAN oyun salonları için geliştirilmiş masa/kiosk sipariş yönetim sistemidir. Sistem; WPF masa istemcileri, ASP.NET Core API, MVC tabanlı WebUI, SignalR realtime altyapısı, SQL Server veritabanı, AdminAudioAgent ve ServerNotifier bileşenlerinden oluşur.

## Dokümanlar

| Dosya | İçerik |
| --- | --- |
| `technical-architecture.md` | Katmanlar, servisler, veri akışı, logging, medya ve konfigürasyon mimarisi |
| `functional-overview.md` | Kullanıcı rolleri, sipariş akışı, cihaz yönetimi, katalog, duyuru ve bildirim davranışları |
| `api-reference.md` | REST endpointleri, MVC route'ları, upload ve ses playback API'leri |
| `realtime-events.md` | SignalR hub metodları, eventler, hedef gruplar ve tüketici davranışları |
| `deployment-guide.md` | DEV/Production ayarları, publish, IIS, SQL, WatchDog, upload koruma ve operasyon kontrol listesi |

## Çözüm Bileşenleri

| Proje | Rol |
| --- | --- |
| `CafeOrders.API` | REST API, SignalR hub, migration/seed ve istemci entegrasyon katmanı |
| `CafeOrders.WebUI` | Admin paneli, ürün/kategori/masa/cihaz/sipariş/ayar/log yönetimi |
| `CafeOrders.DesktopApp` | Client makinelerde çalışan WPF kiosk sipariş ekranı |
| `CafeOrders.AdminAudioAgent` | Server PC üzerinde yeni sipariş sesini garantiye alan native ses ajanı |
| `CafeOrders.ServerNotifier` | Server PC sağ alt köşede bekleyen sipariş bildirimi gösteren WPF notifier |
| `CafeOrders.SetupWizard` | GitHub Production paketinden otomatik server kurulumu yapan WPF setup aracı |
| `CafeOrders.Application` | DTO, servis arayüzleri ve uygulama kontratları |
| `CafeOrders.Domain` | Entity ve enum tanımları |
| `CafeOrders.Infrastructure` | EF Core, servis implementasyonları, SignalR notifier, logging ve hosted servisler |
| `CafeOrders.Tests` | Birim/regresyon testleri |

## Branch Rolleri

| Branch | Amaç |
| --- | --- |
| `master` | Kaynak kod, dokümantasyon, testler, geliştirme ayarları ve son stabil geliştirme hali |
| `Test` | DEV ortamına göre hazırlanmış `publishes/` ve `scripts/` çıktıları |
| `Production` | Production ortamına göre hazırlanmış `publishes/` ve `scripts/` çıktıları, kaynak kod içermez |

## Varsayılan DEV Ortamı

| Alan | Değer |
| --- | --- |
| API | `http://192.168.11.24:5001` |
| WebUI | `http://192.168.11.24:5002` |
| SQL | `Server=.\SQLEXPRESS;Database=CafeOrders;User Id=sa;Password=sa@Alperen123!` |
| Shared Web Root | `\\192.168.11.24\inetpub\wwwroot\WebUI\wwwroot` |

## Varsayılan Production Ortamı

| Alan | Değer |
| --- | --- |
| API | `http://192.168.2.11:5001` |
| WebUI | `http://192.168.2.11:5002` |
| SQL | `Server=DESKTOP-ET476QO\SQLEXPRESS01;Database=CafeOrders;User Id=sa;Password=JetNet@Admin120526!` |
| Shared Web Root | `\\192.168.2.11\inetpub\wwwroot\WebUI\wwwroot` |

## Operasyon Notları

- API ve WebUI ilk açılışta EF Core migration çalıştırır ve seed verilerini uygular.
- `wwwroot/uploads/products` ve `wwwroot/uploads/sounds` kullanıcı verisidir; deployment sırasında silinmemelidir.
- WebUI yeni sipariş sesini çalarsa sesi kendisi işaretler; çalamazsa AdminAudioAgent sesin sahibi olur ve order playback bilgisini API'ye bildirir.
- WatchDog scripti API/WebUI IIS AppPool ve Site durumlarını, WebUI health check sonucunu, AdminAudioAgent ve ServerNotifier process durumunu izler.
- Setup Wizard, IIS/WebSocket/Hosting Bundle ön kontrolü yapar; API/WebUI kurulumunu, C:\AdminAudioAgent, C:\ServerNotifier, C:\Scripts kurulumunu, firewall rule ve WatchDog task kaydını tek akışta yönetir.
- DesktopApp ürün görsellerini HTTP veya `Media:SharedWebRootPath` üzerinden okuyabilir.
- Loglar hem dosyaya hem de `ApplicationLogEntries` tablosuna yazılarak WebUI Sistem Logları ekranından izlenebilir.

## Hızlı Kontrol Komutları

```powershell
dotnet build CafeOrders.slnx -c Release
dotnet test tests\CafeOrders.Tests\CafeOrders.Tests.csproj -c Release
```

Publish ve deployment detayları için `deployment-guide.md` dosyasını takip edin.
