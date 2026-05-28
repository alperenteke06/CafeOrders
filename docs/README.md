# CafeOrders Dokümantasyonu

Bu klasör, proje yapısını hızlı şekilde anlamak, geliştirme süreçlerini standartlaştırmak ve kurulum/deployment adımlarını merkezi olarak yönetmek amacıyla hazırlanmıştır.

---

# Doküman İçeriği

* Teknik Mimari
  `docs/technical-architecture.md`

* İşlevsel Doküman
  `docs/functional-overview.md`

* Deployment Rehberi
  `docs/deployment-guide.md`

* API Referansı
  `docs/api-reference.md`

* Realtime Event Sözlüğü
  `docs/realtime-events.md`

---

# Çözüm Yapısı

## Ana Klasörler

| Klasör       | Açıklama                         |
| ------------ | -------------------------------- |
| `src/`       | Uygulama kaynak kodları          |
| `tests/`     | Test projeleri                   |
| `publishes/` | Publish çıktıları                |
| `artifacts/` | Build ve doğrulama çıktıları     |
| `docs/`      | Teknik ve işlevsel dokümantasyon |

---

# Proje Katmanları

`src/` altında yer alan projeler:

| Proje                       | Açıklama                                                           |
| --------------------------- | ------------------------------------------------------------------ |
| `CafeOrders.API`            | REST API ve SignalR tabanlı realtime iletişim katmanı              |
| `CafeOrders.WebUI`          | Yönetim paneli ve MVC tabanlı admin arayüzü                        |
| `CafeOrders.DesktopApp`     | Masa/kiosk kullanım senaryoları için WPF istemci uygulaması        |
| `CafeOrders.Application`    | Uygulama servis kontratları, DTO yapıları ve abstraction katmanı   |
| `CafeOrders.Domain`         | Temel entity, enum ve domain modelleri                             |
| `CafeOrders.Infrastructure` | EF Core, güvenlik, servis implementasyonları ve realtime altyapısı |

---

# Doküman Rehberi

## Teknik Mimari

Sistem mimarisi, katmanlar arası veri akışı, konfigürasyon yapısı, deployment mimarisi ve realtime iletişim bileşenlerini içerir.

## İşlevsel Doküman

Sistemin kullanıcı tarafındaki davranışlarını, modül sorumluluklarını ve temel kullanım senaryolarını açıklar.

## Deployment Rehberi

Publish alma, IIS yapılandırması, SQL Server kurulumu ve istemci dağıtım süreçlerini içerir.

## API Referansı

REST endpoint listesi, kullanım amaçları, request/response yapıları ve temel entegrasyon bilgilerini içerir.

## Realtime Event Sözlüğü

SignalR üzerinden yayınlanan event tiplerini, payload yapılarını ve istemci davranışlarını tanımlar.

---

# Genel Notlar

* Tüm projeler `.NET` tabanlı çok katmanlı mimari yaklaşımı ile geliştirilmiştir.
* Realtime iletişim altyapısında `SignalR` kullanılmaktadır.
* Veri erişim katmanında `Entity Framework Core` tercih edilmiştir.
* Dokümantasyon içerikleri proje geliştikçe güncellenmelidir.
