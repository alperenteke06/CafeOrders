# API Referansi

## 1. Genel

Base URL:

- `http://<server>:5001`

SignalR Hub:

- `http://<server>:5001/hubs/cafe`

API versiyon prefix'i:

- `/api/v1`

## 2. Katalog

Base route:

- `/api/v1/catalog`

Endpointler:

- `GET /api/v1/catalog`
  - aktif kategori ve urun katalogunu dondurur
- `POST /api/v1/catalog/products`
  - urun ekler veya gunceller
- `DELETE /api/v1/catalog/products/{productId}`
  - urunu soft delete ile siler
- `POST /api/v1/catalog/categories`
  - kategori ekler veya gunceller
- `DELETE /api/v1/catalog/categories/{categoryId}`
  - kategoriyi ve bagli urunleri soft delete ile siler

## 3. Dashboard

Base route:

- `/api/v1/dashboard`

Endpointler:

- `GET /api/v1/dashboard/snapshot`
  - dashboard ozet verisini dondurur

## 4. Devices

Base route:

- `/api/v1/devices`

Endpointler:

- `POST /api/v1/devices/register`
  - cihaz kaydi / mevcut cihaz yenileme
- `POST /api/v1/devices/approve`
  - cihaz onaylama
- `POST /api/v1/devices/assign-table`
  - cihaza masa atama
- `DELETE /api/v1/devices/{deviceId}`
  - cihazi reddetme / silme
- `POST /api/v1/devices/heartbeat`
  - cihazin online bilgisini tazeler

## 5. Orders

Base route:

- `/api/v1/orders`

Endpointler:

- `GET /api/v1/orders`
  - siparis listesi
- `GET /api/v1/orders/{orderId}`
  - tek siparis detayi
- `POST /api/v1/orders`
  - yeni siparis olusturma
- `POST /api/v1/orders/{orderId}/accept`
  - siparisi onaylama
- `POST /api/v1/orders/{orderId}/reject`
  - siparisi reddetme
- `POST /api/v1/orders/{orderId}/complete`
  - siparisi tamamlandi olarak isaretleme

## 6. Settings

Base route:

- `/api/v1/settings`

Endpointler:

- `GET /api/v1/settings/app`
  - uygulama ayarlarini getirir
- `PUT /api/v1/settings/app`
  - uygulama ayarlarini gunceller
- `GET /api/v1/settings/info-message`
  - aktif bilgi/duyuru mesajini getirir
- `PUT /api/v1/settings/info-message`
  - aktif bilgi/duyuru mesajini gunceller

## 7. Tables

Base route:

- `/api/v1/tables`

Endpointler:

- `GET /api/v1/tables`
  - masa listesini getirir
- `POST /api/v1/tables`
  - masa ekler veya gunceller

## 8. WebUI Uzerinden Kullanilan Baslica Yonetim Route'lari

Bu route'lar tarayici tarafinda kullanilan MVC / AJAX endpointleridir:

- `GET /`
- `GET /dashboard/section/{section}`
- `GET /dashboard/live`
- `GET /dashboard/presentation`
- `POST /dashboard/orders/{orderId}/accept`
- `POST /dashboard/orders/{orderId}/reject`
- `POST /dashboard/orders/{orderId}/complete`
- `POST /dashboard/products/upload-image`
- `POST /dashboard/products`
- `POST /dashboard/products/bulk-prices`
- `DELETE /dashboard/products/{productId}`
- `POST /dashboard/categories`
- `DELETE /dashboard/categories/{categoryId}`
- `POST /dashboard/tables`
- `POST /dashboard/devices/assign-table`
- `PUT /dashboard/settings/app`
- `POST /dashboard/info-message`

## 9. Auth Notlari

WebUI:

- cookie auth kullanir
- login route: `/account/login`

API:

- JWT yapisi infrastructure tarafinda tanimlidir
- su an istemci akislari ve ic entegrasyonlar projedeki servis ve dashboard kullanimi ile ilerlemektedir

## 10. Operasyonel Notlar

- urun ve kategori yazma islemleri sonunda katalog event'i yayilir
- siparis durum degisimlerinde ilgili kiosk cihaza ve admin grubuna event yayilir
- ayar ve bilgi mesaji degisimleri `All` client'lara gidecek sekilde yayimlanir
