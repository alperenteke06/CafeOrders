# Deployment Rehberi

## 1. Hedef Kurulum

Production ortamında önerilen bileşen dağılımı:

| Bileşen | Konum | Port / Yol |
| --- | --- | --- |
| `CafeOrders.API` | IIS + SQL bulunan server | `5001` |
| `CafeOrders.WebUI` | IIS + SQL bulunan server | `5002` |
| `CafeOrders.AdminAudioAgent` | Server PC kullanıcı oturumu | `C:\AdminAudioAgent` |
| `CafeOrders.ServerNotifier` | Server PC kullanıcı oturumu | `C:\ServerNotifier` |
| `CafeOrders.SetupWizard` | Kurulum operatörü makinesi | GitHub Production paketini indirerek kurulum yapar |
| `CafeOrders.DesktopApp` | Client makineler | Örn. `C:\DesktopApp` |
| WatchDog scripts | Server PC | `C:\Scripts` |
| SQL Server | Server PC | `SQLEXPRESS` veya prod instance |

## 2. Branch ve Artifact Kullanımı

| Branch | Kullanım |
| --- | --- |
| `master` | Kaynak kod ve dokümantasyon. Geliştirme ve test buradan yapılır. |
| `Test` | DEV ortam ayarlı `publishes/` ve `scripts/`. Kaynak kod içermez. |
| `Production` | Prod ortam ayarlı `publishes/` ve `scripts/`. Kaynak kod içermez. |

Production kurulumunda `Production` branch içeriği kullanılmalıdır.

## 3. Otomatik Kurulum: CafeOrders Setup Wizard

Önerilen kurulum yöntemi Setup Wizard kullanmaktır.

Varsayılan paket kaynağı:

```text
https://github.com/alperenteke06/CafeOrders/archive/refs/heads/Production.zip
```

Wizard kullanıcıdan şu bilgileri alır:

- Server IP.
- API ve WebUI portları.
- SQL instance adı.
- SQL kullanıcı adı.
- SQL şifresi.
- IIS root path.
- İsteğe bağlı local ZIP paketi.

Wizard arka planda `installer/Install-CafeOrders.ps1` scriptini çalıştırır. SQL şifresi komut satırına yazılmaz; geçici JSON config dosyası ile script'e iletilir ve işlem sonunda silinir.

Kurulum sırasında yapılan işlemler:

- IIS, WebSocket ve Hosting Bundle ön kontrolü.
- Production paketini indirme veya local paketi açma.
- API ve WebUI dosyalarını IIS path altına kurma.
- `WebUI\wwwroot\uploads` klasörünü koruma.
- `C:\AdminAudioAgent`, `C:\ServerNotifier`, `C:\Scripts` klasörlerini hazırlama.
- Ortama göre `appsettings.json` dosyalarını üretme.
- API/WebUI appsettings dosyalarının ACL izinlerini kısıtlama.
- IIS AppPool ve Site oluşturma/güncelleme.
- 5001/5002 firewall inbound rule oluşturma.
- WatchDog task kaydını `RunLevel Highest` ile oluşturma.
- İlk WatchDog tetiklemesini yapma.
- API ve WebUI health check çalıştırma.

## 4. Sunucu Gereksinimleri

Server PC üzerinde:

- Windows Server önerilir. Windows 10 IIS concurrent connection limitleri nedeniyle yüksek client sayısı için uygun değildir.
- IIS.
- IIS WebSocket Protocol özelliği.
- .NET 8 Hosting Bundle.
- SQL Server veya SQL Server Express.
- TCP `5001` ve `5002` firewall izinleri.
- Paylaşımlı medya klasörü gerekiyorsa `wwwroot` share izni.

Client makinelerde:

- DesktopApp self-contained publish kullanılıyorsa .NET Desktop Runtime gerekmez.
- API `5001` ve Hub endpointine ağ erişimi gerekir.
- Görseller shared path üzerinden okunacaksa `\\server\inetpub\wwwroot\WebUI\wwwroot` okuma izni gerekir.

## 5. DEV Ortam Ayarları

| Alan | Değer |
| --- | --- |
| API | `http://192.168.11.24:5001` |
| WebUI | `http://192.168.11.24:5002` |
| SQL | `Server=.\SQLEXPRESS;Database=CafeOrders;User Id=sa;Password=sa@Alperen123!;TrustServerCertificate=True;MultipleActiveResultSets=True` |
| Shared Web Root | `\\192.168.11.24\inetpub\wwwroot\WebUI\wwwroot` |

## 6. Production Ortam Ayarları

| Alan | Değer |
| --- | --- |
| API | `http://192.168.2.11:5001` |
| WebUI | `http://192.168.2.11:5002` |
| SQL | `Server=DESKTOP-ET476QO\SQLEXPRESS01;Database=CafeOrders;User Id=sa;Password=JetNet@Admin120526!;TrustServerCertificate=True;MultipleActiveResultSets=True` |
| Shared Web Root | `\\192.168.2.11\inetpub\wwwroot\WebUI\wwwroot` |
| AdminAudioAgent | `C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe` |
| ServerNotifier | `C:\ServerNotifier\CafeOrders.ServerNotifier.exe` |
| Scripts | `C:\Scripts` |

## 7. Publish Komutları

Ana kaynak branch üzerinde manuel publish almak için:

```powershell
dotnet publish src\CafeOrders.API\CafeOrders.API.csproj -c Release --self-contained true -r win-x64 /p:PublishSingleFile=false /p:DebugType=None /p:DebugSymbols=false -o publishes\API
dotnet publish src\CafeOrders.WebUI\CafeOrders.WebUI.csproj -c Release --self-contained true -r win-x64 /p:PublishSingleFile=false /p:DebugType=None /p:DebugSymbols=false -o publishes\WebUI
dotnet publish src\CafeOrders.DesktopApp\CafeOrders.DesktopApp.csproj -c Release --self-contained true -r win-x64 /p:PublishSingleFile=false /p:DebugType=None /p:DebugSymbols=false -o publishes\DesktopApp
dotnet publish src\CafeOrders.AdminAudioAgent\CafeOrders.AdminAudioAgent.csproj -c Release --self-contained true -r win-x64 /p:PublishSingleFile=false /p:DebugType=None /p:DebugSymbols=false -o publishes\AdminAudioAgent
dotnet publish src\CafeOrders.ServerNotifier\CafeOrders.ServerNotifier.csproj -c Release --self-contained true -r win-x64 /p:PublishSingleFile=false /p:DebugType=None /p:DebugSymbols=false -o publishes\ServerNotifier
```

## 8. IIS Kurulumu

İki ayrı IIS site önerilir:

| Site | Binding |
| --- | --- |
| `CafeOrders.API` | `*:5001` |
| `CafeOrders.WebUI` | `*:5002` |

App Pool:

- `.NET CLR Version`: `No Managed Code`
- Start Mode: `AlwaysRunning` önerilir.
- Identity dosya izinlerine göre ayarlanmalıdır.

Ek ayarlar:

- WebSocket Protocol aktif olmalı.
- API ve WebUI klasörlerine IIS kullanıcısının okuma izni olmalı.
- WebUI `wwwroot/uploads` klasörüne yazma izni olmalı.

## 9. SQL ve Migration

Migration otomatik çalışır:

- API açılışında `Database.MigrateAsync()`.
- WebUI açılışında `Database.MigrateAsync()`.
- Ardından seed kontrolü yapılır.

Kurulum makinesinde Visual Studio gerekmez. Uygulama ilk açıldığında DB yoksa oluşturulur, migration'lar uygulanır.

DB silinmeden update yapılırken:

- Veritabanını silmeyin.
- API/WebUI yeni publish ile açıldığında migration farkı varsa uygulanır.
- Seed işlemi mevcut kayıtları silmez; eksik temel verileri tamamlar.

## 10. Upload Dosyalarını Koruma

Kullanıcı verisi olan klasörler:

```text
WebUI\wwwroot\uploads\products
WebUI\wwwroot\uploads\sounds
```

Güncelleme sırasında:

1. IIS site ve app pool'larını durdurun.
2. Mevcut `wwwroot/uploads` klasörünü silmeyin.
3. Yeni WebUI publish dosyalarını mevcut klasörün üzerine kopyalayın.
4. Eğer komple klasör silinecekse önce `uploads` yedeği alın, sonra geri koyun.
5. IIS site ve app pool'larını başlatın.

Not:

- Güncel publish çıktısı `wwwroot/uploads` klasörünü artifact içine dahil etmez.
- Bu sayede üzerine kopyalama yöntemi görsel ve ses dosyalarını ezmez.

## 11. Appsettings Dosyaları

### API

```json
{
  "Urls": "http://0.0.0.0:5001",
  "ConnectionStrings": {
    "CafeOrders": "Server=DESKTOP-ET476QO\\SQLEXPRESS01;Database=CafeOrders;User Id=sa;Password=JetNet@Admin120526!;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### WebUI

```json
{
  "Urls": "http://0.0.0.0:5002",
  "ApiBaseUrl": "http://192.168.2.11:5001",
  "SessionSettings": {
    "AdminCookieDays": 3650,
    "DataProtectionKeysPath": "C:\\ProgramData\\CafeOrders\\WebUI\\DataProtectionKeys"
  }
}
```

### DesktopApp

```json
{
  "Endpoints": {
    "ApiBaseUrl": "http://192.168.2.11:5001/",
    "HubUrl": "http://192.168.2.11:5001/hubs/cafe"
  },
  "Media": {
    "SharedWebRootPath": "\\\\192.168.2.11\\inetpub\\wwwroot\\WebUI\\wwwroot"
  },
  "Session": {
    "AutoCloseAfterSeconds": 150
  }
}
```

### AdminAudioAgent

```json
{
  "Agent": {
    "ApiBaseUrl": "http://192.168.2.11:5001/",
    "HubUrl": "http://192.168.2.11:5001/hubs/cafe",
    "WebUiBaseUrl": "http://192.168.2.11:5002/",
    "SharedWebRootPath": "\\\\192.168.2.11\\inetpub\\wwwroot\\WebUI\\wwwroot",
    "CacheDirectory": "cache",
    "LogPath": "AdminAudioAgent.log",
    "FallbackDelayMilliseconds": 0,
    "PollIntervalMilliseconds": 2000,
    "ApiStartupRetryCount": 180,
    "ApiStartupRetryDelayMilliseconds": 2000,
    "MaxPlaybackSeconds": 12,
    "Volume": 90,
    "UseSystemBeepFallback": false
  }
}
```

### ServerNotifier

```json
{
  "Notifier": {
    "ApiBaseUrl": "http://192.168.2.11:5001/",
    "HubUrl": "http://192.168.2.11:5001/hubs/cafe",
    "OrdersUrl": "http://192.168.2.11:5002/?section=orders",
    "PollIntervalSeconds": 5,
    "StartupRetryCount": 90,
    "StartupRetryDelaySeconds": 2,
    "LogPath": "ServerNotifier.log"
  }
}
```

## 12. WatchDog Script

Script klasörü:

```text
C:\Scripts
```

Dosyalar:

- `CafeOrders.WatchDog.ps1`
- `Register-CafeOrders.WatchDogTask.ps1`
- `Run-CafeOrders.WatchDogHidden.vbs`
- `CafeOrders.WatchDog.README.md`

WatchDog sorumlulukları:

- API AppPool durumunu kontrol etmek ve durmuşsa başlatmak.
- WebUI AppPool durumunu kontrol etmek ve durmuşsa başlatmak.
- API Site durumunu kontrol etmek ve durmuşsa başlatmak.
- WebUI Site durumunu kontrol etmek ve durmuşsa başlatmak.
- API health check yapmak.
- WebUI health check yapmak.
- WebUI açık değilse default browser ile açmak.
- AdminAudioAgent çalışmıyorsa başlatmak.
- ServerNotifier çalışmıyorsa başlatmak.

Kurulum:

```powershell
PowerShell'i yönetici olarak açın.
cd C:\Scripts
.\Register-CafeOrders.WatchDogTask.ps1
```

Önemli:

- IIS yönetimi için task `RunLevel Highest` ile oluşturulur.
- PowerShell penceresi görünmemesi için task action `wscript.exe` üzerinden VBS runner kullanır.
- Manuel dry-run admin yetkisi olmadan WebAdministration modülünde hata verebilir; bu normaldir.

## 13. AdminAudioAgent Operasyonu

Önerilen klasör:

```text
C:\AdminAudioAgent
```

Ses çalışma prensibi:

- Hub üzerinden yeni sipariş event'i alır.
- API hazır değilse retry ile bekler.
- Polling ile çalınmamış siparişleri kontrol eder.
- Ses dosyasını shared web root veya cache üzerinden çözer.
- Sistem sesini kontrol eder, gerekirse açar/yükseltir.
- Ses tamamlanınca API'ye `sound-played` bildirir.

Log dosyası:

```text
C:\AdminAudioAgent\AdminAudioAgent.log
```

Loglarda şu bilgiler izlenir:

- Sipariş ID.
- Sesi kimin çaldığı.
- Playback başladı/tamamlandı/failed.
- Sistem sesi mute mıydı.
- Hedef volume değeri.
- Kullanılan ses dosyası yolu.

## 14. ServerNotifier Operasyonu

Önerilen klasör:

```text
C:\ServerNotifier
```

ServerNotifier:

- Server PC'de taskbar üstü sağ alt modal gösterir.
- Bekleyen siparişleri realtime izler.
- WebUI Orders sayfasını açan buton içerir.
- Bekleyen sipariş kalmadığında otomatik kapanır.

Log dosyası:

```text
C:\ServerNotifier\ServerNotifier.log
```

## 15. Güncelleme Sırası

Production güncelleme önerisi:

1. IIS üzerinde API ve WebUI site/app pool'larını durdurun.
2. WatchDog task'ını geçici olarak devre dışı bırakın veya durdurun.
3. `C:\AdminAudioAgent` ve `C:\ServerNotifier` uygulamalarını kapatın.
4. WebUI `wwwroot/uploads` klasörünü koruyun.
5. Yeni `publishes/API` içeriğini API klasörüne kopyalayın.
6. Yeni `publishes/WebUI` içeriğini WebUI klasörüne kopyalayın.
7. Yeni `publishes\AdminAudioAgent` içeriğini `C:\AdminAudioAgent` klasörüne kopyalayın.
8. Yeni `publishes\ServerNotifier` içeriğini `C:\ServerNotifier` klasörüne kopyalayın.
9. `scripts` içeriğini `C:\Scripts` klasörüne kopyalayın.
10. Appsettings değerlerini ortamla karşılaştırın.
11. IIS site/app pool'larını başlatın.
12. WatchDog task'ını çalıştırın.
13. WebUI login, yeni sipariş, ses, notifier ve DesktopApp cihaz akışını test edin.

## 16. Smoke Test Listesi

Sunucu:

- `http://server:5001/api/v1/settings/app` cevap veriyor mu?
- `http://server:5002` login ekranı geliyor mu?
- SQL migration uygulanmış mı?
- WebSocket bağlantısı kuruluyor mu?
- WebUI `uploads` klasörüne dosya yazabiliyor mu?

Admin:

- Login kalıcı mı?
- Ürün görsel upload çalışıyor mu?
- Ses upload çalışıyor mu?
- Cihaz onaylanınca DesktopApp realtime açılıyor mu?
- Sipariş onay/red DesktopApp'e düşüyor mu?
- Log ekranında API/WebUI/DesktopApp/AdminAudioAgent/ServerNotifier kayıtları görünüyor mu?

Ses:

- WebUI odakta değilken AdminAudioAgent sesi çalıyor mu?
- `Orders.IsSoundPlayed` true oluyor mu?
- Aynı sipariş için ses tekrar çalmıyor mu?

Notifier:

- Bekleyen sipariş gelince sağ alt modal açılıyor mu?
- Bekleyen sipariş kapanınca modal gidiyor mu?
- Siparişleri Görüntüle butonu WebUI Orders ekranını açıyor mu?

Client:

- DesktopApp cihaz kayıt isteği gönderiyor mu?
- Cihaz onay bekleme ekranı doğru mu?
- Masa ataması realtime geliyor mu?
- Ürün görselleri yükleniyor mu?
- Minimum sepet tutarı çalışıyor mu?

## 17. Sık Karşılaşılan Sorunlar

### API çalışıyor ama Swagger görünmüyor

Swagger yalnızca development ortamında açık olabilir. Production'da health için `/api/v1/settings/app` gibi endpointler kullanılmalıdır.

### WebUI login her açılışta tekrar istiyor

Kontrol edin:

- `SessionSettings:AdminCookieDays`
- `SessionSettings:DataProtectionKeysPath`
- IIS recycle sonrası DataProtection key dosyalarının silinmediği

### DesktopApp kilit ekranından çıkmıyor

Kontrol edin:

- API erişimi.
- Cihaz kaydı DB'de var mı?
- Cihaz onaylı mı?
- DeviceKey değişmiş mi?
- Hub bağlantısı kuruluyor mu?
- DesktopApp log dosyası.

### Görseller DesktopApp'te gelmiyor

Kontrol edin:

- WebUI `uploads/products` dosyası mevcut mu?
- DesktopApp `SharedWebRootPath` doğru mu?
- UNC klasör okuma izni var mı?
- Dosya adı encode/decode problemi var mı?

### Yeni sipariş sesi çalmıyor

Kontrol edin:

- AdminAudioAgent çalışıyor mu?
- Agent API ve Hub'a bağlandı mı?
- Ses dosyası `uploads/sounds` altında var mı?
- Sistem sesi mute mı?
- `Orders.IsSoundPlayed` yanlışlıkla true olmuş mu?
- Agent logunda playback owner ve hata var mı?
