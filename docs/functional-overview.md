# Islevsel Dokuman

## 1. Sistem Amaci

CafeManagement, masa/kiosk istemcileri ile yonetim panelini ayni siparis ekosisteminde birlestiren bir siparis ve operasyon yonetim sistemidir.

Ana hedefler:

- kullanicinin masadan urun siparisi verebilmesi
- adminin siparisleri ve cihazlari tek panelden yonetebilmesi
- katalog, duyuru ve ayar degisikliklerinin canli yansitilmasi

## 2. Ana Roller

### Musteri / kiosk kullanicisi

- urunleri gorur
- kategoriye gore filtreler
- sepete ekler
- siparisi olusturur
- siparis durumunu ekranda takip eder

### Admin / operator

- cihazlari onaylar ve masalara baglar
- siparisleri kabul eder, reddeder, tamamlar
- urun, kategori ve fiyat gunceller
- kiosk bilgi mesaji ve duyurulari yonetir
- masa ve cihaz durumlarini takip eder

## 3. Ana Moduller

## 3.1 Cihaz Kaydi ve Onay

DesktopApp ilk acildiginda:

- cihaz kimligini olusturur
- API'ye kayit istegi yollar
- cihaz henuz onaylanmadiysa bekleme ekranina gecer

Admin:

- WebUI cihaz listesinde yeni cihazı gorur
- cihazı onaylar
- masaya baglar

Beklenen sonuc:

- DesktopApp otomatik olarak menu ekranina gecer

## 3.2 Katalog ve Kategori Gezinme

Kiosk tarafinda:

- kategori pills uzerinden filtreleme yapilir
- `Tumu` secildiginde urunler kategori sirasina gore listelenir
- urun kartlarinda isim, aciklama, fiyat ve gorsel bulunur

Admin tarafinda:

- kategori ekleme / guncelleme / silme
- urun ekleme / guncelleme / silme
- hizli fiyat girisi ile toplu fiyat degisimi

## 3.3 Sepet ve Siparis

Kiosk tarafinda:

- kullanici urunu sepete ekler
- miktar arttirir / azaltir
- siparisi onaylar

Sistem:

- siparisi API'ye kaydeder
- kiosk ekraninda durum ekranina gecer

Durumlar:

- `Siparisiniz Olusturuluyor`
- `Siparisiniz Alindi`
- `Siparisiniz Hazirlaniyor`
- `Siparisiniz Hazir`
- `Siparisiniz Iptal Edildi`

## 3.4 Siparis Yonetimi

WebUI uzerinden admin:

- yeni siparisleri gorur
- siparisi kabul eder
- reddeder
- tamamlar

Beklenen davranis:

- DesktopApp bu durumlari realtime popup / ekran gecisi ile alir

## 3.5 Duyuru ve Kiosk Bilgi Mesajlari

Iki temel kaynak vardir:

- varsayilan kiosk bilgi metni
- aktif bilgi/duyuru mesaji

Durum tipleri:

- `Onemli`
- `Duyuru`
- `Genel`

Beklenen sunum dili:

- Onemli: kirmizi tonlar
- Duyuru: altin/sari tema
- Genel: acik mavi tonlar

Beklenen davranis:

- admin guncellemesi sonrasinda WebUI ve DesktopApp sunumu canli degismelidir

## 3.6 Masa ve Cihaz Takibi

Admin paneli:

- hangi cihaz hangi masaya bagli
- cihaz online / offline mi
- masa aktif mi

Sistem:

- heartbeat ile cihaz varligi izlenir
- ani disconnect ve timeout senaryolari desteklenir

## 3.7 Bildirimler

WebUI tarafinda:

- yeni siparis
- siparis onaylandi
- siparis reddedildi
- siparis tamamlandi

gibi olaylar bildirim listesine duser.

Bildirimlerde:

- kayda git
- okunmamis / yeni vurgu
- siparis ozeti

beklenir.

## 4. Realtime Beklentiler

Sistemde asagidaki degisiklikler canli yansimalidir:

- cihaz onayi
- masa atamasi
- online / offline durumu
- urun gorsel / fiyat / isim degisikligi
- kategori degisikligi
- siparis durumu
- kiosk bilgi mesaji
- aktif duyuru
- footer / branding sunumu

## 5. Dosya ve Icerik Yonetimi

### Urun gorselleri

- admin panelinden yuklenir
- `wwwroot/uploads/products` altinda saklanir
- DesktopApp ister HTTP ister paylasimli klasor uzerinden gorseli alabilir

### Ses dosyalari

- admin panelinden yuklenir
- `wwwroot/uploads/sounds` altinda tutulur

## 6. Operasyonel Islevler

Yonetim paneli su operasyonel senaryolari kapsar:

- cihaz kaydi onaylama
- masa esleme
- urun ve kategori bakimi
- toplu fiyat guncelleme
- kiosk duyurusu ve onem seviyesi ayarlama
- marka / gelistirici bilgisi guncelleme

## 7. Baslica Kullanici Senaryolari

### Senaryo 1: Yeni cihaz devreye alma

1. DesktopApp acilir
2. cihaz bekleme ekraninda kalir
3. admin cihazı onaylar
4. masa atanir
5. cihaz otomatik menuye gecer

### Senaryo 2: Musteri siparisi

1. kullanici kategori secer
2. urunleri sepete ekler
3. siparisi gonderir
4. admin siparisi gorur
5. admin kabul eder
6. kiosk durum ekraninda hazirlaniyor gorunur

### Senaryo 3: Urun guncelleme

1. admin urun fiyatini veya gorselini gunceller
2. sistem katalog guncelleme yayini yapar
3. WebUI ve DesktopApp guncel urunu gosterir

### Senaryo 4: Acil duyuru

1. admin aktif bilgi mesaji tanimlar
2. tip `Onemli` olarak secilir
3. kiosk istemciler kirmizi uyarili mesaj kutusuna gecer

## 8. Mevcut Gelisim Alanlari

Fonksiyonel olarak gelecekte genisletilebilecek alanlar:

- detayli raporlama ve satis analitigi
- kullanici yetki seviyeleri
- urun stok entegrasyonu
- siparis mutfak ekran entegrasyonu
- test ve kabul senaryolari icin yazili SOP dokumani
