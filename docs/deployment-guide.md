# Deployment Rehberi

## 1. Hedef Mimari

Onerilen kurulum:

- `CafeOrders.API` -> sunucu uzerinde `5001`
- `CafeOrders.WebUI` -> sunucu uzerinde `5002`
- `CafeOrders.AdminAudioAgent` -> sunucu PC uzerinde yeni siparis sesi icin native fallback ajan
- `CafeOrders.DesktopApp` -> client makinelere dagitilan WPF istemci
- SQL Server -> ayni sunucu veya ayri veritabani sunucusu

## 2. Sunucu Gereksinimleri

IIS / SQL sunucusunda:

- Windows Server veya Windows
- IIS
- WebSocket ozelligi
- .NET 8 Hosting Bundle
- SQL Server

Not:

- API ve WebUI framework-dependent publish ile calistirilabilir
- DesktopApp ve AdminAudioAgent icin self-contained publish onerilir

## 3. Client Gereksinimleri

DesktopApp self-contained publish kullaniliyorsa:

- ek .NET Desktop Runtime zorunlu degildir

Ek gereksinimler:

- API sunucusuna ag erisimi
- gerekirse paylasimli `wwwroot` klasorune okuma erisimi
- `appsettings.json` icinde API / Hub / medya yolu dogru tanimlanmalidir

## 4. Publish Ciktilari

Onerilen publish hedefleri:

- `publishes/API`
- `publishes/WebUI`
- `publishes/DesktopApp`
- `publishes/AdminAudioAgent`

Ornek publish komutlari:

### API

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.API\CafeOrders.API.csproj -c Release -r win-x64 --self-contained true -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\API
```

### WebUI

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.WebUI\CafeOrders.WebUI.csproj -c Release -r win-x64 --self-contained true -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\WebUI
```

### DesktopApp

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.DesktopApp\CafeOrders.DesktopApp.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false /p:IncludeNativeLibrariesForSelfExtract=true -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\DesktopApp
```

### AdminAudioAgent

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.AdminAudioAgent\CafeOrders.AdminAudioAgent.csproj -c Release -r win-x64 --self-contained true -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\AdminAudioAgent
```

## 5. Konfigurasyon Dosyalari

### API

Dosya:

- `src/CafeOrders.API/appsettings.json`

Temel alanlar:

- `Urls`
- `ConnectionStrings:CafeOrders`
- `Jwt`
- `Branding`

### WebUI

Dosya:

- `src/CafeOrders.WebUI/appsettings.json`

Temel alanlar:

- `Urls`
- `ConnectionStrings:CafeOrders`
- `ApiBaseUrl`
- `Jwt`
- `Branding`

### DesktopApp

Dosya:

- `src/CafeOrders.DesktopApp/appsettings.json`

Temel alanlar:

- `Endpoints:ApiBaseUrl`
- `Endpoints:HubUrl`
- `Media:SharedWebRootPath`

## 6. Onerilen Production Degerleri

### API

```json
{
  "Urls": "http://0.0.0.0:5001",
  "ConnectionStrings": {
    "CafeOrders": "Server=.\\SQLEXPRESS;Database=CafeOrders;User Id=sa;Password=sa@Alperen123!;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### AdminAudioAgent

Dosya:

- `src/CafeOrders.AdminAudioAgent/appsettings.json`

Temel alanlar:

- `Agent:ApiBaseUrl`
- `Agent:HubUrl`
- `Agent:WebUiBaseUrl`
- `Agent:FallbackDelayMilliseconds`
- `Agent:Volume`

WebUI yeni siparis sesini basariyla calarsa Hub uzerinden ses onayi gonderir. Onay gelmezse AdminAudioAgent belirtilen gecikme sonunda sesi Windows uzerinden native olarak calar.

### WebUI

```json
{
  "Urls": "http://0.0.0.0:5002",
  "ApiBaseUrl": "http://192.168.11.24:5001"
}
```

### DesktopApp

```json
{
  "Endpoints": {
    "ApiBaseUrl": "http://192.168.11.24:5001/",
    "HubUrl": "http://192.168.11.24:5001/hubs/cafe"
  },
  "Media": {
    "SharedWebRootPath": "\\\\192.168.11.24\\inetpub\\wwwroot\\WebUI\\wwwroot"
  }
}
```

## 7. IIS Kurulum Onerisi

Iki ayri site onerilir:

- `CafeOrders.API` -> `*:5001`
- `CafeOrders.WebUI` -> `*:5002`

App Pool:

- `No Managed Code`

Ek notlar:

- WebSocket aktif olmali
- publish klasorune yazma/okuma izinleri dogru tanimlanmali
- `wwwroot/uploads/products` ve `wwwroot/uploads/sounds` klasorleri yazilabilir olmali

## 8. SQL ve Migration

Bu projede migration otomatik uygulanir.

Baslangicta:

- API acilisinda `Database.MigrateAsync()`
- WebUI acilisinda `Database.MigrateAsync()`
- sonrasinda `DbSeeder.SeedAsync(...)`

Yani production makinede Visual Studio gerekmeksizin:

1. SQL Server hazirlanir
2. connection string yazilir
3. API veya WebUI bir kez baslatilir
4. veritabani otomatik guncellenir

## 9. Paylasimli Medya Yapisi

WPF istemci gorselleri iki sekilde tuketebilir:

- HTTP URL
- paylasimli UNC klasor yolu

Mevcut yaklasim:

- serverdaki `wwwroot` klasoru paylasilir
- DesktopApp `SharedWebRootPath` ile bu kok klasoru bilir
- `/uploads/products/...` yolunu fiziksel dosyaya map eder

Bu yapi, IIS static file erisim problemlerinde daha stabil davranir.

## 10. Kurulum Sirasi

1. SQL Server kur ve yetkileri hazirla
2. IIS ve Hosting Bundle kur
3. API publish dosyalarini yerlestir
4. WebUI publish dosyalarini yerlestir
5. AdminAudioAgent publish dosyalarini server PC uzerinde uygun bir klasore yerlestir
6. `appsettings.json` dosyalarini production degerleriyle guncelle
7. paylasimli medya klasoru gerekiyorsa `wwwroot` paylasimini ac
8. API ve WebUI'yi baslat
9. AdminAudioAgent'i server PC kullanici oturumunda calistir veya startup/task ile baslat
10. DesktopApp `appsettings.json` icinde sunucu adreslerini guncelle
11. DesktopApp'i client makinelere dagit

## 11. Operasyonel Kontrol Listesi

- API `5001` cevap veriyor mu
- WebUI `5002` cevap veriyor mu
- DB migration otomatik uygulandi mi
- admin login calisiyor mu
- AdminAudioAgent Hub'a baglanip WebUI ack gelmeyen sipariste ses caliyor mu
- yeni cihaz kaydi gorunuyor mu
- cihaz onayi DesktopApp'e dusuyor mu
- urun gorseli degisikligi realtime yenileniyor mu
- media share erisimi client makinede calisiyor mu
