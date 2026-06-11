# CafeOrders WatchDog

## Amac

Bu script, IIS uzerindeki `CafeOrders.API` ve `CafeOrders.WebUI` AppPool/Site durumlarini kontrol eder. Herhangi biri calismiyorsa baslatir. IIS taraflari ayaga kalktiktan sonra once API, sonra WebUI icin HTTP health check yapar. API ve WebUI saglikliysa `CafeOrders.AdminAudioAgent` ve `CafeOrders.ServerNotifier` calisiyor mu kontrol eder ve calismiyorlarsa baslatir. Son olarak Chrome/default tarayici uzerinde admin panelinin zaten acik olup olmadigini kontrol eder.

## Dosyalar

- `CafeOrders.WatchDog.ps1`: asil izleme ve kurtarma scripti
- `Register-CafeOrders.WatchDogTask.ps1`: Task Scheduler gorevini olusturan yardimci script
- `Run-CafeOrders.WatchDogHidden.vbs`: PowerShell penceresinin ekrana gelmesini engelleyen gizli calistirici

## Onerilen Konum

Production makinede:

```powershell
C:\Scripts\CafeOrders.WatchDog.ps1
C:\Scripts\Register-CafeOrders.WatchDogTask.ps1
C:\Scripts\Run-CafeOrders.WatchDogHidden.vbs
C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe
C:\ServerNotifier\CafeOrders.ServerNotifier.exe
```

## Task Scheduler Kurulum

PowerShell'i yonetici olarak acin ve calistirin:

```powershell
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\Register-CafeOrders.WatchDogTask.ps1"
```

IIS AppPool ve Site baslatma islemleri icin task `RunLevel Highest` ile olusturulur. Task action `wscript.exe` uzerinden `Run-CafeOrders.WatchDogHidden.vbs` dosyasini calistirir; bu nedenle her dakika tetiklemede PowerShell/CMD penceresi ekrana gelip gitmez. Chrome/default browser acilisi ise varsayilan olarak `explorer.exe` uzerinden yapilir. Bu, URL'nin normal kullanici oturumundaki default browser profilinde acilmasini saglar.

## Manuel Test

Gorunur PowerShell ile debug etmek icin:

```powershell
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\CafeOrders.WatchDog.ps1"
```

Task Scheduler ile ayni gizli calisma davranisini test etmek icin:

```powershell
wscript.exe "C:\Scripts\Run-CafeOrders.WatchDogHidden.vbs" "C:\Scripts\CafeOrders.WatchDog.ps1" "http://192.168.2.11:5001/api/v1/settings/app" "http://192.168.2.11:5002/" "CafeOrders.API" "CafeOrders.WebUI" "CafeOrders.API" "CafeOrders.WebUI" "C:\Scripts\CafeOrders.WatchDog.log" "C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe" "C:\ServerNotifier\CafeOrders.ServerNotifier.exe"
```

## Parametreler

```powershell
-ApiAppPoolName "CafeOrders.API"
-WebUiAppPoolName "CafeOrders.WebUI"
-ApiSiteName "CafeOrders.API"
-WebUiSiteName "CafeOrders.WebUI"
-ApiHealthUrl "http://192.168.2.11:5001/api/v1/settings/app"
-WebUiUrl "http://192.168.2.11:5002/"
-LogPath "C:\Scripts\CafeOrders.WatchDog.log"
-AdminAudioAgentPath "C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe"
-ServerNotifierPath "C:\ServerNotifier\CafeOrders.ServerNotifier.exe"
```

## AdminAudioAgent Kontrolu

WatchDog, `CafeOrders.AdminAudioAgent.exe` surecini belirtilen exe yoluna gore kontrol eder. Calismiyorsa gizli sekilde baslatir. Agent kendi durumunu su dosyaya yazar:

```powershell
C:\ProgramData\CafeOrders\AdminAudioAgent\AdminAudioAgent.log
```

WebUI kapali veya sesi calamaz durumdaysa Agent, API ayarlarinda kayitli `Yeni Siparis Sesi` dosyasini WebUI uzerinden indirip oynatir. Ses kapaliysa veya dosya bulunamazsa appsettings izin veriyorsa sistem beep fallback devreye girer.

## ServerNotifier Kontrolu

WatchDog, `CafeOrders.ServerNotifier.exe` surecini belirtilen exe yoluna gore kontrol eder. Calismiyorsa gizli sekilde baslatir. Notifier, API/Hub uzerinden bekleyen siparis snapshot'ini takip eder ve bekleyen siparis oldugunda ekranin sag alt kosesinde top-most bildirim karti gosterir.

Varsayilan konum:

```powershell
C:\ServerNotifier\CafeOrders.ServerNotifier.exe
```

## Chrome Davranisi

Script su kontrolleri yapar:

- Chrome process command line icinde `192.168.2.11:5002` var mi
- aktif Chrome pencere basliginda CafeOrders basligi var mi
- Chrome tab basliklari UI Automation ile CafeOrders basligi tasiyor mu

Sayfa zaten aciksa yeni sekme acmaz. Sayfa acik degilse URL'yi Windows shell uzerinden acar.

