# Deployment Rehberi

## 1. Hedef Mimari

Onerilen kurulum:

- `CafeOrders.API` -> sunucu uzerinde `5001`
- `CafeOrders.WebUI` -> sunucu uzerinde `5002`
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
- DesktopApp icin self-contained publish onerilir

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

Ornek publish komutlari:

### API

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.API\CafeOrders.API.csproj -c Release --self-contained false /p:UseSharedCompilation=false -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\API
```

### WebUI

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.WebUI\CafeOrders.WebUI.csproj -c Release --self-contained false /p:UseSharedCompilation=false -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\WebUI
```

### DesktopApp

```powershell
dotnet publish C:\AllActivities\SoftwareDev\CafeOrders\src\CafeOrders.DesktopApp\CafeOrders.DesktopApp.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false /p:IncludeNativeLibrariesForSelfExtract=true -o C:\AllActivities\SoftwareDev\CafeOrders\publishes\DesktopApp
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
    "CafeOrders": "Server=SUNUCU\\SQLEXPRESS;Database=CafeOrders;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### WebUI

```json
{
  "Urls": "http://0.0.0.0:5002",
  "ApiBaseUrl": "http://localhost:5001"
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
5. `appsettings.json` dosyalarini production degerleriyle guncelle
6. paylasimli medya klasoru gerekiyorsa `wwwroot` paylasimini ac
7. API ve WebUI'yi baslat
8. DesktopApp `appsettings.json` icinde sunucu adreslerini guncelle
9. DesktopApp'i client makinelere dagit

## 11. Operasyonel Kontrol Listesi

- API `5001` cevap veriyor mu
- WebUI `5002` cevap veriyor mu
- DB migration otomatik uygulandi mi
- admin login calisiyor mu
- yeni cihaz kaydi gorunuyor mu
- cihaz onayi DesktopApp'e dusuyor mu
- urun gorseli degisikligi realtime yenileniyor mu
- media share erisimi client makinede calisiyor mu
