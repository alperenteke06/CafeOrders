# CafeManagement WatchDog

## Amac

Bu script, IIS uzerindeki `CafeManagement.Api` ve `CafeManagement.Manager` AppPool/Site durumlarini kontrol eder. Herhangi biri calismiyorsa baslatir. IIS taraflari ayaga kalktiktan sonra WebUI icin HTTP health check yapar. WebUI saglikliysa Chrome/default tarayici uzerinde admin panelinin zaten acik olup olmadigini kontrol eder.

## Dosyalar

- `CafeManagement.WatchDog.ps1`: asil izleme ve kurtarma scripti
- `Register-CafeManagement.WatchDogTask.ps1`: Task Scheduler gorevini olusturan yardimci script
- `Run-CafeManagement.WatchDogHidden.vbs`: PowerShell penceresinin ekrana gelmesini engelleyen gizli calistirici

## Onerilen Konum

Production makinede:

```powershell
C:\Scripts\CafeManagement.WatchDog.ps1
C:\Scripts\Register-CafeManagement.WatchDogTask.ps1
C:\Scripts\Run-CafeManagement.WatchDogHidden.vbs
```

## Task Scheduler Kurulum

PowerShell'i yonetici olarak acin ve calistirin:

```powershell
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\Register-CafeManagement.WatchDogTask.ps1"
```

IIS AppPool ve Site baslatma islemleri icin task `RunLevel Highest` ile olusturulur. Task action `wscript.exe` uzerinden `Run-CafeManagement.WatchDogHidden.vbs` dosyasini calistirir; bu nedenle her dakika tetiklemede PowerShell/CMD penceresi ekrana gelip gitmez. Chrome/default browser acilisi ise varsayilan olarak `explorer.exe` uzerinden yapilir. Bu, URL'nin normal kullanici oturumundaki default browser profilinde acilmasini saglar.

## Manuel Test

Gorunur PowerShell ile debug etmek icin:

```powershell
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\CafeManagement.WatchDog.ps1"
```

Task Scheduler ile ayni gizli calisma davranisini test etmek icin:

```powershell
wscript.exe "C:\Scripts\Run-CafeManagement.WatchDogHidden.vbs" "C:\Scripts\CafeManagement.WatchDog.ps1" "http://192.168.1.104:5002/" "CafeManagement.Api" "CafeManagement.Manager" "CafeManagement.Api" "CafeManagement.Manager" "C:\Scripts\CafeManagement.WatchDog.log"
```

## Parametreler

```powershell
-ApiAppPoolName "CafeManagement.Api"
-WebUiAppPoolName "CafeManagement.Manager"
-ApiSiteName "CafeManagement.Api"
-WebUiSiteName "CafeManagement.Manager"
-WebUiUrl "http://192.168.1.104:5002/"
-LogPath "C:\Scripts\CafeManagement.WatchDog.log"
```

## Chrome Davranisi

Script su kontrolleri yapar:

- Chrome process command line icinde `192.168.1.104:5002` var mi
- aktif Chrome pencere basliginda CafeManagement basligi var mi
- Chrome tab basliklari UI Automation ile CafeManagement basligi tasiyor mu

Sayfa zaten aciksa yeni sekme acmaz. Sayfa acik degilse URL'yi Windows shell uzerinden acar.
