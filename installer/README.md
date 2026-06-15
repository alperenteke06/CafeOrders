# CafeOrders Setup Installer

Bu klasör CafeOrders otomatik kurulum motorunu içerir.

## Dosyalar

| Dosya | Amaç |
| --- | --- |
| `Install-CafeOrders.ps1` | GitHub Production paketini veya local paketi kullanarak IIS, appsettings, agent, notifier, scripts, firewall ve Task Scheduler kurulumunu yapar |
| `Build-CafeOrders.ProductionPackage.ps1` | `publishes` ve `scripts` içeriklerinden local `CafeOrders-Production.zip` paketi üretir |

## GitHub Production Paketi

Varsayılan kaynak:

```text
https://github.com/alperenteke06/CafeOrders/archive/refs/heads/Production.zip
```

Bu paket içinde şunlar beklenir:

- `publishes/API`
- `publishes/WebUI`
- `publishes/DesktopApp`
- `publishes/AdminAudioAgent`
- `publishes/ServerNotifier`
- `scripts`

## Local ZIP Paketi Üretme

```powershell
.\Build-CafeOrders.ProductionPackage.ps1
```

Üretilen zip paketi self-contained publish çıktıları nedeniyle genellikle GitHub'ın normal 100 MB dosya limitini aşar. Bu yüzden zip dosyasını repo içine commit etmek yerine GitHub Releases veya ayrı bir artifact/file share konumunda tutmak daha güvenlidir. Setup Wizard varsayılan olarak GitHub Production branch archive adresini kullanır; istenirse local zip veya farklı URL seçilebilir.

## Manuel Kullanım

PowerShell'i yönetici olarak açın:

```powershell
.\Install-CafeOrders.ps1 `
  -ServerIp "192.168.2.11" `
  -SqlInstanceName "DESKTOP-ET476QO\SQLEXPRESS01" `
  -SqlUser "sa" `
  -SqlPassword "JetNet@Admin120526!" `
  -IisRootPath "C:\inetpub\wwwroot"
```

## Güvenlik Notları

- SQL şifresi API ve WebUI `appsettings.json` dosyalarına yazılır.
- Script, API/WebUI appsettings dosyalarında inheritance'ı kapatıp Administrators, SYSTEM ve ilgili IIS AppPool kimliğine izin vermeye çalışır.
- `wwwroot/uploads` klasörü korunur; ürün görselleri ve ses dosyaları update sırasında silinmemelidir.
- WatchDog task `RunLevel Highest` ile oluşturulur.
- `5001` ve `5002` firewall inbound rule'ları otomatik oluşturulur.
