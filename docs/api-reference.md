# API Referansı

## 1. Genel Bilgiler

Varsayılan API base URL:

```text
http://<server>:5001
```

API prefix:

```text
/api/v1
```

SignalR hub:

```text
http://<server>:5001/hubs/cafe
```

WebUI kendi içinde de `/hubs/cafe` route'unu map eder; admin panel realtime bağlantıları WebUI hostu üzerinden de kurulabilir.

## 2. Catalog API

Base route:

```text
/api/v1/catalog
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/api/v1/catalog` | Aktif katalog, kategori ve ürün listesini döndürür |
| `POST` | `/api/v1/catalog/products` | Ürün ekler veya günceller |
| `DELETE` | `/api/v1/catalog/products/{productId}` | Ürünü soft delete ile siler |
| `POST` | `/api/v1/catalog/categories` | Kategori ekler veya günceller |
| `DELETE` | `/api/v1/catalog/categories/{categoryId}` | Kategoriyi ve bağlı ürünleri soft delete ile siler |

Yazma işlemleri sonrasında `CatalogUpdated` event'i yayınlanır.

## 3. Dashboard API

Base route:

```text
/api/v1/dashboard
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/api/v1/dashboard/snapshot` | Dashboard özetini, cihazları, son siparişleri ve aktif bilgi mesajını döndürür |

## 4. Devices API

Base route:

```text
/api/v1/devices
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `POST` | `/api/v1/devices/register` | DesktopApp cihaz kaydı veya mevcut cihaz yenileme |
| `POST` | `/api/v1/devices/approve` | Cihaz onaylama ve gerekiyorsa masaya bağlama |
| `POST` | `/api/v1/devices/assign-table` | Cihazın bağlı olduğu masayı değiştirme |
| `DELETE` | `/api/v1/devices/{deviceId}` | Cihazı reddetme/silme |
| `POST` | `/api/v1/devices/heartbeat` | Cihaz online bilgisini güncelleme |

Yayınlanan eventler:

- `DeviceApproved`
- `DeviceRejected`
- `DeviceMapped`
- `DevicesUpdated`
- `TablesUpdated`

## 5. Orders API

Base route:

```text
/api/v1/orders
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/api/v1/orders` | Sipariş listesini döndürür |
| `GET` | `/api/v1/orders/{orderId}` | Tek sipariş detayını döndürür |
| `POST` | `/api/v1/orders` | Yeni sipariş oluşturur |
| `POST` | `/api/v1/orders/{orderId}/accept` | Siparişi onaylar |
| `POST` | `/api/v1/orders/{orderId}/reject` | Siparişi reddeder |
| `POST` | `/api/v1/orders/{orderId}/complete` | Siparişi tamamlandı yapar |
| `POST` | `/api/v1/orders/{orderId}/sound-played` | Yeni sipariş sesinin çalındığını işaretler |

Yeni sipariş oluşturma sırasında:

- Minimum sepet tutarı kontrol edilir.
- Sipariş `Pending` olarak kaydedilir.
- `OrderCreated` yayınlanır.
- Ses playback için `IsSoundPlayed=false` kalır.

`sound-played` endpoint'i WebUI veya AdminAudioAgent tarafından çağrılır. Başarılı çağrı sonrası:

- `IsSoundPlayed=true`
- `SoundPlayedAt=UtcNow`
- `OrderSoundAcknowledged` event'i
- merkezi log kaydı

## 6. Settings API

Base route:

```text
/api/v1/settings
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/api/v1/settings/app` | Uygulama ayarlarını döndürür |
| `PUT` | `/api/v1/settings/app` | Uygulama ayarlarını günceller |
| `GET` | `/api/v1/settings/info-message` | Aktif bilgi/duyuru mesajını döndürür |
| `PUT` | `/api/v1/settings/info-message` | Aktif bilgi/duyuru mesajını günceller |

App settings alanları:

- cafe adı
- geliştirici adı/telefonu
- sipariş kabul/red mesajları
- varsayılan kiosk bilgi metni
- kiosk bilgi tipi
- kiosk ikon anahtarı
- yeni sipariş sesi aktif/pasif
- hızlı onay modu
- canlı duyurular
- minimum sepet tutarı
- yeni sipariş ses URL'i

Yayınlanan eventler:

- `AppSettingsUpdated`
- `InfoMessageUpdated`

## 7. Tables API

Base route:

```text
/api/v1/tables
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/api/v1/tables` | Masa listesini döndürür |
| `POST` | `/api/v1/tables` | Masa ekler veya günceller |

## 8. Logs API

Base route:

```text
/api/v1/logs
```

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/api/v1/logs` | Merkezi log kayıtlarını filtreli döndürür |
| `POST` | `/api/v1/logs/client` | DesktopApp, AdminAudioAgent veya ServerNotifier gibi client kaynaklarından log kabul eder |

Filtreleme alanları:

- `source`
- `level`
- `search`
- `take`

Log eklendiğinde `ApplicationLogCreated` event'i yayınlanır.

## 9. WebUI MVC Route'ları

Admin panel route'ları:

| Method | Route | Açıklama |
| --- | --- | --- |
| `GET` | `/` | Ana dashboard shell |
| `GET` | `/dashboard/section/{section}` | Aktif bölüm partial içeriği |
| `GET` | `/dashboard/live` | Dashboard live JSON |
| `GET` | `/dashboard/orders/pending-sound` | Ses çalınmamış aktif siparişler |
| `GET` | `/dashboard/presentation` | Header/footer/ses/bilgi sunum ayarları |
| `POST` | `/dashboard/devices/approve` | Cihaz onayı |
| `POST` | `/dashboard/devices/assign-table` | Cihaz masa atama |
| `DELETE` | `/dashboard/devices/{deviceId}` | Cihaz silme/reddetme |
| `POST` | `/dashboard/orders/{orderId}/accept` | Sipariş onaylama |
| `POST` | `/dashboard/orders/{orderId}/reject` | Sipariş reddetme |
| `POST` | `/dashboard/orders/{orderId}/complete` | Sipariş tamamlama |
| `POST` | `/dashboard/products/upload-image` | Ürün görseli upload |
| `POST` | `/dashboard/products` | Ürün ekleme/güncelleme |
| `POST` | `/dashboard/products/bulk-prices` | Hızlı fiyat güncelleme |
| `DELETE` | `/dashboard/products/{productId}` | Ürün silme |
| `GET` | `/dashboard/catalog/categories` | Ürün popup kategori seçenekleri |
| `POST` | `/dashboard/categories` | Kategori ekleme/güncelleme |
| `DELETE` | `/dashboard/categories/{categoryId}` | Kategori silme |
| `POST` | `/dashboard/tables` | Masa ekleme/güncelleme |
| `PUT` | `/dashboard/settings/app` | App settings güncelleme |
| `POST` | `/dashboard/settings/upload-sound` | Yeni sipariş sesi upload |
| `POST` | `/dashboard/info-message` | Aktif bilgi mesajı güncelleme |

## 10. Upload Kuralları

Ürün görseli:

- Endpoint: `POST /dashboard/products/upload-image`
- Maksimum boyut: `UploadValidation.MaxProductImageBytes`
- Formatlar: `JPG`, `PNG`, `WEBP`, `GIF`
- Kayıt yolu: `wwwroot/uploads/products`

Sipariş sesi:

- Endpoint: `POST /dashboard/settings/upload-sound`
- Maksimum boyut: `UploadValidation.MaxSoundBytes`
- Formatlar: `MP3`, `WAV`, `OGG`, `M4A`, `AAC`, `FLAC`, `WEBM`
- Kayıt yolu: `wwwroot/uploads/sounds`

Dosyalar GUID tabanlı isimle saklanır ve URL olarak geri döner.

## 11. Auth Notları

WebUI:

- Cookie authentication kullanır.
- Login route: `/account/login`.
- Oturum süresi `SessionSettings:AdminCookieDays` ile yönetilir.
- DataProtection key path production'da sabit tutulmalıdır; aksi halde cookie geçersizleşebilir.

API:

- JWT konfigürasyonu appsettings altında bulunur.
- DesktopApp ve iç entegrasyonlarda servis bazlı validasyon ve cihaz token akışları kullanılır.

## 12. Hata ve Operasyon Notları

- Yazma endpointleri başarısız olduğunda JSON `{ message }` döndürmeye çalışır.
- WebUI API hatasını kullanıcıya toast olarak gösterir.
- Kritik realtime event kaçırılırsa DesktopApp, AdminAudioAgent veya ServerNotifier tarafında retry/polling fallback bulunur.
- Orders ekranı range filtresine bağlı kalmadan siparişleri listeler; arama kutusu aktif sayfa içinde çalışır.
