# CafeOrders Docs

Bu klasor, proje yapisini hizli anlamak ve kurulum / gelistirme surecini standartlastirmak icin hazirlandi.

## Icerik

- [Teknik Mimari](C:\AllActivities\SoftwareDev\CafeOrders\docs\technical-architecture.md)
- [Islevsel Dokuman](C:\AllActivities\SoftwareDev\CafeOrders\docs\functional-overview.md)
- [Deployment Rehberi](C:\AllActivities\SoftwareDev\CafeOrders\docs\deployment-guide.md)
- [API Referansi](C:\AllActivities\SoftwareDev\CafeOrders\docs\api-reference.md)
- [Realtime Event Sozlugu](C:\AllActivities\SoftwareDev\CafeOrders\docs\realtime-events.md)

## Cozum Yapisi

Ana klasorler:

- `src/`: uygulama kaynak kodlari
- `tests/`: test projeleri
- `publishes/`: publish ciktilari
- `artifacts/`: build ve dogrulama ciktilari
- `docs/`: teknik ve islevsel dokumanlar

`src/` altindaki projeler:

- `CafeOrders.API`: istemci ve yonetim islemleri icin REST API + SignalR hub
- `CafeOrders.WebUI`: admin paneli / MVC yonetim arayuzu
- `CafeOrders.DesktopApp`: masa/kiosk icin WPF istemci
- `CafeOrders.Application`: servis abstraksiyonlari ve contract DTO katmani
- `CafeOrders.Domain`: temel entity ve enum tanimlari
- `CafeOrders.Infrastructure`: EF Core, servis implementasyonlari, guvenlik ve realtime katmani

## Hangi Dokuman Ne Icin?

- Teknik mimari: klasor yapisi, veri akisi, konfigurasyon, deployment ve realtime baglantilari
- Islevsel dokuman: sistemin kullanici tarafindaki davranisi, modul bazli sorumluluklar ve temel senaryolar
- Deployment rehberi: publish, IIS, SQL ve istemci dagitimi adimlari
- API referansi: temel endpoint listesi ve beklenen kullanim amaci
- Realtime event sozlugu: SignalR uzerinden yayinlanan olaylar ve tuketici davranislari
