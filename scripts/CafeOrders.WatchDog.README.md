# CafeOrders WatchDog

## Amac

Bu script, IIS uzerindeki `CafeOrders.API` ve `CafeOrders.WebUI` AppPool/Site durumlarini kontrol eder. Herhangi biri calismiyorsa baslatir. IIS taraflari ayaga kalktiktan sonra WebUI icin HTTP health check yapar. WebUI saglikliysa Chrome/default tarayici uzerinde admin panelinin zaten acik olup olmadigini kontrol eder.

## Dosyalar

- `CafeOrders.WatchDog.ps1`: asil izleme ve kurtarma scripti
- `Register-CafeOrders.WatchDogTask.ps1`: Task Scheduler gorevini olusturan yardimci script

## Onerilen Konum

Production makinede:

```powershell
C:\Scripts\CafeOrders.WatchDog.ps1
C:\Scripts\Register-CafeOrders.WatchDogTask.ps1
```

## Task Scheduler Kurulum

PowerShell'i yonetici olarak acin ve calistirin:

```powershell
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\Register-CafeOrders.WatchDogTask.ps1"
```

IIS AppPool ve Site baslatma islemleri icin task `RunLevel Highest` ile olusturulur. Chrome/default browser acilisi ise varsayilan olarak `explorer.exe` uzerinden yapilir. Bu, URL'nin normal kullanici oturumundaki default browser profilinde acilmasini saglar.

## Manuel Test

```powershell
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\CafeOrders.WatchDog.ps1"
```

## Parametreler

```powershell
-ApiAppPoolName "CafeOrders.API"
-WebUiAppPoolName "CafeOrders.WebUI"
-ApiSiteName "CafeOrders.API"
-WebUiSiteName "CafeOrders.WebUI"
-WebUiUrl "http://192.168.1.104:5002/"
-LogPath "C:\Scripts\CafeOrders.WatchDog.log"
```

## Chrome Davranisi

Script su kontrolleri yapar:

- Chrome process command line icinde `192.168.1.104:5002` var mi
- aktif Chrome pencere basliginda CafeOrders basligi var mi
- Chrome tab basliklari UI Automation ile CafeOrders basligi tasiyor mu

Sayfa zaten aciksa yeni sekme acmaz. Sayfa acik degilse URL'yi Windows shell uzerinden acar.
