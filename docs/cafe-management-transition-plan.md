# CafeManagement Gecis Plani

## Amac

Bu dokuman, mevcut `CafeManagement` uygulamasini calisan ozellikleri bozmadan internet kafe ve gaming center odakli `CafeManagement` platformuna evirmek icin ilk teknik yol haritasini tanimlar.

Gecis stratejisi:

- Once mevcut siparis, cihaz onay, kiosk ve realtime akislari korunur.
- Yeni domain modelleri mevcut katmanlara kontrollu olarak eklenir.
- Rename Sprint ile solution, proje ve namespace isimleri `CafeManagement.*` standardina alinmistir.
- Yeni moduller buyudukce davranis testleri korunarak ayrilir.

## Mevcut Solution Durumu

Mevcut projeler:

- `CafeManagement.Core`: Entity ve enum cekirdegi.
- `CafeManagement.Application`: Use-case arayuzleri ve request/response kontratlari.
- `CafeManagement.Infrastructure`: EF Core, servis implementasyonlari, SignalR notifier, seed ve migration.
- `CafeManagement.Api`: REST API ve SignalR hub yayin noktasi.
- `CafeManagement.Manager`: Manager paneli benzeri MVC web arayuzu.
- `CafeManagement.Kiosk`: Client kiosk/siparis WPF uygulamasi.
- `CafeManagement.Tests`: Mevcut unit/integration testleri.

Mevcut urun yetenekleri:

- Cihaz register/onay/reject akisi.
- Masa esleme.
- Urun/kategori yonetimi.
- Kiosk bilgi/duyuru ayarlari.
- Siparis olusturma, onaylama, reddetme.
- SignalR ile realtime katalog, cihaz, siparis ve ayar bildirimi.
- WPF client uzerinde kiosk benzeri menu ve sepet arayuzu.

## Hedef Modul Haritasi

Rename Sprint sonrasinda aktif proje adlari asagidaki sorumluluk haritasi ile ilerler.

| Hedef Modul | Ilk Konum | Not |
| --- | --- | --- |
| Core | `CafeManagement.Core` | Entity ve enum cekirdegi. |
| Application | `CafeManagement.Application` | Session, pricing, payment ve command use-case kontratlari burada baslar. |
| Infrastructure | `CafeManagement.Infrastructure` | EF Core, persistence, file storage ve realtime implementasyonlari burada kalir. |
| Api | `CafeManagement.Api` | Device, session, payment, command, screenshot endpointleri buraya eklenir. |
| Manager | `CafeManagement.Manager` | Mevcut admin paneli Manager UI'a evrilir. |
| Kiosk | `CafeManagement.Kiosk` | Kiosk UI simdilik WPF projesinde kalir. |
| Agent | Yeni proje adayi | Process, command, screenshot, file transfer icin ayrilacak servis katmani. |
| Watchdog | Yeni proje adayi | Agent/Kiosk saglik izleme ve restart sorumlulugu. |
| Orders | Mevcut domain/application | Urun ve siparis akisi session bazli hale getirilir. |
| Payments | Mevcut domain/application | Odeme modelleri eklendikten sonra ayrilabilir. |
| Communication | Mevcut SignalR altyapisi | Chat, zorunlu popup, teslim/okundu bilgileri eklenir. |
| Screenshots | Yeni servis/model seti | Agent geldikten sonra ayrilmasi daha saglikli. |
| FileTransfer | Yeni servis/model seti | Storage ve audit ihtiyaci netlesince ayrilir. |
| Remote | Sonraki faz | Once command/screenshot altyapisi tamamlanmali. |
| Updater | Sonraki faz | Agent/Kiosk/Watchdog stable olduktan sonra eklenmeli. |

## Sprint 1 Kapsami

Sprint 1 hedefi kod davranisini degistirmek degil, guvenli gelistirme hattini kurmaktir.

Tamamlanacaklar:

- `development/cafe-management-platform` branch'i master uzerinden olusturulur.
- Mevcut solution yapisi analiz edilir.
- Hedef CafeManagement modul haritasi dokumante edilir.
- Buyuk rename/refactor icin riskler belirlenir.
- Build ve testler calistirilir.

Sprint 1 disinda birakilanlar:

- Namespace toplu rename. Rename Sprint kapsaminda tamamlandi.
- Solution dosyasinda coklu yeni proje acma.
- Database migration ile yeni modeller ekleme.
- Kiosk shell replacement davranisi.
- Remote control, screenshot ve file transfer implementasyonu.

## Rename Sprint Kapsami

Rename Sprint hedefi mevcut davranisi bozmadan proje kimligini `CafeManagement` standardina tasimaktir.

Tamamlananlar:

- Solution dosyasi `CafeManagement.slnx` olarak yeniden adlandirildi.
- Proje klasorleri ve `.csproj` adlari `CafeManagement.Core`, `CafeManagement.Application`, `CafeManagement.Infrastructure`, `CafeManagement.Api`, `CafeManagement.Manager`, `CafeManagement.Kiosk` ve `CafeManagement.Tests` olarak tasindi.
- Namespace ve assembly referanslari yeni proje adlariyla hizalandi.
- EF Core context adi `CafeManagementDbContext` olarak guncellendi.
- WatchDog scriptleri `CafeManagement` isimlendirmesine tasindi.
- Publish ciktilari yeni assembly adlariyla yeniden olusturuldu.

## Gecis Ilkeleri

- `master` dogrudan gelistirme branch'i olarak kullanilmaz.
- Her sprint sonunda build ve test calistirilir.
- Database migrationlari kucuk ve geri izlenebilir tutulur.
- Manager UI dogrudan database'e erismez; API/Application kontratlari uzerinden ilerler.
- Session sure/ucret hesaplari client tarafina birakilmaz.
- Realtime event kontratlari versiyonlanabilir sekilde genisletilir.
- Kritik islemler icin audit log modeli Sprint 2-3 bandinda hazirlanir.

## Onerilen Sprint Sirasi

### Sprint 2: Core Domain Genisletme

Oncelikli entity/enum seti:

- `Branch`
- `TableGroup`
- `Session`
- `SessionEvent`
- `PriceProfile`
- `PriceRule`
- `Member`
- `MemberTransaction`
- `Payment`
- `CashRegisterTransaction`
- `AuditLog`

Enum seti:

- `SessionType`
- `SessionStatus`
- `TableStatus`
- `PaymentMethod`
- `PaymentStatus`
- `AuditActionType`

### Sprint 3: Device Register ve Hardware Snapshot

Mevcut `Device` akisi korunarak genisletilecek alanlar:

- Client version
- OS bilgisi
- CPU/RAM/GPU bilgisi
- Agent status
- Kiosk status
- Last heartbeat metadata

Yeni model:

- `DeviceHardwareSnapshot`

### Sprint 4: Session Yonetimi

Session server-authoritative olmalidir.

Desteklenecek oturum tipleri:

- Sureli
- Suresiz
- Uye
- Admin
- Servis

### Sprint 5: Pricing, Orders ve Payments

Mevcut siparis modeli session'a baglanir.

Odeme destekleri:

- Nakit
- POS/kart
- Parcali odeme
- Uye bakiyesi

### Sprint 6: Manager Cihaz Kontrolu

Komut altyapisi:

- Lock/unlock
- Shutdown/restart
- Process listesi
- Process start/kill
- Execute command
- WOL

### Sprint 7: Kiosk, Agent ve Watchdog

Bu sprintten once Agent proje siniri netlestirilmelidir.

### Sprint 8: Communication ve Popup

Manager ile Client arasinda:

- Chat
- Zorunlu popup
- Teslim edildi bilgisi
- Okundu bilgisi

### Sprint 9: Screenshot ve File Transfer

Agent uzerinden ilerlemelidir.

### Sprint 10: Remote, Reports ve Stabilizasyon

Remote destek, raporlama ve genel hardening fazidir.

## Riskler

- Toplu namespace/proje rename mevcut publish ve deployment akislarini bozabilir.
- Agent/Watchdog ayrimi yapilmadan shell replacement'a gecmek client makinelerde kilitlenme riski tasir.
- Remote command ve screenshot ozellikleri yetki, audit ve guvenlik modeli olmadan eklenmemelidir.
- Payment ve session modelleri tamamlanmadan mevcut siparis akisi agresif refactor edilmemelidir.

## Karar

Ilk gelistirme sprintlerinde proje adlari korunacak. `CafeManagement` isimlendirmesi once domain dili, UI metinleri ve dokumantasyon seviyesinde yerlestirilecek. Fiziksel proje/namespace rename islemi, session-payment-device-command temel modelleri oturduktan ve test kapsami genisledikten sonra ayri bir refactor sprintinde yapilacak.
