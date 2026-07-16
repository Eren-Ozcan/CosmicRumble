# CosmicRumble — Backlog

Deferred work identified during the economy/achievement audit and fix pass. Not started unless noted.

## YAYIN YOL HARİTASI — proje sonuna kadar kalan her şey (genel kontrol, 2026-07-09)

Kod tabanının tamamı + bu backlog + master spec taranarak çıkarıldı. Çekirdek oyun, online
multiplayer, ekonomi, başarımlar, kupa/leaderboard, cloud save, ses, mobil girdi, Brawl Stars UI
ve sosyal sistem + tam ekran giriş bitmiş durumda — proje "yayına hazırlama" aşamasında.
**Kritik yol:** Play Console kapalı test zorunluluğu (madde 4) takvimin darboğazı — önce o
başlatılmalı; beklerken 1-3 kapanmalı; kostümler (9) + avatar ikonları (13) sanat işi olarak
paralel yürümeli (ikisi de veri/kod tarafı tamam, yalnız gerçek görsel bekliyor).
Madde 5 (Gem fiyatlandırma) kod değil İŞ KARARI — kullanıcı verecek. Madde 21 (localization) artık
tamamen bitti (2026-07-10) — CJK font dahil, kalan yalnız 150 kostüm isminin diğer 6 dile çevrilmesi.

### 1. Açık işin devamı (şimdi sırada)
1. Google girişi Console/Dashboard kurulumu — aşağıdaki "Google Play Games GİRİŞİ" bölümündeki
   7 adım. Kod hazır, bekliyor.
2. Arkadaş/davet akışının iki cihazlı uçtan uca testi (istek gönder/kabul, presence, davet →
   özel maç → maç sonu). Tek taraflı doğrulandı, iki taraflı hiç test edilmedi.
3. İlk gerçek Android cihaz build testi — yeni giriş akışı, gerçek store IAP davranışı,
   performans. Proje şu ana dek yalnız Editor + Device Simulator'da koştu.

### 2. Yayın öncesi zorunlu — mağaza/hesap işleri (kod değil, uzun teslim süreli, paralel başlat)
4. Google Play Console: uygulama kaydı, **kapalı testte 12 test kullanıcısı × 14 gün zorunluluğu**
   (yeni bireysel hesaplar — yayın takvimini bu belirler), Data Safety formu, içerik
   derecelendirme, mağaza görselleri/açıklama, AAB imzalama.
5. IAP gerçek SKU'ları: `gem_pack_100..6000` Console'da birebir aynı ID'lerle; fiyat/tier'lar
   placeholder — fiyatlandırma iş kararı olarak verilmedi.
6. Başarım ID eşlemesi: **kod tarafı tamam (2026-07-11)** — `AchievementDefinition` artık
   `steamId`/`googlePlayId`/`gameCenterId` alanları taşıyor (boşsa `achievementId`'ye düşer),
   `AchievementManager.ResolvePlatformId()` aktif sağlayıcıya göre doğru ID'yi seçip provider'a
   onu gönderiyor. **Kalan: yalnız veri girişi** — 50 başarım ilgili Console'larda oluşturulup
   ürettikleri opak ID'ler (`CgkI...` vb.) bu üç alana Inspector'dan tek tek yazılacak, kod
   değişikliği gerekmeyecek.
7. Yasal: **taslak metin + kod altyapısı tamam (2026-07-11)** — `legal/PRIVACY_POLICY.md` ve
   `legal/TERMS_OF_SERVICE.md` (TR+EN, kodda gerçekten aktif olan UGS sistemleri baz alınarak
   yazıldı) eklendi, **hukuki incelemeden geçmeden yayınlanmamalı** (KVKK/GDPR maddeleri ve
   sorumluluk sınırlaması `{{...}}` placeholder'ları hukukçu onayı bekliyor). Ayarlar panelinde
   (`MainMenuUI`) tüm sekmelerde görünen "Gizlilik Politikası"/"Kullanım Koşulları" linkleri
   eklendi (`Assets/Scripts/Utilities/LegalLinks.cs` üzerinden `Application.OpenURL`). **Kalan**:
   metinler hukukçu onayından geçip gerçek bir URL'de barındırılmalı, sonra `LegalLinks.cs`'teki
   placeholder URL'ler gerçek adresle değiştirilmeli; yaş derecelendirmesi Console'da belirlenince
   madde 5 (çocuk gizliliği) doldurulmalı.
8. iOS hattı (Android'den sonra): Apple Developer hesabı, Mac/build pipeline,
   `AppleGameCenterAuthProvider` stub'ının doldurulması (Apple.GameKit +
   `SignInWithAppleGameCenterAsync`), App Privacy etiketi, TestFlight. Şu an iOS için hiçbir şey yok.
   **Denendi ve sert bir platform engeli bulundu (2026-07-11)**: Apple.GameKit'i Package Manager'a
   git URL'iyle (`https://github.com/apple/unityplugins.git?path=/plug-ins/Apple.GameKit`) eklemek
   "package.json bulunamadı" hatasıyla temiz başarısız oldu (projede kalıntı yok, derleme temiz
   kaldı) — resmi Apple deposu doğrudan UPM git-paketi değil. Apple'ın kendi Quickstart dokümanına
   göre önce `python3 build.py` ile native (Xcode gerektiren) kütüphaneler derlenip bir `.tgz`
   üretilmeli, paket Unity'ye "Add package from tarball" ile öyle eklenmeli — bu derleme adımı
   **yalnızca macOS'ta çalışır**. Yani bu yalnızca Apple Developer hesabı meselesi değil: Windows'ta
   hiçbir şekilde kurulamıyor, Mac/Xcode olmadan denenecek başka bir yol yok.

### 3. Oyun içeriği eksikleri (spec'te var, başlanmadı)
9. 150 kostüm (master spec Bölüm 4): **veri tarafı tamam** (2026-07-09) — 150 `CostumeDefinition` +
   `CostumeDatabase` üretildi, `CostumeManager` bootstrap edildi, GARDIROP paneli (yalnızca sahip
   olunanlar) çalışıyor ve play-test edildi. **Kalan: gerçek sprite yok**, tüm kostümler UI'da rarity
   renkli daire + baş harf placeholder ile gösteriliyor. En büyük kalan içerik kalemi — tier başına
   şablon + renk varyasyonuyla küçültülebilir.
10. Harita/gezegen çeşitliliği: tek oynanış sahnesi (SampleScene); `LobbyData.MapName` kullanılmıyor.
    En az 2-3 farklı gezegen düzeni (çok gezegenli sahneler yerçekiminin vitrini).
11. **Tamam (2026-07-10)** — Tutorial/onboarding: `Assets/Scripts/Tutorial/TutorialManager.cs`
    (yeni). Bu cihazda oynanan ilk offline maçta (hotseat veya Antrenman — `GameInitializer`'ın
    spawn ettiği yerel karakter) 3 ipucu kartını sırayla gösterir ("A/D ile hareket et", "SPACE ile
    zıpla", "Bir silah seç, fareyle nişan al, ateş et"), her biri 4.5s, ✕ ile atlanabilir, son
    karttan sonra otomatik kapanır. Tek seferlik: `PlayerPrefs["cr_tutorial_seen"]` kalıcı, bir daha
    gösterilmez. Tam ekran blok değil — küçük üst-orta kart, hareket/ateş kontrollerini kaplamaz,
    turn timer'ı etkilemez. Online (Quick Match/özel maç) akışına bilerek bağlanmadı — oraya
    ulaşan oyuncu zaten en az bir offline maç oynamış olur. Play-tested: Editor'da `cr_tutorial_seen`
    temizlenip misafir girişiyle ANTRENMAN'a girildi, 3 kart sırayla doğru dilde (o an ayarlı olan
    İspanyolca) göründü, otomatik kapandı, `PlayerPrefs` değeri 1 olarak doğrulandı, konsolda hata
    yok.
12. Bot AI: **Antrenman modu tamam** (2026-07-10) — ana menü ☰ çekmecesinde gerçek oyunculara açık
    "ANTRENMAN" butonu, doğrudan Game sahnesini 2 tamamen pasif botla açar (hiç hareket/ateş
    etmezler — bkz. "Antrenman Modu" bölümü). Quick Match'te rakip yoksa bot doldurma hâlâ yapılmadı
    (ayrı, opsiyonel iş).
13. Profil ikonları/avatar: **tamam** (2026-07-10) — seçilebilir 16 avatarlık sistem çalışıyor,
    üst bar canlı güncelleniyor. **Kalan: gerçek ikon görselleri yok** (bkz. "Profil Avatarları"
    bölümü) — sonraki iş listesine eklendi, şimdilik renk+baş harf placeholder.

### 4. Bilinen pürüzler / teknik borç
14. SOSYAL kategorisi başarımları: **8/10 çalışır durumda** (2026-07-10) — `SOSYAL_KELEBEK`,
    `HERKESE_MEYDAN`, `DUELLO_SAMPIYONU` (önceki geçiş) + `INTIKAM`, `REKABETCI`, `KOZMIK_EKIP`,
    `BIR_NUMARA`, `KOZMIK_AVCI` (bu geçiş) bağlandı, bkz. "Sosyal Başarımlar" bölümü. Yalnızca
    `OGRETMEN` kapsam dışı kaldı — cross-client bildirim + ayrı bir gerçek iki-process test ortamı
    gerektiriyor, gerekçesi aynı bölümde.
15. `ui_button_hover` klibi: **tamam** (2026-07-10) — `UiKit.Hover()` eklendi, tüm programatik
    butonlara (29/30, bilinçli 1 istisna) bağlandı, play-test edildi. Bkz. "ui_button_hover wiring".
16. Ölü kod: **tamam** (2026-07-10) — `AbilityController.cs` ve `ObjectSpawnSkill.cs` silindi.
    Her ikisi de kod tabanında (script referansları) ve tüm `.unity`/`.prefab`/`.asset` dosyalarında
    (GUID bazlı Component referansları) tek eşleşme dahi bulunamadı — kendi dosyaları dışında hiçbir
    yerden çağrılmıyor/kullanılmıyorlardı (eski, `IAbility`'yi merkezi bir `List<MonoBehaviour>` ile
    yöneten mimarinin kalıntısı; güncel sistem her yeteneği kendi script'i üzerinden bağımsız çalıştırıyor).
    Silindikten sonra Play Mode'da misafir girişi + ana menü akışı hatasız çalıştığı doğrulandı
    (missing-script/missing-reference hatası yok).
17. **Tamam (2026-07-10)** — UGS timeout mesajı: `CloudSaveManager.IsUnavailable` public'e açıldı,
    `MainMenuUI.BootstrapSequence` init/pull zaman aşımına uğrarsa `LoadingScreenUI`'da kısa süreli
    yerelleştirilmiş "Playing offline" mesajı gösteriyor (önceden sessizce sıradaki adıma atlıyordu).
18. **Tamam (2026-07-10)** — Davet köşe durumları: `FriendLobbyPanelUI` ve `OnlineLobbyPanelUI`'a
    `OnApplicationPause`/`OnApplicationQuit` eklendi — host/client hâlâ lobi bekleme aşamasındayken
    (maç başlamadan) uygulama arka plana atılır/kapatılırsa `NetworkBootstrap.LeaveSessionAsync()`
    otomatik çağrılıp UGS session'ı temizleniyor. Maç başladıktan sonra bu panelller zaten
    yok/inaktif olduğu için mid-match'i etkilemiyor.

### 5. Yayın sonrası / opsiyonel
19. **Tamam (2026-07-10)** — Crash raporlama + analitik. `com.unity.services.cloud-diagnostics`
    1.0.12 ve `com.unity.services.analytics` 5.1.1 kuruldu (6.3.0 önce denendi, bu Unity sürümüyle
    [6000.1.17f1] uyumsuz çıktı — `RuntimePlatform.Switch2` derleme hatası, Unity 6000.2+ gerektiriyor;
    5.1.1'e düşürüldü). Crash raporlama tamamen kod gerektirmiyor — `ProjectSettings.asset`'te
    `enableCrashReportAPI: 1` (Player Settings → Other Settings → Crash Report API) açıldı, native
    `CrashReportHandler` + Cloud Diagnostics paketi geri kalanını otomatik hallediyor. Analitik için
    yeni `Assets/Scripts/Analytics/AnalyticsManager.cs` — `MainMenuUI.BootstrapSequence`'ta UGS Core
    hazır olduktan sonra `StartDataCollection()` çağırıyor (otomatik session/engagement event'leri
    dashboard şeması gerektirmeden toplanır), `TurnManager.FinishMatchLocally`'den her maç sonunda
    `match_completed` custom event'i (won/ranked) gönderiyor — **bu custom event'in gerçekten
    kaydedilmesi için UGS Dashboard'da aynı isimle bir şema tanımlanmalı** (kod hazır, dashboard adımı
    ayrı — Achievement/Leaderboard kurulumlarıyla aynı desen). SDK'nın kendi dokümantasyonu
    `StartDataCollection()`'ı "kullanıcı onayının alındığını veya gerekmediğini teyit eder" olarak
    tanımlıyor — gizlilik politikası (yol haritası madde 7, henüz YOK) canlıya alınmadan gerçek
    kullanıcı build'lerine dağıtılmamalı; Editor/iç test için sorun değil. Play-tested: Editor'da
    misafir girişiyle `AnalyticsManager` singleton'ının kurulduğu, `AnalyticsService.Instance`'ın
    gerçek user/session ID döndürdüğü ve `RecordMatchCompleted` çağrısının hatasız çalıştığı
    doğrulandı.
20. **Tamam (2026-07-10), cihaz testi hariç** — Push notification. Gerçek sunucu-tetikli UGS Push
    Notifications DEĞİL — bu oyunun ekonomisi client-authoritative ve hatırlatmaların ihtiyaç duyduğu
    tek veri (login streak, günlük sandık hakkı) zaten cihazda, sunucu tetikleyicisi gerekmiyor;
    standart mobil oyun deseni olan **local (cihazda zamanlanan) notification** ile yapıldı
    (`com.unity.mobile.notifications` 2.4.3 kuruldu). Yeni
    `Assets/Scripts/Notifications/LocalNotificationManager.cs`: `MainMenuUI.EnsureCoreSingletons`'ta
    bootstrap edilip `NotificationCenter.Initialize` çağrılıyor; `OnApplicationPause(true)`'da (oyuncu
    arka plana atınca) `LoginStreakManager.GetCurrentStreak() > 0` ise ~20 saat sonrasına "Serini
    kaybetme!" bildirimi, `ChestManager.GetRemainingChests() > 0` ise ~4 saat sonrasına "Sandıklar
    seni bekliyor!" bildirimi zamanlanıyor; `OnApplicationPause(false)`'da (geri dönünce) ikisi de
    iptal ediliyor. Tüm SDK çağrıları `#if UNITY_ANDROID || UNITY_IOS` ile korunuyor — paketin birleşik
    API assembly'si (`Unity.Notifications.Unified`) yalnızca Android/iOS/Editor için derleniyor, Windows
    Standalone (online test için kullanılan DevClient build'i) dışarıda kalıyor, bu yüzden
    `STEAMWORKS_INSTALLED`/`GPGS_INSTALLED` ile aynı korumalı-derleme deseni kullanıldı.
    **Cihaz/platform testi yapılamadı**: Editor şu an StandaloneWindows64 build target'ında
    (`UNITY_ANDROID` bu oturumda hiç tanımlı değildi), gerçek bildirim zamanlama/tetikleme davranışı
    yalnızca build target Android'e çevrilip gerçek cihazda/emülatörde denenince doğrulanabilir — aynı
    GPGS/Steamworks entegrasyonlarında olduğu gibi. Derleme (Editor, mevcut Standalone hedefinde) temiz.
21. Localization: **tamamen bitti** (2026-07-10) — İngilizce varsayılan + TR/ZH/ES/JA/KO/DE 7 dil,
    tüm UI paneller + achievement/quest verisi çevrildi, Ayarlar'da dil seçici var, CJK font (Noto
    Sans SC/JP/KR) kuruldu ve play-test edildi. **Kalan: yalnız** 150 kostüm ismi henüz İngilizce
    (diğer 6 dile çevrilmedi, düşük öncelik).
22. Sunucu tarafı doğrulama: ekonomi/CloudSave client-authoritative (hile açığı); IAP makbuz
    doğrulama + kritik işlemler için Cloud Code — gelir başlayınca öncelik.
23. Steam: bilinçli dondurulmuş (`STEAMWORKS_INSTALLED` hazır) — greenlight olursa App ID +
    Steamworks kurulumu.
24. Büyüme fikirleri (spec dışı): 2v2/4 oyuncu, sezonluk lig sıfırlama, battle pass.

## Kostüm Yeniden Tasarımı — 5 karakter × 3 kademe = 15 kostüm (2026-07-16, 2. tur)
Kullanıcı kararı: 150 kostüm 15'e indirildi — 5 karakter, her birinde 3 kademe
(Standard/Advanced/Elite). Silah kostümleri tamamen kalktı. Karakter isimleri ŞİMDİLİK JENERİK
("Character 1..5") — gerçek isim/tema kostüm sanatı tasarlanırken verilecek (yalnız asset
alanı, kod değişikliği gerekmez). 8 atomik commit, Editor'de canlı play-test edildi.

- **Veri**: `CostumeDefinition.characterId` (1-5) eklendi; `CostumeAssetGenerator` yeniden
  yazıldı (15 üretir + eski seti kendisi siler), 150 eski asset silinip c1_1..c5_3 üretildi.
- **Dağılım** (kullanıcı "Gold/Gem dahil + satın alma UI" seçti): 5× Default Standard;
  Advanced'ler ByGold 800 (c1_2) / ByChest (c2_2, c5_2 — bilinçli Uncommon, sandık filtresi
  yalnız Common/Uncommon seçebiliyor) / ByLevel 10 (c3_2) / ByGold 1200 (c4_2); Elite'ler
  ByLevel 20 (c1_3) / ByGem 50 (c2_3) / ByAchievement EFSANE (c3_3, Legendary) /
  ByGem 80 (c4_3) / ByLevel 35 (c5_3). Böylece 15 kostümün TAMAMI bugün fiilen kazanılabilir.
- **Başarım temizliği**: 12 başarımın `rewardCostumeId`'si silinen id'lere işaret ediyordu
  (sessiz no-op olurdu) — EFSANE → c3_3'e bağlandı, kalan 11 temizlendi.
- **ByLevel kostüm auto-grant**: `CostumeManager` artık `OnLevelUp` + Start'ta catch-up
  taraması yapıyor (UnlockManager deseni) — önceden ByLevel kostümler hiç verilemiyordu.
- **Gardırop yeniden yazıldı**: KARAKTER/SİLAH sekmeleri yerine 5 karakter sütunu × 3 kademe;
  kilitli kostümler artık GÖRÜNÜR — Gold/Gem olanlar fiyat pill'i + doğrudan satın alma
  (`TryPurchase`, bakiye yetmezse pasif, `OnCurrencyChanged` ile tazelenir), Level/Sandık/
  Başarım olanlar koşul etiketi. Bir layout bug'ı bulundu ve düzeltildi: `childControl=false`
  layout grubu `LayoutElement`'ı yok sayıp RectTransform boyutunu okur — hücreler 100×100
  varsayılanında kalmıştı, sizeDelta açıkça verildi.
- **Loc**: 15 kostüm ismi + koşul etiketleri 7 dilde — "150 kostüm ismi çevrilmedi" backlog
  kalemi geçersizleşti (yeni set tam çevirili çıkıyor).
- **Play-test (Editor, misafir Lv22 profili)**: 15 kostüm doğru veriyle listelendi; defaults
  (5) + ByLevel catch-up (c3_2, c1_3) = 7 sahipli açılış; c1_2 satın alma −800 Altın ve
  kuşanma çalıştı; c2_3 (50 Gem, bakiye yetersiz) pasif; sayaç 8/15; konsolda yalnız bilinen
  zararsız hatalar (NGO stop-play temizliği, Coplay screenshot artefaktı).
- **Kalan**: kostüm sprite'ları hâlâ placeholder (renk+harf); kuşanılan kostümün oyun içi
  karakter görünümüne yansıması hâlâ bağlı değil — ikisi tek iş olarak kostüm sanatıyla
  birlikte yapılacak (artık 150 değil yalnız 15 görsel gerekiyor). Eski oyuncu save'lerindeki
  c001/c002 id'leri zararsız (IsOwned listede kalır, db'de bulunamaz, hiçbir yol patlamaz).

## Sistem Bağlantı Geçişi — progression/ekonomi zinciri (2026-07-16)
"Ana fikirden sapma / mantık hatası" kontrolünde bulunan kopukluklar: oyunun üç progression
sistemi veri tarafında tamamdı ama oynanışa hiç bağlanmamıştı. Bu geçişte düzeltilenler
(hepsi Editor'de canlı play-test edildi, ayrı atomik commit'ler):

1. **Level artık silah/skill açıyor** (önceden Lv1 oyuncu 10 silahın hepsini kullanabiliyordu —
   `UnlockManager` unlock'ları işliyordu ama hiçbir yer okumuyordu): yeni
   `AbilitySlotCatalog` (slot ↔ itemId eşlemesi, `UnlockManager` yoksa fail-open — Game
   sahnesi Editor'de doğrudan açılırsa kapı devre dışı), kapı tek seçim boğazında
   (`CharacterAbilities.SelectSkill/ConfirmSkill` — klavye + dokunmatik ikisini de kapsar),
   kilitli slot UI'da koyu renk + cephane sayacı yerine "LvN" etiketi. Online'da yalnızca
   yerel oyuncunun kendi inputunu kısıtlar (ekonomi zaten client-authoritative, madde 22).
   Play-test: Lv22 profilde unlock listesi bellekte kırpılıp antrenman maçında kilitli
   slotların Lv2/6/8/10 etiketiyle çizildiği ve `SelectSkill`'in reddettiği doğrulandı.
2. **`UnlockManager` level catch-up taraması**: `OnLevelUp` yalnız canlı artışta çalışıyordu —
   cloud-restore ile gelen seviye (veya UnlockManager yokken kazanılmış seviyeler) hiçbir
   zaman unlock üretmiyordu. `Start()`'ta mevcut seviyeye kadar tüm ByLevel item'lar bir kez
   taranıyor. (Bu tarama, testte bellekten sökülen unlock'ların da kendini onarmasını sağlar.)
3. **Ekonomiye ilk harcama yolu** (önceden `CurrencyManager.Spend`'i çağıran hiçbir UI yoktu —
   Gold sonsuz birikiyordu ve IAP ile satılan Gem'in harcanabileceği tek akış bile yoktu, store
   review açısından da düpedüz tuzaktı): mağazaya SANDIKLAR şeridi — Rare sandık 800 Gold,
   Epic sandık 25 Gem (`ChestManager.TryPurchaseChest`, fiyatlar `ChestConfig`'te; günlük
   galibiyet-sandığı limitinden tamamen bağımsız). Bakiye yetmeyince buton pasif; ödül mevcut
   `RewardPopupManager` toast'uyla düşüyor. Play-test: iki satın alma da gerçek bakiye
   değişimiyle doğrulandı (Rare: −800 Altın; Epic: −25 Gem; günlük sayaç 0'da kaldı).
4. Küçük pürüz: sınırsız cephane (Pistol, -1) tray'de artık "-1" değil "∞".

**Bilinçli ertelendi (kostüm tasarımıyla birlikte, kullanıcı kararı)**: ByLevel/ByGold/ByGem
kostümlerin edinme akışı (`CostumeManager.TryPurchase`'ı çağıran UI yok, ByLevel kostüm grant
edilmiyor) ve kuşanılan kostümün karakter/silah görünümüne yansıması (`GetEquipped`'i oyun içi
okuyan kod yok) — kostüm sprite'ları üretilirken tek iş olarak ele alınacak. Çoklu gezegen
sahneleri de (madde 10 — SampleScene'de 1 gezegen var, `YÖRÜNGE` başarımı mevcut haritada
imkânsız) ayrı iş olarak duruyor.

## Güvenlik/Bug Denetimi — Tam Geçiş (2026-07-15)
Tüm kod tabanı (136 script) güvenlik açığı/bug/eksik davranış için tarandı; bulunan HER şey
düzeltildi ve 15 atomik commit olarak işlendi. **Derleme/play-test borcu kapandı (2026-07-16)**:
derleme temiz, misafir girişi + ana menü + antrenman maçı + mağaza Editor'de hatasız koştu.
Bomb.prefab'ın `GlobalObjectIdHash`'i dosyada hâlâ 0 ama bu diğer çalışan mermi prefab'larıyla
aynı desen (NGO runtime'da üretiyor); Bomb'un gerçek iki-client online ateşleme testi hâlâ
yapılmadı (iki-cihazlı test kalemiyle birlikte, yol haritası madde 2).

Düzeltilenler (commit sırasıyla):
1. `movementLocked` kalıcı kilit: mermi havadayken Tab veya onaylı-ateşlenmemiş silahla süre
   dolması karakteri maç sonuna dek felç ediyordu — tur geçişinde koşulsuz açılıyor.
2. Cloud Save ↔ cihaz-bağlı HMAC çelişkisi: yeni cihaza inen currency.json "kurcalama" sanılıp
   sıfırlanıyor ve sıfır buluta geri yazılıyordu — imza cihazdan bağımsız yapıldı (`SaveIntegrity`),
   eski imzalar bir kez kabul edilip yeniden imzalanıyor.
3. Kupa önbelleği imzalandı (cihaz-bağlı HMAC) — regedit ile kupa şişirip leaderboard'a gönderme
   kapatıldı (asıl otorite hâlâ Cloud Code işi, madde 22).
4. 6 silahın Fire RPC'sine server-side hız clamp'i (`ClampFireVelocity`) — modifiye client
   sınırsız güçte ateş edemez.
5. **Bomb** güvenlik geçişindeki eksik 10. silahtı: ServerRpc/ServerTryConsume(slot 9)/
   NetworkObject.Spawn eklendi, prefab network bileşenleri + DefaultNetworkPrefabs kaydı yapıldı.
6. Client'ta spawned NetworkObject'lere yerel `Destroy` (NGO hatası + desync) —
   `NetworkPhysicsGuard.DespawnOrDestroy` (client'ta görsel kapat, server despawn'ını bekle);
   `ProjectileBase.OnDestroy→SettleOnce` eklendi (DeathBoundary imhası turn sayacını sızdırıyordu).
7. Gezegen tahribatı server-authoritative + senkron: delikler artık her makinede aynı
   pos/yarıçapla `TurnManager.PlanetExplosionClientRpc` üzerinden açılıyor (ayrışma bitti).
8. Ateş sesi her makinede + silah-kullanım başarım kredisi atıcının makinesinde
   (`AbilityBase.AnnounceFire`); roket/el bombası uçuş loop'u client kopyalarında da çalıyor.
9. Ölüm efekti ClientRpc ile her makinede; `Die()` artık NGO senkron bileşenlerini kapatmıyor.
10. Online oyuncu adları/etiketleri: `GravityBody.playerName` (owner-write NetworkVariable) —
    "Player_1 Wins!" yerine gerçek ad, isim etiketi + takım rengi online'da da kuruluyor.
11. Reconnect kimlik doğrulaması: sahipsiz karakter yalnızca aynı UGS PlayerId ile dönene
    devrediliyor (`NetworkIdentityRegistry` + `TurnManager.SubmitIdentityServerRpc`).
12. Online client HUD'ı canlandı: tur sayacı NetworkVariable ile replike; skill paneli her
    makinede KENDİ karakterine bağlanıyor (mobil client silah seçemiyordu); tur pas geçme
    RequestEndTurn RPC'si + TurnTimerUI'da programatik SKIP butonu (host artık rakibin turunu
    Tab ile atlayamıyor).
13. `PlanetClickExploder` (guard'sız debug hile aracı, hiçbir yerde takılı değildi) silindi.
14. IAP: validator kurulamayınca doğrulamanın sessizce kapalı kalması artık release'te de
    hata loguyla görünür.
15. Maç sonu "{0} Wins!"/"Draw!"/"+{0} Gold" metinleri Loc.T'ye bağlandı (6 dil).

Bilinen kalan boşluklar (bu geçişte bilinçli kapsam dışı):
- Kara delik ZONE görselleri/GIF client'ta yok (zone server'da runtime kuruluyor; çekim kuvveti
  `GravityBody.ApplyForce` yönlendirmesiyle zaten doğru çalışıyor — saf görsel eksik).
- İsabet/ıskalama (accuracy) istatistiği online'da hâlâ makine-yerel: `FireShotFired` her
  makinede kendi yerel simülasyonundan ateşleniyor; atıcı kimliğini projectile'a replike etmek
  gerekir (ayrı iş).
- Ekonomi/CloudSave hâlâ client-authoritative (madde 22, Cloud Code planı değişmedi).

## Costumes
Done (2026-07-09) — GARDIROP (Wardrobe) panel added, `CostumeManager` bootstrapped, 150-costume data
generated. Data-complete; still needs real art (see below).

- **`Assets/Scripts/UI/WardrobePanelUI.cs` (new)**: ana menüdeki yeni GARDIROP butonu (sol rayda, MARKET'in
  üstünde) → sahip olunan kostümlerin paneli. KARAKTER/SİLAH sekmeleri, rarity renkli çerçeveli grid kartları
  (sprite yoksa baş harfli rozet fallback — `previewSprite` null-safe), karta dokunmak `CostumeManager.Equip()`
  çağırıp KUŞANILDI etiketini günceller. **Yalnızca sahip olunan kostümler listelenir** — kilitli/satın
  alınmamış hiçbiri hiç görünmez (mağaza/kilit açma akışı kapsam dışı, ayrı bir iş). `QuestsPanelUI` ile aynı
  UiKit programatik Canvas kalıbını izler.
- **`CostumeManager` artık bootstrap ediliyor** (`MainMenuUI.EnsureProgressSingletons()`) — daha önce
  kasıtlı olarak dahil edilmemişti (bkz. eski not), TODO.md yol haritasının 1. maddesiyle birlikte etkinleştirildi.
  `Awake()`'e `GrantDefaultCostumes()` eklendi: `CostumeUnlock.Default` olan kostümler (ör. Gray Soldier,
  Standard Blue) oyuncuya sessizce (ödül popup'ı tetiklemeden) baştan verilir — aksi halde gardırop hep boş
  görünürdü, çünkü hiçbir yer "varsayılan" kostümleri otomatik sahiplendirmiyordu.
- **150 kostüm verisi üretildi**: `CostumeAssetGenerator.cs` (`CosmicRumble/Economy/Generate Costume Assets`)
  Editor menü komutu çalıştırıldı — `Assets/Resources/Costumes/*.asset` (150 `CostumeDefinition`) ve
  `Assets/Resources/Economy/CostumeDatabase.asset` artık projede kalıcı. Rarity dağılımı master spec'e uygun
  (Common/Uncommon/Rare/Epic/Legendary), unlock yöntemleri karışık (Default/ByLevel/ByGold/ByGem/ByChest/
  ByAchievement). **Hâlâ eksik: hiçbir kostümün gerçek sprite'ı yok** (`previewSprite` hepsinde null) — UI
  bunu rarity renkli daire + baş harf ile telafi ediyor, ama gerçek karakter/silah görselleri ayrı, büyük bir
  sanat işi olarak duruyor (bkz. yol haritası madde 9).
- **Play-tested end-to-end in the Unity Editor via Coplay MCP**: misafir girişiyle boot, GARDIROP panelini
  açma, KARAKTER sekmesinde yalnızca 2 sahip olunan kostümün ("Gray Soldier", "Standard Blue", ikisi de
  SIRADAN/Common) göründüğü ve "Sahip olunan: 2 / 86" sayacının doğru olduğu doğrulandı — kilitli 84 kostümün
  hiçbiri listede görünmedi.
- **Bir gerçek bug bulundu ve düzeltildi bu geçişte**: `WardrobePanelUI.Show()` ilk yazımda `Populate()`'ı
  `_panelRoot.SetActive(true)`'dan ÖNCE çağırıyordu. Panel hâlâ inaktifken oluşturulan `TextMeshProUGUI`
  objelerinde `UiKit.BrawlText()`'in `outlineWidth` setter'ı font materyali instance'ı oluşturmaya çalışıyor,
  bu da TMP'nin `OnEnable()`'ının çalışmış olmasını gerektiriyor — inaktif hiyerarşide `OnEnable` ertelendiği
  için `NullReferenceException` (Material.CreateWithMaterial, source null) fırlatıyordu, panel ilk açılışta
  patlıyordu. Sıra değiştirildi (`SetActive(true)` önce, `Populate()` sonra) — düzeltmeden sonra hatasız
  çalıştığı doğrulandı.
- **Kostüm görseli üretimi denendi, askıya alındı (2026-07-11).** Sırayla denenen yollar:
  1. Coplay MCP `generate_or_edit_images` → **401 Unauthorized**. Coplay panelinde (Coplay menüsü →
     Toggle Window → Model Selection) kontrol edildi: hesaba giriş yapılı ama bakiye **$0,0000** —
     Coplay'in AI üretim özellikleri (görsel/ses/3D, hepsi) kullanım başına ücretli ve kredi yok. Görsel
     üretim modeli olarak "Nano Banana Pro" (Google Gemini'nin görsel modeli) seçili duruyor — yani
     Coplay'in arkasında zaten Gemini çalışıyor, sadece Coplay'in kendi faturalandırması üzerinden.
  2. Ücretsiz üçüncü parti hazır asset (Kenney.nl, CC0 — projenin ses/font'ta zaten kullandığı kaynak)
     araştırıldı: silahlar için "Game Icons" paketi uygun görünüyordu, karakterler için "Toon Characters"
     paketi indirilip incelendi (6 arketip: Female/Male adventurer, Female/Male person, Robot, Zombie —
     stil oyunun `player_15.png`'sine yakın ama tam eşleşmiyor, sadece 6 arketip var 10 tema için).
     Kullanıcı kararıyla bu yoldan **vazgeçildi**.
  3. Kullanıcının kendi Gemini erişimiyle (Coplay dışında, doğrudan) görselleri üretip projeye asset
     olarak aktarma teklif edildi — kullanıcı **işi şimdilik askıya aldı**, ilerlemedi.
  **Sonraki oturum için**: kostüm/avatar görselleri hâlâ tamamen eksik (`previewSprite` null). Eğer
  kullanıcı Coplay'e kredi yüklerse madde 1 doğrudan denenebilir; yüklemezse madde 3 (kullanıcının kendi
  Gemini'siyle üretip PNG teslim etmesi) en hızlı yol — bu durumda 10 karakter + 10 silah teması şablonu
  yeterli (isimden renk çıkarıp tonlama otomasyonu ile 150 kostüme dağıtılabilir, plan hazır ama
  uygulanmadı).

## Quests
Done — full quest pool (14 assets: 8 daily / 4 weekly / 2 monthly), `QuestsPanelUI.cs` (Daily/Weekly/Monthly
tabs, progress bars, rewards, reset countdown), and end-to-end gameplay event wiring are all in place and
play-tested.

- `QuestDefinition.cs` gained `requiredId` (filter a tracked event to one specific ability/weapon id, e.g.
  `skill_blackhole`) and `distinctTracking` (progress = count of distinct ids seen, not a running +1) so
  quests like "use every weapon this week" (`weekly_weapons`, target 5 = the 5 weapon ids) and "use 3
  different abilities" (`weekly_abilities`, target 3 of 5 ability ids) are expressible without new code per
  quest. `QuestManager.AdvanceById()` implements both.
- **Found and fixed a bigger pre-existing gap while wiring this up:** almost none of `AchievementEvents`'
  Fire* methods were ever called from gameplay code — only `TurnManager` fired match-level events
  (`FireMatchWon/Lost/Completed/PlayerCountInMatch`). Damage, shots, weapon/ability usage, and planet
  destruction were never reported, so every damage/shot/weapon/ability/planet-based quest *and* achievement
  was dead on arrival regardless of UI. Wired: `CombatEventReporter` (new,
  `Assets/Scripts/Achievements/Core/CombatEventReporter.cs`) centralizes `FireDamageDealt` + a headshot
  heuristic (top half of the target's collider along its own `transform.up`, which `GravityBody` already keeps
  oriented away from the planet surface) from every damage call site (`KineticProjectile`, `Projectile`,
  `HandGrenadeProjectile`, `BombExplosion`, `ProjectileBase`, `BlackHoleZone`). `FireShotFired(isHit)` fires
  once per weapon projectile at resolution (hit or miss/expiry), not at cast time, to avoid double-counting
  shots (the master spec's literal "fire at cast time AND at hit time" wording would have silently halved
  accuracy stats — deliberately deviated from that). `FireWeaponUsed`/`FireAbilityUsed` fire once per
  cast/activation in each of the 9 weapon/ability scripts, using the same id strings `AchievementTracker.cs`
  already expected (`weapon_pistol`, `skill_blackhole`, etc.). `DestructiblePlanet.cs` now tracks remaining
  non-core pixels and fires `FirePlanetDestroyed()` once the destructible mass (outside `minDestructionRadius`)
  is fully cleared.
- **Also found and fixed: none of the economy/achievement singletons were ever instantiated anywhere in the
  project** (`QuestManager`, `CurrencyManager`, `PlayerLevelManager`, `UnlockManager`, `ChestManager`,
  `LoginStreakManager`, `AchievementManager`, `AchievementTracker` had no GameObject in any scene/prefab —
  confirmed via play-mode testing that `QuestManager.Instance` was `null` and the quests panel silently showed
  a fallback message). Added them all to `MainMenuUI.EnsureSingletons()` alongside the existing
  `GameConfig`/`SceneFader`/`AuthManager`/`AudioManager` bootstrap (`CostumeManager` intentionally excluded,
  see Costumes section above). This means achievements were very likely non-functional in any actual playtest
  before this fix too, not just quests.
- Play-tested end-to-end in the Unity Editor via MCP: bootstrap creates all managers, opening the quest panel
  from the main menu shows real quest names/progress/rewards per tab (3 daily / 2 weekly / 1 monthly), tab
  switching works, no runtime errors.

## Localization
Done (2026-07-10) — 7-language system built and wired through every player-facing screen: English
default + Turkish, Chinese (Simplified), Spanish, Japanese, Korean, German. Decision made by the user
after weighing population-based vs. mobile-game-industry-standard language sets; chose the latter
(EN/TR + the 5 languages with the largest mobile-game player bases/revenue).

- **`Assets/Scripts/Localization/LocalizationManager.cs`**: `Language` enum (English, Turkish,
  ChineseSimplified, Spanish, Japanese, Korean, German), singleton with PlayerPrefs persistence,
  defaults to English. `SetLanguage()` reloads the active scene so every programmatically-built UI
  (this project has no prefab-based text, everything is built in code) retranslates on next `BuildUI()`
  pass — same pattern already used for account-switch reloads, not a new mechanism.
- **`Loc.T(string english)`**: the call-site convention across the whole codebase. The English string
  literal itself is the lookup key (no separate ID scheme to keep in sync) — e.g. `Loc.T("QUESTS")`.
  Falls back to English automatically if a translation is missing for the current language, so a
  partially-translated string never renders blank/broken.
- **`LocStrings.cs`** (~150 UI strings) and **`LocContentStrings.cs`** (achievement + quest
  name/description pairs) hold the actual `[tr, zh, es, ja, ko, de]` translation arrays, keyed by
  English text. Split into two files by source (UI code call sites vs. `.asset` data content) —
  `Loc.T()` checks both tables.
- **Converted every UI file** in `Assets/Scripts/UI/` and `Assets/Scripts/Menu/` from hardcoded
  Turkish strings to `Loc.T()` — verified via a project-wide grep sweep for Turkish-only string
  literals (only `[Header]`/`[Tooltip]` Inspector labels and `Debug.Log` diagnostics remain
  Turkish, both developer-only, never player-visible). Also caught and fixed player-visible error/
  status strings living outside UI files: `AuthManager` sign-in/register errors, `FriendsManager`
  friend-request errors, `NetworkBootstrap`/`NetworkPlayerSpawner` reconnect status banner.
- **Found and fixed a real bug along the way**: `FriendsManager.PresenceActivity.status` used the
  literal display strings `"Maçta"`/`"Menüde"` as an internal wire-protocol value shared between
  clients via UGS Friends presence — i.e. the network protocol was coupled to Turkish display text.
  Changed to language-neutral `"in_match"`/`"in_menu"` markers; `SocialPanelUI` now maps these to a
  `Loc.T()`-translated display string instead of comparing against/showing the raw value.
- **Achievement (50) and Quest (14) data was already fully in English** in the `.asset` files before
  this pass (an earlier, undocumented translation pass had already happened, discovered while
  auditing content for this work) — no English authoring needed, only added TR/ZH/ES/JA/KO/DE
  translations keyed by the existing English `displayName`/`description` text.
- **Settings → Account tab** gained a Language row using the same prev/next cycler control already
  used for Resolution/Quality (`MainMenuUI.MakeCycler`) — picks from `LocalizationManager.DisplayName()`
  per language (each shown in its own script, e.g. "Türkçe", "简体中文", "日本語"), calls `SetLanguage()`
  on change.
- **CJK font gap closed (2026-07-10).** Downloaded Noto Sans SC/JP/KR (OFL-licensed, free for
  commercial redistribution — source: `google/fonts` GitHub repo, the canonical distribution point;
  license files kept alongside the source `.ttf`s at `Assets/Fonts/CJK_Source/OFL_*.txt` for
  compliance record-keeping) and generated three **Dynamic-atlas** TMP Font Assets
  (`Assets/Fonts/NotoSansSC SDF.asset`, `NotoSansJP SDF.asset`, `NotoSansKR SDF.asset` — Dynamic mode
  because pre-baking a static atlas for the full CJK glyph set, tens of thousands of characters, isn't
  practical; glyphs are added to the atlas on first use at runtime instead). Added all three to the
  `fallbackFontAssetTable` of both `TitanOne SDF` (headers/buttons, `UiKit.BrawlText`) and
  `LiberationSans SDF` (TMP's default body-text fallback) so any text component picks up CJK glyphs
  regardless of which font it's assigned. **Play-tested end-to-end in the Unity Editor**: switched
  language to Chinese, Japanese, and Korean in turn (via `LocalizationManager.SetLanguage`) and
  screenshotted the main menu each time — real Han/Hiragana-Katakana/Hangul characters render
  correctly (衣橱/ワードローブ/옷장 for Wardrobe, 商店/ショップ/상점 for Shop, etc.), no tofu-box glyphs,
  no console errors beyond the pre-existing benign ones (Coplay's own screenshot-capture artifact,
  NetworkManager scene-reload cleanup). Did NOT use Windows system fonts (Microsoft YaHei/Malgun
  Gothic/Yu Gothic) — those aren't licensed for redistribution in a shipped game, which is why this
  was flagged as needing real sourcing rather than a quick local substitution.
- **Known gap: 150 costume `displayName` strings are English-only**, not yet translated into the other
  6 languages (already wrapped in `Loc.T()` in `WardrobePanelUI.cs`, so this is purely missing table
  entries, not missing code — falls back to English cleanly in the meantime). Deprioritized behind
  core UI/achievement/quest text since costume names are decorative flavor text, not functional UI.

## Antrenman Modu
Done (2026-07-10) — gerçek oyunculara açık, doğrudan erişilebilir pratik modu. Eski "BOT MAÇI (DEV)"
girişi Editor'a kilitli kaldı (bot sayısı seçilebilen, geliştirici test aracı); bu YENİ giriş ayrı ve
her build'de çalışır.

- **`LobbyData.IsTraining`** (yeni flag) + **`TurnManager.isTrainingMode`**: antrenmanda botlar
  `TurnManager.characters` rotasyonuna HİÇ eklenmiyor. `GravityBody.isActive` varsayılan olarak
  `false` (`NetworkVariable<bool>(false, ...)`) ve yalnız `TurnManager.ActivateCharacter()` onu
  `true` yapıyor — rotasyona hiç girmeyen bir karakterde bu asla olmadığı için botlar tasarım
  gereği kalıcı olarak pasif kalıyor (ayrıca "ateş etmeyi engelle" kodu YAZILMADI, zaten var olan
  input-gate deseni yeterli). `TurnManager.CheckGameOver()` normalde `characters.Count <= 1`
  olduğunda maçı bitirir (kazanan ilan eder) — antrenmanda insan tek başına kayıtlı olduğu için bu
  bypass edilmezse maç ilk frame'de biterdi; `isTrainingMode` bayrağı bu kontrolü atlıyor.
- **`MainMenuUI`**: ☰ çekmecesine yeni "ANTRENMAN"/"TRAINING" girişi (`dw_training`,
  `StartTrainingMatch()`) — `LobbyData.IsTraining=true`, `BotCount=2` set edip lobi ekranı
  olmadan doğrudan Game sahnesine geçiyor (tek tıkla pratik, `LobbyPanelUI`'ın aksine).
- **Play-tested end-to-end in the Unity Editor**: misafir girişiyle ana menüye ulaşıp ANTRENMAN'a
  tıklandı, Game sahnesi insan (`Pulsar630`) + `Bot_1` + `Bot_2` ile açıldı, `GameOverPanel`
  inaktif kaldı (maç bitmedi — `isTrainingMode` bypass'ı doğrulandı), ~15 saniye sonra `Bot_1`'in
  pozisyonu birebir aynı ölçüldü (x=18.2469711, y=-10.5347147 → değişmedi, bot hiç hareket etmedi).
  Hata/uyarı yok.
- Antrenmandan çıkış mevcut `InGameMenu` ESC menüsündeki "Ana Menüye Dön" ile — ayrı bir çıkış
  mantığı yazılmadı, zaten var olan yol kullanılıyor.
- Ödül/XP/Gold/başarım verilmiyor (bilinçli): `TriggerGameOver` hiç çağrılmadığı için maç
  tamamlama event'leri ateşlenmiyor — bu, diğer mobil oyunlardaki "antrenman modu ilerleme
  vermez" kuralıyla tutarlı, ayrıca özel bir kısıtlama kodu gerektirmedi (yan etki olarak geldi).

## Profil Avatarları
Done (2026-07-10) — kostüm sistemiyle aynı desende (`Assets/Scripts/Economy/Avatars/`), ama daha
basit: kostümlerin aksine tüm avatarlar baştan açık (unlock/rarity yok), yalnızca "hangisi seçili"
kalıcı. **Gerçek ikon görseli yok** — 150 kostüm/avatar sprite'ı ile birlikte SONRA YAPILACAK
listesine eklendi (aşağıda), şimdilik renk + baş harf placeholder (`AvatarDefinition.icon` null-safe,
UI önceliği zaten ikona veriyor — sprite eklenince otomatik geçiş, kod değişikliği gerekmez).

- **`AvatarDefinition`/`AvatarDatabase`/`AvatarManager`** (kostüm üçlüsüyle birebir aynı kalıp):
  `AvatarManager.Select(id)` seçimi `avatar.json`'a kaydeder (CostumeManager'ın `costumes.json`'ı
  gibi), `OnAvatarChanged` event'i yayınlar.
- **`Assets/Editor/AvatarAssetGenerator.cs`**: uzay temalı 16 avatar (Nova, Comet, Blaze, Nebula,
  Pulsar, Quasar, Meteor, Orbit, Solstice, Eclipse, Vortex, Cosmos, Photon, Asteroid, Aurora,
  Zenith), her biri ayrı placeholder renkte — `CostumeAssetGenerator.cs`'in küçük ölçekli eşdeğeri.
- **`AvatarPickerUI.cs`**: `WardrobePanelUI`/`QuestsPanelUI` ile aynı grid kalıbı, 4 sütunlu ızgara,
  seçili avatar yeşil kontur + "SEÇİLİ" etiketiyle işaretli.
- **Üst bar entegrasyonu**: `MainMenuUI`'daki profil plakasının avatar dairesi artık oyuncu adının
  ilk harfi yerine seçili avatarın rengini/harfini gösteriyor; dairenin köşesine kendi
  Button/raycast hedefi olan küçük bir "+" rozeti eklendi (plakanın geri kalanı hâlâ Sıralama'yı
  açıyor, yalnızca bu küçük alan `AvatarPickerUI.Show()` çağırıyor) — çakışan tıklama bölgesi riski
  olmadan iki farklı eylem aynı plakada bir arada.
- **Canlı güncelleme düzgün bağlandı**: ilk yazımda üst bar yalnızca `BuildUI()` sırasında bir kez
  kuruluyordu, seçim değiştiğinde menü yeniden açılana kadar güncellenmiyordu — fark edilip
  `MainMenuUI.OnAvatarChangedForTopBar` + `ApplyAvatarVisuals()` eklendi (`AvatarManager.OnAvatarChanged`'a
  abone), artık seçim anında (sahne reload'u olmadan) yansıyor.
- **Play-tested end-to-end in the Unity Editor**: misafir girişiyle menüye ulaşıldı, varsayılan
  avatar (Nova, kırmızı/pembe "N") üst barda doğrulandı, avatar seçici açılıp "Meteor" seçildi,
  seçicideki SEÇİLİ rozeti Nova'dan Meteor'a taşındığı doğrulandı, üst bardaki `Initial.text`
  ve `Avatar.Image.color`'ın `get_game_object_info` ile Meteor'un tanımlı değerleriyle
  (`"M"`, `RGB(0.85, 0.25, 0.55)`) birebir eşleştiği doğrulandı — canlı güncelleme çalışıyor.
  Hata/uyarı yok (yalnızca bilinen zararsız Coplay/NetworkManager artifact'leri).

## ui_button_hover wiring
Done (2026-07-10) — `UiKit.Hover(GameObject)` + `UiHoverSound` (yeni, `IPointerEnterHandler`, `Assets/Scripts/UI/UiKit.cs`)
eklendi ve `UiKit.Press()`in yanına, mevcut tüm buton oluşturma yerlerine (29 GameObject, 14 dosya)
tek tek eklendi. Devre dışı (`Selectable.IsInteractable() == false`) butonlarda sessiz kalır.

- Programatik buton oluşturan tüm dosyalar (`MainMenuUI`, `InGameMenu`, `WardrobePanelUI`,
  `SocialPanelUI`, `ShopPanelUI`, `QuestsPanelUI`, `OnlineLobbyPanelUI`, `LoginScreenUI`,
  `LoginPanelUI`, `LobbyPanelUI`, `LeaderboardPanelUI`, `InvitePopupUI`, `FriendLobbyPanelUI`,
  `AvatarPickerUI`) gözden geçirildi — `AddComponent<Button>()` çağrılan 30 yerin 29'una
  `UiKit.Hover(go)` eklendi. **Bilinçli olarak atlanan tek yer**: `MainMenuUI`'daki çekmece
  arka planı kapatma butonu (`dimGO`) — görünmez tam ekran tıklama yakalayıcısı, hover'ı
  anlamlı bir buton değil.
- **Test**: Play Mode'da misafir girişiyle ana menüye ulaşıldı, `btn_wardrobe` üzerinde
  `ExecuteEvents.Execute(..., pointerEnterHandler)` ile pointer-enter simüle edildi,
  `AudioManager`'ın loop olmayan (SFX) `AudioSource`'unun `isPlaying` durumu hover öncesi
  `False`, hover sonrası `True` olarak doğrulandı — klip gerçekten çalıyor. Konsolda hata yok.

## Sosyal Başarımlar (kontrol + eksik event wiring)
Done (2026-07-10) — SOSYAL kategorisindeki 10 başarımdan 3'ü (`SOSYAL_KELEBEK`, `HERKESE_MEYDAN`,
`DUELLO_SAMPIYONU`) çalışır hale getirildi; kalan 6'sı ayrı, daha büyük iş olarak bilinçli kapsam
dışı bırakıldı (aşağıda tek tek gerekçelendirildi).

- **Gerçek bug bulundu ve düzeltildi**: `AchievementEvents.FirePlayerDefeated(string id)` tüm
  kod tabanında hiçbir yerden çağrılmıyordu — event ve `AchievementTracker.HandlePlayerDefeated`
  aboneliği "bağlı" görünüyordu ama tetikleyen hiçbir kod yoktu, yani `SOSYAL_KELEBEK` sessizce
  ölüydü. `CombatEventReporter.ReportHit()` her çağrıldığında hedefin `TakeDamage()`'ı zaten önceden
  çalışmış oluyor (bkz. metod içi yorum) — bu sıralamadan yararlanılarak isabet sonrası
  `ch.GetCurrentHealth() <= 0f` kontrolüyle öldürücü darbe tespit edildi ve `FirePlayerDefeated`
  oradan tetiklendi. Saldırgan kimliğini tüm projectile boru hattına taşımak gibi çok daha büyük bir
  refactor'a girmeden çözüldü.
- **Yeni event: `AchievementEvents.OnDamagedTarget`/`FireDamagedTarget(string id)`** — her isabette
  (öldürücü olsun olmasın) hedef kimliğini yayınlıyor. `HERKESE_MEYDAN` ("aynı maçta 7 farklı
  oyuncuya hasar ver") artık `AchievementTracker._matchDamagedTargets` (maç başına sıfırlanan
  `HashSet<string>`) ile takip ediliyor, 7'ye ulaşınca `UnlockAchievement` çağrılıyor.
  `ResetMatchState()`'e temizleme eklendi.
  Davetli özel maç da Quick Match ile birebir aynı `CombatEventReporter`/`TurnManager` boru hattını
  kullandığından ayrı bir kod yolu yok — event kaynağı maç tipinden bağımsız.
- **`DUELLO_SAMPIYONU` ("1v1 modda 10 galibiyet") bağlandı**: `AchievementTracker.HandleMatchWon()`
  içinde `_matchPlayerCount == 2` kontrolü ile kümülatif `_totalDuelWins` sayacı artırılıp
  `UpdateProgress` çağrılıyor — ayrı bir "duel modu" event'i gerekmedi, mevcut
  `OnPlayerCountInMatch` verisi yeterliydi.
- **Test**: Play Mode'da geçici bir script (`Temp_TestSocialAchievements.cs`, sonradan silindi)
  ile üç senaryo da doğrudan event ateşleyerek doğrulandı — 7 farklı `FireDamagedTarget` çağrısı
  sonrası `HERKESE_MEYDAN` unlock oldu, bir `FirePlayerDefeated` sonrası `SOSYAL_KELEBEK` ilerlemesi
  0'dan 1'e çıktı, 2 oyunculu `FireMatchWon` sonrası `DUELLO_SAMPIYONU` ilerlemesi 0'dan 1'e çıktı.
  Konsolda hata/uyarı yok.
- **Kalan 6 SOSYAL başarımın 5'i bağlandı (2026-07-10)** — yalnızca `OGRETMEN` gerekçeli kapsam
  dışı kaldı (aşağıda). Önceki not (bu satırın eski hali) `REKABETCI`/`OGRETMEN` gerekçelerini
  birbirine karıştırmıştı — gerçek `.asset` açıklamaları kontrol edilerek düzeltildi.
  - **`INTIKAM`** ("Defeat whoever killed you in the next match"): saldırgan kimliğini projectile
    boru hattına taşımaya gerek kalmadı — proje her zaman 1v1 olduğu için "bizi kim yendi" sorusunun
    cevabı zaten maçın tek rakibi. `AchievementEvents.FireMatchLost(string winnerName)` (önceden
    parametresizdi) kaybedilen maçın kazananının adını `AchievementTracker`'a taşıyor,
    `PlayerPrefs["cr_intikam_target"]`'e yazılıyor; bir sonraki maçta `HandlePlayerDefeated` aynı
    isimle eşleşirse `INTIKAM` unlock olup hedef temizleniyor.
  - **`REKABETCI`** (gerçek açıklaması "Finish top 3 in a ranked match" — tutorial ile ilgisi yok):
    proje kesinlikle 1v1 olduğu için (`MaxPlayers=2` her SessionOptions'ta) 2 oyuncudan biri olmak
    zaten her zaman "top 3" içinde — sahte bir 3+ kişilik sıralama sistemi uydurmak yerine dereceli
    (Quick Match) bir maç kazan/kaybet fark etmeden tamamlandığında doğrudan unlock ediliyor
    (`AchievementEvents.OnRankedMatchCompleted`, `TurnManager.FinishMatchLocally`'ye eklenen
    `ranked` parametresinden tetikleniyor).
  - **`KOZMIK_EKIP`** ("Play 5 matches with the same 3 people" — 1v1'de "aynı 3 kişi" anlamsız,
    "aynı arkadaşla 5 maç" olarak ölçeklendirildi): `FriendLobbyPanelUI.ShowAsHost/ShowAsClient`
    artık arkadaşın `PlayerId`'sini `LobbyData.FriendOpponentId`'e yazıyor (client tarafı için
    `InvitePopupUI.HandleInvite`'ın zaten aldığı `senderId` `ShowAsClient`'a yeni bir parametre
    olarak eklendi); maç bitince `TurnManager.FinishMatchLocally` bunu okuyup
    `AchievementEvents.FireFriendMatchCompleted(friendId)` ateşliyor ve alanı temizliyor (iptal/
    arka plana atma durumlarında da `OnCancelClicked`/`CleanupOnBackground`'da temizleniyor —
    aksi halde bir sonraki alakasız maça sızardı). `AchievementTracker` her arkadaş için ayrı
    `PlayerPrefs` sayacı tutuyor, ilerleme o arkadaşın sayacının en yükseği (başka bir arkadaşla
    oynamak ilerlemeyi düşürmüyor).
  - **`BIR_NUMARA`/`KOZMIK_AVCI`** (leaderboard sıralaması): eski not "senkron erişim yok"
    diyordu ama achievement kontrolünün senkron olmasına hiç gerek yoktu — `LeaderboardManager.
    SubmitScoreAsync` skor gönderildikten sonra `FetchOwnEntryAsync()` ile (zaten var olan metod)
    async sıralamayı öğrenip `AchievementEvents.FireLeaderboardRankKnown(rank)` ateşliyor;
    `AchievementTracker` rank==0'da `BIR_NUMARA`, rank<10'da `KOZMIK_AVCI` unlock ediyor.
  - **Test**: Play Mode'da (`TestSocialAchievements.cs`, geçici, sonradan silindi) her 5 event
    doğrudan ateşlenerek doğrulandı — `INTIKAM`/`REKABETCI`/`BIR_NUMARA`/`KOZMIK_AVCI` unlock,
    `KOZMIK_EKIP` ilerlemesi 5/5 ve unlock. Konsolda yeni hata yok (yalnızca bilinen zararsız NGO
    stop-play temizlik hataları).
  - **`OGRETMEN` kapsam dışı (gerekçeli)**: gerçek açıklaması "Guide a new player through the
    tutorial" — mentor'un kendi cihazında, davet ettiği arkadaşın TutorialManager'ını tamamladığını
    öğrenmesi gerekiyor. Bu, mentee'nin cihazındaki yerel `PlayerPrefs` durumunu mentor'un cihazına
    taşıyacak bir cross-client bildirim ister (FriendsService.MessageAsync ile teknik olarak
    mümkün görünüyor ama gerçek iki-taraflı akış, orijinal multiplayer milestone'da olduğu gibi
    ayrı bir gerçek ikinci OS process'i gerektirir — bu geçişte test edilemedi). Ayrıca
    `TutorialManager` bilinçli olarak yalnızca offline (hotseat/Antrenman) akışında tetikleniyor
    (bkz. "Antrenman Modu" bölümü üstündeki tutorial notu), online arkadaş daveti akışına henüz
    bağlanmadı — o da ayrı bir iş. Kendi başına ayrı bir alt sistem (cross-client bildirim) ve ayrı
    bir test ortamı (iki gerçek process) gerektirdiğinden bu geçişte yapılmadı.

## Audio
Done — all 21 SFX + `menu_music` generated (ElevenLabs SFX for SFX, a separate AI music tool for the loop
track since ElevenLabs SFX isn't built for long loops), placed in `Assets/Resources/Audio/{SFX,Music}/`, and
play-tested end-to-end in the Unity Editor (Resources.Load finds every clip, AudioManager plays/loops them,
no console errors).

- `AudioManager.cs` was rewritten to load clips by id from `Resources/Audio/{SFX,Music}/{id}` instead of
  requiring manual Inspector drag-and-drop. Missing files are a silent no-op (cached as null so it doesn't
  retry `Resources.Load` every call), so drop-in works incrementally — add one file, it plays; nothing else
  breaks in the meantime.
- All 9 weapon/ability `Fire()` sites, 4 explosion call sites (`ProjectileBase`, `Projectile`/RPG,
  `HandGrenadeProjectile`, `BombExplosion`), `DestructiblePlanet` (planet fully destroyed), and `TurnManager`
  (match win/lose) now call `AudioManager.Instance?.PlaySfx("...")`. Menu click/hover already wired
  (`PlayClick()`/`PlayHover()` — `PlayHover()` itself works but nothing calls it yet, no button has
  pointer-enter wiring; out of scope, separate task if wanted).
- **Explosive weapons (RPG, HandGrenade, Bomb) got a 3-stage sound treatment** — fire/throw → in-flight loop
  → impact — since a single "fire" clip wasn't enough to sell a rocket/grenade/bomb actually traveling.
  Added `AudioManager.PlayLoopingSfxOnObject(GameObject, clipId)`: attaches an `AudioSource` to the projectile
  itself and loops the clip for as long as the projectile is alive (dies with it automatically, no explicit
  stop needed — acceptable minor cutoff on impact). Pistol/Shotgun (`KineticProjectile`, non-explosive
  single-hole-punch weapons) were deliberately left out of this — fire sound only, no flight/impact stage,
  since they don't explode and a whoosh-per-bullet felt like the wrong fidelity for that weapon type.
  - **HandGrenade is the special case**: unlike RPG/Bomb it does NOT explode on first contact — it has a
    `delayBeforeExplosion` fuse timer, so it can bounce off terrain multiple times before detonating.
    `HandGrenadeProjectile.cs` gained an `OnCollisionEnter2D` bounce detector (debounced via
    `bounceSfxCooldown` + `minBounceSpeed` so rapid low-speed rolling doesn't spam the sound) that plays
    `grenade_bounce` on every real bounce, separate from `projectile_flight_grenade` (loop, plays throughout)
    and `explosion_small` (plays once, on fuse timeout).
  - Bomb also gets a flight loop even though `BombBehaviour.OnCollisionEnter2D` detonates on first contact
    (no bounce phase) — just for the brief airborne moment between throw and impact.
- Coplay MCP's AI audio generation (`generate_sfx`/`generate_music`) returned 401 Unauthorized — needs
  Coplay account credits/Professional subscription, not available in this session. Decided to source files
  externally instead (free libraries like freesound.org/Kenney/Zapsplat/Mixkit, or another AI tool) and just
  drop them in.
- **Manifest — exact filename (no extension shown; `.wav` or `.mp3` both work), folder, and what's needed:**

  `Assets/Resources/Audio/SFX/`
  | id | sound |
  |---|---|
  | `weapon_pistol_fire` | sharp kinetic pistol shot |
  | `weapon_shotgun_fire` | shotgun blast, multiple pellets |
  | `weapon_rpg_fire` | rocket launch whoosh |
  | `projectile_flight_rocket` | **loop** — rocket flying through the air |
  | `weapon_grenade_throw` | pin-pull + throw whoosh |
  | `projectile_flight_grenade` | **loop** — grenade tumbling through the air |
  | `grenade_bounce` | one-shot — grenade bouncing off terrain |
  | `weapon_bomb_place` | mechanical drop/arm beep |
  | `projectile_flight_bomb` | **loop** — bomb briefly airborne after being thrown |
  | `skill_blackhole_activate` | vortex suction whoosh, deep bass |
  | `skill_teleport` | warp zap whoosh |
  | `skill_shield_activate` | energy shield hum/shimmer |
  | `skill_bathammer_swing` | heavy swing + metallic impact |
  | `skill_superjump` | energy charge + launch whoosh |
  | `explosion_small` | grenade/generic ability explosion |
  | `explosion_large` | RPG/bomb explosion, deeper boom |
  | `planet_destroyed` | big rumble/crumble, planet fully cleared |
  | `match_win` | short victory fanfare |
  | `match_lose` | short defeat stinger |
  | `ui_button_click` | crisp UI click |
  | `ui_button_hover` | soft UI hover blip |

  `Assets/Resources/Audio/Music/`
  | id | sound |
  |---|---|
  | `menu_music` | loopable ambient sci-fi menu background music |

  The old `Assets/Audio/bomb_Explosion.mp3` is NOT under a `Resources/` folder so `AudioManager` can't find
  it — either move/rename a copy into the manifest above, or leave it (unused, harmless).

## Achievement platform providers
- `SteamAchievementProvider`, `GooglePlayAchievementProvider`, `AppStoreAchievementProvider`
  (`Assets/Scripts/Achievements/Providers/`) now have real SDK-calling implementations instead of log-only
  stubs. `LocalAchievementProvider` is still the only one that persists locally (unchanged — it's the source
  of truth; platform providers only *report* to the storefront).
  - **AppStore** is fully live as-is: it uses Unity's built-in `UnityEngine.SocialPlatforms.GameCenter`, which
    ships with the engine — no package to install.
  - **Steam** uses the real Facepunch.Steamworks API (`SteamClient.Init`, `SteamUserStats.Achievements[].Trigger()`,
    `IndicateAchievementProgress`, `StoreStats`), but is gated behind a `STEAMWORKS_INSTALLED` scripting define
    so the standalone build keeps compiling before the package is added. To activate: add the OpenUPM scoped
    registry (`https://package.openupm.com`, scope `com.facepunch.steamworks`) in Package Manager, install the
    package, register a real App ID in the Steamworks partner portal (replace the placeholder `AppId = 480`
    test ID in `SteamAchievementProvider.cs`), then add `STEAMWORKS_INSTALLED` under Player Settings →
    Scripting Define Symbols (Standalone). Each achievement's Steamworks Admin API name must match the
    corresponding `AchievementDefinition.id` exactly.
  - **Google Play** uses the real Play Games Services v2 API (`PlayGamesPlatform.Activate()`,
    `Social.ReportProgress`), gated behind `GPGS_INSTALLED` for the same reason (Google ships this plugin as a
    `.unitypackage`, not a clean UPM package).
    - **Done (2026-07-06) — plugin installed, code-side half of the setup is complete.** Downloaded
      `GooglePlayGamesPlugin-2.1.0.unitypackage` directly from the repo's own `current-build/` folder
      (`github.com/playgameservices/play-games-plugin-for-unity`, the exact source already named here) and
      imported it via `AssetDatabase.ImportPackage(path, false)` — added `Assets/GooglePlayGames/` (the plugin
      itself, 59 `.cs` files), `Assets/ExternalDependencyManager/` (Google's EDM4U dependency resolver, a
      prerequisite the package brings in on its own), and `Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/`.
      Added `GPGS_INSTALLED` under Player Settings → Scripting Define Symbols (**Android** specifically, via
      `PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, ...)` — confirmed persisted in
      `ProjectSettings/ProjectSettings.asset`). Compiles clean, no errors.
    - **Not done, and can't be done without the user's own Play Console account:** no app is registered in Play
      Console yet, so there are no real achievements or a real resource XML to configure. Remaining steps,
      all requiring the user's own Google Play Developer account:
      1. Register the app in Play Console (package name, Play Games Services enabled for it).
      2. Create each achievement there — **Play Console generates its own opaque achievement ID** for each one
         (e.g. `CgkI27iow...`), not a human-readable string like Steam's Admin API names. This means
         `GooglePlayAchievementProvider.UnlockAchievement(id)`/`UpdateProgress(id, ...)` — which currently just
         forwards whatever `AchievementDefinition.id` string is passed straight to `Social.ReportProgress` —
         will need a lookup mapping from our internal ids (`"ROKETCI"`, etc.) to Play Console's real opaque IDs
         once those exist; not implemented since there's nothing to map to yet. A small `Dictionary<string,
         string>` in the provider is the obvious shape once the real IDs are known.
      3. Run **Window → Google Play Games → Setup → Android Setup** in the Editor with the resource XML
         downloaded from Play Console → Play Games Services → Configuration, once step 1/2 exist.
      Everything else (the plugin, the define, the API calls) is ready and waiting on those three steps.

## Google Play Games GİRİŞİ — Console/Dashboard kurulumu (yapılmadı, kod hazır)

2026-07-08/09'daki sosyal+auth yenilemesinin (tam ekran login, Platform + Cosmic ID modeli,
UGS Friends, davet lobisi) kod tarafı tamamen bitti ve Editor'da doğrulandı. Android'de Google
girişinin fiilen çalışması için kalan adımların HEPSİ kullanıcının kendi Google hesaplarını
gerektiriyor — kod değişikliği gerekmiyor:

1. **Play Console'da uygulama kaydı** (taslak yeterli) + **Play Games Hizmetleri kurulumu**
   (Büyüme → Play Games Hizmetleri → Yapılandırma → "Hayır, yeni oyun kur"; verilen 12 haneli
   oyun kimliğini not et). Yukarıdaki "Achievement platform providers" bölümünün 1. adımıyla
   aynı kayıt — bir kere yapılır, ikisine de yarar.
2. **Android kimliği** (Credentials → Android): paket adı + APK'yı imzalayan keystore'un SHA-1'i.
   Play App Signing anahtarı ile lokal test keystore'unun SHA-1'leri farklıysa İKİ ayrı Android
   kimliği eklenmeli (en sık takılınan nokta: SHA-1 uyuşmazlığında giriş sessizce başarısız olur).
3. **Oyun sunucusu kimliği** (Credentials → Game server): Google Cloud Console'da **Web
   uygulaması** tipi OAuth istemcisi oluştur (Android tipi DEĞİL; ilk seferde OAuth izin ekranı
   da doldurulur — Test modundaysa kendi Gmail'i test kullanıcılarına eklenmeli). Çıkan
   **Client ID + Client Secret** kopyalanır.
4. **Unity Dashboard** → Player Authentication → Identity Providers → **Google Play Games** →
   3. adımdaki Web Client ID + Secret girilir → Enable. (Unity tarafındaki tek adım; Unity
   hiçbir kimlik üretmez, sadece Google'ın verdiğini bildirirsin.)
5. **Play Games Hizmetleri → Test kullanıcıları**: PGS yayınlanana kadar sadece bu listedekiler
   giriş yapabilir — kendi hesabını ekle.
6. **Unity Editor'da bir kere**: Window → Google Play Games → Setup → Android Setup — Play
   Console "Kaynakları al" XML'i + 3. adımdaki WEB Client ID (yukarıdaki achievements bölümünün
   3. adımıyla aynı sihirbaz, tek seferde ikisi de biter).
7. **Cihaz testi**: 2. adımdaki keystore ile imzalı build → beklenen akış: açılışta sessiz
   Google girişi → yükleme → menü; Ayarlar → Hesap'ta "GOOGLE — Bağlı (ad)".

Kod tarafında hazır bekleyenler: `GooglePlayAuthProvider` (sessiz/etkileşimli auth code),
`AuthManager.SignInWithPlatformAsync` (Link/SignIn + AccountAlreadyLinked hesap değişimi),
LoginScreenUI'daki "GOOGLE İLE DEVAM ET" butonu (`UNITY_ANDROID && GPGS_INSTALLED`).

Ayrıca ertelenen: **iOS Game Center girişi** — `AppleGameCenterAuthProvider` stub olarak duruyor
(IsAvailable=false); iOS hattı kurulunca Apple.GameKit plugin'i + `SignInWithAppleGameCenterAsync`
ile doldurulacak. **Arkadaş/davet uçtan uca testi** de iki gerçek kimlik gerektirir (Editor'daki
testuser1 + ikinci cihaz/build'de ikinci Cosmic ID) — Editor'da tek taraflı doğrulandı
(Friends init + kendi kodu "Pulsar630#51647" UGS'den geldi), iki taraflı akış cihaz testinde.

## Multiplayer

### Done (2026-07-04) — Milestone 1: two-client connection + turn sync, verified end-to-end
Scope was deliberately tiny and explicitly agreed with the user first: two players connect over the network
and correctly synchronize whose turn it is. No ability firing, no projectile sync, no damage sync — those are
later phases (see "Still a large, separate future effort" below, unchanged in scope).

**Stack:** `com.unity.netcode.gameobjects` (NGO 2.13.0) for replication, `com.unity.services.multiplayer`
(2.2.4, the current unified Session API) for host/join — `MultiplayerService.Instance.CreateSessionAsync(new
SessionOptions{MaxPlayers=2}.WithRelayNetwork())` returns a real join code and **automatically starts NGO's
NetworkManager as host** (confirmed empirically via reflection + live test — no manual Relay allocation/UTP
wiring needed), `JoinSessionByCodeAsync` does the same for the client side. Both packages installed under the
same already-linked UGS project (org `eren-zcan`) that Auth/Cloud Save already use.

**Files added:** `Assets/Scripts/Networking/NetworkBootstrap.cs` (Host/JoinSessionAsync wrapper, reuses
`CloudSaveManager`'s existing UGS init/sign-in — doesn't re-initialize), `Assets/Scripts/Networking/
NetworkPlayerSpawner.cs` (server-only, spawns one Player prefab per connected client via the existing
`SpawnPositioning.CalculateSpawnPositions()`, calls `TurnManager.RegisterPlayers()` + a new
`TurnManager.BeginMatch()`), `Assets/Scripts/UI/OnlineLobbyPanelUI.cs` (new "ONLINE" main-menu button, separate
from the existing local-hotseat "PLAY"/`LobbyPanelUI.cs` which stays untouched — Host shows a join code and
waits, Join takes a code, both wait for the host to trigger the Game scene load once 2 clients are connected).

**Files modified for networking, all narrowly scoped:**
- `GravityBody.cs`: `MonoBehaviour` → `NetworkBehaviour`; `isActive` (plain bool, single writer = TurnManager,
  confirmed via full codebase search before touching it) → `NetworkVariable<bool>`
  (`WritePermission.Server`). Its own `Update()` (WASD/jump input) gated with `if (!isActive.Value ||
  (IsSpawned && !IsOwner)) return;` — **found and fixed a gap the initial research missed**: `GravityBody`
  reads raw `Input.*` directly (not just `PlayerController2D`/`AbilityBase` as first assumed), so without this
  fix, once it became the other player's turn, *both* clients' keyboards would have independently tried to
  drive that shared character. The `IsSpawned` half of the guard is what keeps offline hotseat mode
  (`NetworkManager` never listening, so `IsSpawned` is always false there) working exactly as before — the
  ownership check only ever activates when actually networked.
- `AbilityBase.cs` and `PlayerController2D.cs`: same `.Value` + `IsSpawned`/`IsOwner` gating pattern.
- `BatHammerSkill.cs`: same fix — a second, separate `gravityBody.isActive` read (Keypad8 alt activation key)
  that the initial research pass also didn't catch; found while fixing the resulting compile errors.
- `TurnManager.cs`: `MonoBehaviour` → `NetworkBehaviour`; `Update()` and `Start()` gated with `if (IsSpawned &&
  !IsServer) return;` (same offline-safe pattern as above). The old `Start()`-only match kickoff was split into
  a public `BeginMatch()` — offline hotseat still calls it from `Start()` once `characters` is populated by
  `GameInitializer`; online, `Start()` finds an empty `characters` list (players haven't spawned yet at scene-load
  time) and harmlessly no-ops, then `NetworkPlayerSpawner` calls `RegisterPlayers()` + `BeginMatch()` itself once
  both clients are actually connected. `TurnManager` needed a `NetworkObject` component (scene-placed in
  `SampleScene`, not dynamically spawned, so NGO's scene management syncs it identically for host and client).
- `GameInitializer.cs`: single additive guard — `if (NetworkManager.Singleton != null &&
  NetworkManager.Singleton.IsListening) return;` at the top of `Start()`, so the existing local-spawn path
  (human + test bots) is skipped entirely when an online session is driving the match instead.
- `Player.prefab`: gained a `NetworkObject` component (root). NGO auto-detected and auto-registered it in
  `Assets/DefaultNetworkPrefabs.asset` — no manual prefab-list wiring needed.

**Verified — genuinely two separate OS processes, not two script calls in one Editor:** Unity Editor (driven
via Coplay MCP) as host, a real standalone Windows dev build (`Builds/DevClient/CosmicRumble.exe`, gitignored)
launched as a second process for the client, connecting through the *actual* Host/Join UI flow (a join code
generated by clicking "HOST OLUŞTUR" in the Editor, fed to the standalone build, which called the real
`JoinSessionAsync` path — not a raw-loopback shortcut). Confirmed via logs on **both sides independently**:
```
Host:   [TURN] Player_0 isActive False->True IsOwner=True  frame=1201   (host's own turn starts)
Host:   [TURN] Player_0 isActive True->False  IsOwner=True  frame=1451
Host:   [TURN] Player_1 isActive False->True  IsOwner=False frame=1451  (client's turn starts, host doesn't own it)
Client: [TURN] Player_0 isActive False->True  IsOwner=False frame=542   (host's turn, client doesn't own it)
Client: [TURN] Player_0 isActive True->False  IsOwner=False frame=2567
Client: [TURN] Player(Clone) isActive False->True IsOwner=True frame=2567  (client's OWN turn — correct ownership)
Client: [TURN] Player(Clone) isActive True->False IsOwner=True frame=4722
Client: [TURN] Player(Clone) isActive False->True IsOwner=False frame=4722 (back to host's turn)
```
Both processes agree on turn order and each correctly sees `IsOwner=True` only for its own character — this was
the actual bar for Milestone 1, proven independent of any visual/position sync (none exists yet, see below).

**Known limitations, all expected/out of scope for this milestone, not bugs:**
- Ability firing and projectiles are not networked — each client only sees its own local `Instantiate()` calls,
  invisible to the other player. If the *non-host* client fires a weapon, `TurnManager.NotifyProjectileLaunched()`
  runs on their machine and would try to set `gb.isActive.Value` on a server-write-only `NetworkVariable` from a
  non-server client — not exercised by this milestone's test (turn sync only, no firing), but expect an NGO
  permission error/no-op if tried before ability sync is implemented.
- No `NetworkTransform` — a character's position only visibly updates on its own client during its own turn; the
  other client sees it stay at spawn position. Turn-alternation correctness doesn't depend on this.
- `AutoJoinFromCmdLine.cs` and `NetSmokeTestAutoClient.cs` were temporary verification-only scripts (command-line
  auto-join / auto-connect, since the standalone build's UI can't receive simulated clicks from the Editor
  process running the MCP tooling) — both were deleted after use, not part of the shipped code.
- Along the way, found and fixed two **pre-existing, unrelated** compile bugs that only surface on an actual
  Standalone Player build (invisible in Editor Play Mode, where `UNITY_EDITOR` is always defined): `SpawnDebugger.cs`
  had a `#if UNITY_EDITOR`/`#endif` splitting a single multi-line statement in half (now the whole class is
  wrapped, matching its own "debug only, don't ship" doc comment); `AppStoreAchievementProvider.cs` had an
  unguarded `using UnityEngine.SocialPlatforms.GameCenter;` even though the class body below was correctly
  `#if UNITY_IOS`-gated. A proactive full-codebase audit (95 `#if UNITY_EDITOR`/platform-define occurrences)
  found no further instances of this pattern.
- **Bigger discovery along the way:** the project's old path (`OneDrive\Masaüstü\...`, non-ASCII `ü`) crashed
  Unity's Input System build-pipeline code (`Assembly.GetCodeBase()`, a known Mono/Unity limitation with
  non-ASCII paths) on *every* Standalone Player build attempt — not a Coplay-tooling issue, a real Unity engine
  bug that would have blocked Steam/Windows builds later too. Fixed by moving the whole project (git history
  intact) to `C:\Projects\CosmicRumble` — ASCII-only, no longer under OneDrive sync. This was necessary
  regardless of multiplayer; multiplayer work is just what surfaced it first.

### Done (2026-07-05) — Milestone 2: networked ability firing + damage authority, verified end-to-end
Scope (agreed with user): network-sync the "simple flying projectile" ability family — Pistol, Shotgun, Rpg,
HandGrenade — plus the damage-authority fix needed to make any of it correct (without it, a hit would apply
once per connected machine). `BlackHoleSkill`/`Teleport`/`ShieldSkill`/`BatHammerSkill` are each a genuinely
different sync problem and stay out of scope, still local-only, same as before.

**All code changes below are implemented and compile clean. Individually verified pieces (each confirmed
working in isolation, offline and/or via a partial live 2-process test):**
- `Pistol_Bullet_Projectile.prefab` fixed to have `KineticProjectile` at the prefab-asset level instead of a
  runtime `Destroy(Projectile)+AddComponent(KineticProjectile)` swap (that swap would only ever have run on the
  server once firing became RPC-driven, leaving every other peer's replica on the wrong component). Verified:
  offline Pistol/Shotgun fire identically to before.
- `TurnManager.NotifyProjectileLaunched()`/`NotifyProjectileSettled()` gained `if (Instance.IsSpawned &&
  !Instance.IsServer) return;` — without this, every peer's own local projectile physics would have
  independently mutated turn state once projectiles became networked (a bug this milestone's own change would
  have introduced, caught during planning, not by accident later).
- `CharacterHealth` → `NetworkBehaviour`, `currentHealth` → server-written `NetworkVariable<float>`,
  `TakeDamage` no-ops on non-server peers when spawned. `HealthBarUI` needed no changes (only calls
  `GetCurrentHealth()`, still returns `float`). Verified offline: health before=100 after=85 for a 15-damage hit,
  identical to pre-change behavior.
- `AbilityBase` → `NetworkBehaviour` (same precedent as `GravityBody`/`TurnManager`). `SuperJumpSkill`'s own
  `OnDestroy()` fixed to `override`+`base.OnDestroy()` (was silently shadowing `NetworkBehaviour`'s own cleanup
  — found while doing this conversion, not obvious otherwise).
- Pistol/Shotgun/Rpg/HandGrenade: each `Fire()` now does `if (IsSpawned) FireServerRpc(...) else SpawnAndInit(...)`
  — offline path untouched, online path executes the actual `Instantiate`+configure+`Init` on the server via
  `[ServerRpc]`, then `NetworkObject.Spawn()`. Verified offline: all 4 still spawn correctly (Shotgun 5 pellets,
  RPG 1, HandGrenade 1, no exceptions).
- `Player.prefab`: added `NetworkTransform` (**Owner Authoritative**) + `NetworkRigidbody2D`. `Projectile.prefab`
  (shared base all 3 real projectile variants nest from): added `NetworkObject` + `NetworkTransform` (**Server
  Authoritative**, default) + `NetworkRigidbody2D`. `GravityBody.FixedUpdate()` gained `if (IsSpawned &&
  !IsOwner) return;` at the top (kinematic non-owner rigidbodies still honor direct `linearVelocity` writes,
  which would otherwise still fight the replicated position). Verified offline only (screenshot, character
  renders/stands normally) — **cross-machine jitter/position-sync has not been visually confirmed yet**.
- Host/Join UI polish: `NetworkBootstrap` now retains the `ISession` and exposes `LeaveSessionAsync()`.
  `OnlineLobbyPanelUI` gained a working "İPTAL ET" (Cancel) button on the Host card (visible while waiting for
  an opponent) and a disconnect-message overlay. **Verified live, fully working**: clicked Cancel while hosting
  → `NetworkManager.IsListening` confirmed `False` → hosted again immediately after → succeeded cleanly with a
  fresh join code. This piece is done, not just implemented.

**RESUMED 2026-07-05 — project moved to `C:\Projects\CosmicRumble` (Hub still pointed at the old OneDrive
path; had to Remove + re-add from disk at the new path before MCP could attach). Fixed one new build-only
compile error found in this pass: `AutoJoinFromCmdLine.cs:50` had `Object.FindObjectsByType<Pistol>(...)` —
ambiguous between `UnityEngine.Object`/`System.Object`, same class of bug as the two pre-existing ones noted
below (only surfaces in an actual Standalone Player build, invisible in Editor since the whole method is
`#if !UNITY_EDITOR`-gated). Fixed by fully qualifying `UnityEngine.Object.FindObjectsByType`.**

- **Cross-process ability firing — VERIFIED.** Rebuilt `Builds/DevClient/CosmicRumble.exe`, hosted in-Editor
  (join code, e.g. `MMF9JC`), launched the standalone build with `-joinCode`. Client log confirms the full
  sequence: `[NET] Joined session ... IsClient=True` → turn alternation (`[TURN] Player(Clone) isActive
  False->True IsOwner=True`) → `[NET] AutoFireWhenMyTurn: firing from Player(Clone)`. Host log confirms the
  RPC actually arrived and executed server-side: `[FIRE] Player_1 spawning Pistol_Bullet_Projectile
  IsServer=True owner=1`. Non-host-client-fires-and-host-executes is proven, not assumed. Reconfirmed again in
  a second fresh host+join pass later the same day (join code `FL6FBT`) — same sequence, same result, not a
  fluke.
- **Damage/health convergence — VERIFIED (finished 2026-07-05, this session).** With a live host+client match
  connected, called `TakeDamage(15)` directly on the server-side `Player_1` `CharacterHealth` via a throwaway
  Editor script. Host log showed `[DMG] Player_1 took 15 newHealth=85` exactly once (not twice — `IsSpawned &&
  !IsServer` early-return in `TakeDamage` does its job), confirming single-application server authority.
- **Found and fixed along the way: online-spawned players (`NetworkPlayerSpawner`) never got a `HealthBarUI`
  or `CharacterNameTag` — only the offline hotseat path (`GameInitializer.AddHealthBar`/`AddNameTag`) added
  them, via `go.AddComponent<...>()` called *after* `Instantiate`.** That pattern doesn't work for NGO-spawned
  objects: every peer instantiates its own local copy of the referenced prefab *asset* when `NetworkObject.Spawn()`
  replicates, so a component added at runtime only on the server's instance never appears on any client's
  replica — it has to be baked onto the prefab itself. Fixed by adding `HealthBarUI` directly to
  `Player.prefab` (via Coplay's `add_component`, not a code change) so it's present on every replica for both
  offline and online spawns alike; `GameInitializer`'s existing `if (existing == null)` guard makes its own
  `AddComponent` call a harmless no-op now. `CharacterNameTag` was deliberately *not* given the same fix —
  unlike health (already synced via `CharacterHealth`'s `NetworkVariable<float>`), the display name has no
  sync mechanism at all yet (`NetworkPlayerSpawner` never sets a name), so baking the component alone would
  just show a blank/default tag on remote peers instead of the real username — a genuinely separate,
  not-yet-scoped feature, not a one-line fix like the health bar was.
- Re-verified end-to-end with the health bar fix in place, in a third fresh host+join pass (join code
  `MLDQNG`, rebuilt client exe first so it picked up the new `Player.prefab`): screenshot of `Player_1`
  right after spawn shows a green `100` health bar; called `TakeDamage(35)` again, screenshot immediately
  after shows the bar reading `65`, matching the host log (`[DMG] Player_1 took 35 newHealth=65`) exactly.
  Visual damage feedback for online play is confirmed working, not just the underlying number.
- **Visual position sync (`NetworkTransform`) — partially confirmed, one honest gap remains.** Both
  `Player_0`/`Player_1` render at the correct calculated spawn position on their respective planets in every
  screenshot taken across all three host+join passes this session (correct orientation, no missing-renderer
  errors), and the `NetworkTransform`/`NetworkRigidbody2D` components are present and configured as designed.
  What's **not** independently confirmed: literally watching both the host's and the standalone client's
  windows *simultaneously* over several seconds of live movement to rule out rubber-banding/jitter for the
  non-active character — MCP tooling can screenshot the host's Scene/Game view on demand but has no way to
  capture or watch the standalone client's own window, and no tool here can diff two live views side-by-side
  over time. Everything inspectable (spawn correctness, component config, health sync working end-to-end
  through the same NGO replication path) is consistent with position sync also working correctly, but this
  specific claim is inference from code + partial evidence, not a direct observation — flag this if
  jitter/rubber-banding is ever reported by an actual two-person playtest.

- **Tooling gotcha, still applies going forward:** creating or editing ANY `.cs` file under `Assets/` while
  the Unity Editor is in Play Mode triggers a script recompile + domain reload, resetting every runtime
  singleton `Instance` and silently dropping any live NGO connection. Always write/edit every `.cs` file
  needed for a test pass *before* entering Play Mode; re-running an already-compiled, unchanged script via
  `execute_script` is safe.
- **New gotcha found this session:** `BuildPipeline.BuildPlayer` cannot be called directly from an
  `execute_script` invocation — it fails immediately with `"A player build cannot be executed while inside
  the player loop"` (the MCP bridge's own call path counts as "inside the player loop" from Unity's
  perspective, same restriction that normally stops you building from inside `OnGUI`/`Update`). A single
  `EditorApplication.delayCall` was not enough to escape it either (silently never fired, no error, no build
  — the callback needs the editor to be *and stay* idle across multiple ticks, not just running one operation
  later). What worked: subscribe to `EditorApplication.update`, skip while `isCompiling`/`isUpdating`, wait a
  handful of ticks, then unsubscribe and call `BuildPipeline.BuildPlayer` from inside that later tick. The
  triggering `execute_script` call itself then blocks (times out client-side after 60s, harmlessly — the
  build keeps running server-side) since the main thread is genuinely busy building; poll the output exe's
  mtime or `get_unity_logs` for `[BUILD] result=...` instead of trusting the RPC's own return.
- Also learned: `capture_ui_canvas` with no `canvasPath` arg only captures the *first* canvas in the scene
  (`MenuCanvas`), not whatever is topmost/active — pass the specific `canvasPath` (e.g. `OnlineLobbyCanvas`)
  explicitly to see an overlay panel that lives on its own Canvas.

**Cleanup done (2026-07-05) — all temporary verification-only code removed, Milestone 2 is now actually
closed:** `Assets/Scripts/Networking/AutoJoinFromCmdLine.cs` (+ its component on `NetworkBootstrap` in
`MenuScene`) deleted; the `[FIRE]`/`[DMG]` unconditional `Debug.Log` lines removed from `Pistol.cs`/
`CharacterHealth.cs`; `Assets/Editor/Temp_ClickButton.cs`, `Temp_CheckState.cs`, `Temp_TestDamage.cs`,
`Temp_BuildClient.cs`, `Temp_SaveScene.cs` all deleted; `MenuScene.unity` re-saved in place at its correct
path. Nothing test-only remains in the tree for this milestone.

**Not started (code-wise), still later phases:** the transport recommendation below has changed now that a
mobile release sharing the same online system is a stated goal — see "Online backend" below for the full
reasoning. Ability sync for `BlackHoleSkill`/`Teleport`/`ShieldSkill`/`BatHammerSkill` (out of scope for
Milestone 2, still local-only), matchmaking pools, and Host/Join UI polish (reconnect, regional Relay
selection) remain as the next real chunks of work.

**Cross-play scope — reversed 2026-07-06, see Milestone 5 below.** This section originally said Steam would be
its own isolated pool, never matching mobile players. The user explicitly overturned that: Steam and mobile
players are now meant to match each other freely (Steam's release is uncertain anyway) — Quick Match (Milestone
5) implements **one single unified pool**, no platform split. The `crossplayGroup`/indexed-lobby-field design
described just below was never built and is now explicitly not planned unless this decision changes again.

- Netcode layer stays **Unity Netcode for GameObjects (NGO)** — this part of the earlier recommendation still
  holds regardless of transport, since `TurnManager`'s single-actor-per-turn model maps cleanly onto a
  host-authoritative NGO session and Photon Fusion's real-time rollback/prediction is unneeded overhead for a
  turn-based, max-8-player game.
- **Transport is still Unity Relay + Lobby (UGS) for both pools, not Steam P2P relay** — even though Steam and
  mobile no longer need to match against each other, using one transport for both keeps the netcode layer
  identical across builds (no `#if` branching between a Steam-relay code path and a mobile-relay code path,
  one thing to test and maintain instead of two). Verified: Steam does not require Steamworks Networking/SDR
  for multiplayer — third-party transports are explicitly allowed, and running Facepunch.Steamworks (for
  achievements/overlay/rich-presence) alongside Relay+UTP (for netcode) in the same build has no documented
  conflict.
  - The pool split is handled at the *matchmaking* layer, not the transport layer: tag each Lobby with a
    `crossplayGroup` data field (`"steam"` or `"mobile"`) and filter lobby queries on it. **Verified caveat:**
    the field must be set as an **indexed, Public** data field (string index slots are `S1`–`S5`, only 5 per
    lobby) to be queryable via `QueryFilter` — e.g. `crossplayGroup` on `S1`. Budget the other 4 string slots
    for region/mode/etc. since that cap is hard.
  - Steamworks (once added for achievements, see above) stays purely for Steam-specific extras — overlay, rich
    presence, invites — not for core networking.
- `TurnManager`'s client/server refactor for turn sync itself is now done (Milestone 1, above). Host/Join UI
  polish (cancel/leave-session handling, reconnect) is done — see Milestone 4 below; regional Relay selection
  was considered and deliberately not built, it's already automatic (see Milestone 4's reasoning). Matchmaking
  itself (Quick Match, single unified pool, no Steam/mobile split) is done — see Milestone 5 below.

### Done (2026-07-05) — Milestone 3: BlackHoleSkill/Teleport/ShieldSkill/BatHammerSkill networking
Scope: the four abilities explicitly deferred out of Milestone 2 as "each a genuinely different sync problem."
Each needed its own approach since none of them is a simple flying projectile with a damage-on-hit event.

- **`GravityBody` gained two general-purpose cross-machine effect helpers** (`ApplyForce(Vector2, ForceMode2D)`
  and `Teleport(Vector2 position, Vector2 up)`), used by all three of BlackHoleZone/BatHammerSkill/
  TeleportOrbProjectile below. Both follow the same rule: if offline or already the owner, apply directly
  (zero overhead); if server and not owner, send a **targeted `[ClientRpc]`** to `OwnerClientId` so the actual
  owning machine performs the write. This is necessary because `Player.prefab`'s `NetworkTransform` is Owner
  Authoritative — a server-side (or any non-owner) direct write to a remote player's `Rigidbody2D` either
  no-ops (`AddForce` on a body `NetworkRigidbody2D` has auto-kinematic'd for non-owners) or gets silently
  overwritten by the real owner's next authoritative update (`.position` writes). Both BatHammer's knockback
  and BlackHole's pull needed the force fix; Teleport needed the position fix for the exact same underlying
  reason.
- **Found and fixed a severe, unrelated pre-existing regression while testing this:** every character's
  `Rigidbody2D` is permanently stuck **Kinematic in offline hotseat mode** — `NetworkRigidbody2D.Awake()`
  (`AutoUpdateKinematicState=true`, added to `Player.prefab` in Milestone 2) unconditionally forces Kinematic
  at startup and only corrects it in `OnNetworkSpawn()`, which never fires offline (`NetworkObject` is never
  spawned in hotseat mode). This silently no-ops **every `Rigidbody2D.AddForce()` call in the entire
  codebase when playing offline** — jump impulses (`GravityBody.PerformJump`), the Zone-3 downhill slide
  force, RPG/HandGrenade explosion knockback, and now this session's own BatHammer/BlackHole work. Confirmed
  directly: `rb.AddForce(Vector2.up * 1000f, ...)` on the offline human player produced zero velocity change
  before the fix, and (accidentally, hilariously) launched the character to their death after the fix worked
  (extreme test force + genuinely-Dynamic body = real physics). Fixed with one line in `GravityBody.Start()`
  (runs after all `Awake()`s regardless of component order, unlike fixing it in `Awake()` which would have
  been undone by `NetworkRigidbody2D`'s own later `Awake()`): `if (!IsSpawned) rb.bodyType =
  RigidbodyType2D.Dynamic;`. Re-verified after the fix: offline jump-equivalent `AddForce` test and BatHammer
  knockback both produced real, correct velocity changes.
- **BlackHoleSkill**: same `FireServerRpc`/`SpawnAndInit` pattern as Pistol/HandGrenade — `Fire()` routes
  through the server when spawned, `SpawnAndInit()` calls `NetworkObject.Spawn()` on the projectile. Added
  `NetworkObject` + `NetworkTransform` (Server Authoritative) + `NetworkRigidbody2D` to
  `PF_BlackHoleProjectile.prefab` (auto-registered into `DefaultNetworkPrefabs.asset` by NGO, same as
  Milestone 2's projectiles). `BlackHoleZone`'s pull force (`BlackHoleZone.cs`) now resolves the hit's
  `GravityBody` and calls `ApplyForce()` instead of touching `rb.AddForce` directly — its pre-existing
  `bodyType != Dynamic → skip` filter is kept as a fallback for non-`GravityBody` dynamic props, but no longer
  the only path.
  - **Found and fixed while wiring this up: `BlackHoleSkill` was never actually attached to `Player.prefab`
    at all** — the script existed and (per this session) is now fully networked, but no character in any
    scene ever had the component, so the ability was completely dead code in real gameplay, independent of
    networking. Asked the user whether to attach it now or leave it prepared-but-dormant; no response within
    the wait window, proceeded with attaching it (the more useful default — code that's networked but still
    unreachable in-game serves nobody). Added to `Player.prefab` with `firePoint`/`projectilePrefab` wired to
    match the other abilities' pattern.
  - **Also found and fixed in the same step:** `BlackHoleSkill`'s own default `activationKey` was
    `KeyCode.Alpha8` — colliding with `BatHammerSkill`'s `Alpha8`, and contradicting `BlackHoleSkill`'s own
    code comment (`// Slot 8 corresponds to keyboard '9'`). Changed the default to `KeyCode.Alpha9` in both
    the script and the already-serialized `Player.prefab` override (adding the component bakes in whatever
    the field default was at that moment, so both needed the fix).
- **Teleport**: same `FireServerRpc`/`SpawnAndInit` pattern; added `NetworkObject` + `NetworkTransform`
  (Server Authoritative) + `NetworkRigidbody2D` to `TeleportOrbProjectile.prefab`. `TryTeleportOwner()` (only
  ever runs server-side, since `Init()` is only ever called from `SpawnAndInit()`) now calls
  `ownerGravityBody.Teleport(target, up)` instead of writing `ownerRb.position`/`transform.up` directly, so a
  client-owned character's teleport actually reaches and sticks on the owner's machine instead of being
  silently overwritten by their own next `NetworkTransform` update.
- **ShieldSkill**: `CharacterHealth.isShielded` converted from a plain `bool` to a server-authoritative
  `NetworkVariable<bool>` (same exact pattern as `currentHealth`), exposed as a read-only `isShielded`
  property plus a `SetShielded(bool)` writer (offline: direct; online: only the server actually writes).
  **This was a real bug, not just missing infrastructure**: `ShieldSkill.OnFireUpdate()` only ever runs on the
  ability owner's own machine (`AbilityBase` gates all input on `IsOwner`), so a remote client activating
  Shield was mutating only *their own local copy* of a plain bool — the server's copy (the only one
  `CharacterHealth.TakeDamage()` — itself server-only — ever reads) never found out, so the damage reduction
  silently never applied for anyone except the host. Fixed via `ActivateShieldServerRpc()`. The visual
  (sprite color change) is now driven by a new `CharacterHealth.OnShieldedChanged` event tied to the
  `NetworkVariable`'s `OnValueChanged`, replacing the old owner-only `Update()` polling — this also fixes a
  second, related bug where other peers could never see a remote player's shield visual at all.
  - **Verified the exact bug-then-fix, offline, atomically** (single script call, no turn-cycle race): before
    activation `isShielded=False`; immediately after, `isShielded=True`; a 20-damage hit reduces to a 10-point
    loss (`shieldDamageReduction=0.5`), not 20 — matching the design exactly.
- **BatHammerSkill**: had no server-side path at all before this (`OnFireUpdate()`'s entire cone
  detection+knockback ran only on the swinging player's own machine). Split the old `PerformKnockback` into
  `DetectTargets(aimDir)` (pure query, no side effects) and `ApplyKnockback(targets, power01)` (the actual
  force application, now via `GravityBody.ApplyForce`). `OnFireUpdate()` still runs `DetectTargets` locally
  first to decide the existing "only consume ability/cooldown if something was actually in the cone" behavior
  unchanged (safe since the turn-based model means only the active/swinging character moves — targets are
  stationary during your own turn, so client-local detection and the server's later re-detection agree in
  practice); the actual force application routes through a new `[ServerRpc] SwingServerRpc(aimDir, power01)`
  when networked, which re-runs `DetectTargets`+`ApplyKnockback` server-side for authoritative delivery to
  whichever peer truly owns each hit character.

**Verified — fresh host+join session, standalone client build vs. Editor host, same MCP-driven workflow as
Milestone 1/2:**
- **BlackHoleSkill and Teleport: fully confirmed end-to-end.** The standalone (non-host) client fired both
  abilities on its own turn via a temporary test harness (`AutoJoinFromCmdLine.cs`, same role as Milestone 2's
  — reads `-joinCode`, then invokes a sequence of skills via reflection on each of the client's own turns).
  Host-side log confirms both `FireServerRpc → SpawnAndInit → Init()` chains executed server-side with the
  correct stack trace, for a request that originated from the non-host client. **Teleport additionally
  self-confirmed via a real position change**: `Player_1` (client-owned) spawned at `(0.00, -16.88)`;
  after its own `Teleport.Fire()` call (RPC → `TryTeleportOwner` → `GravityBody.Teleport` → targeted
  `ClientRpc` back to the real owner), its host-replicated position had moved to `(-0.22, -3.33)` — a large,
  deliberate jump consistent with a successful teleport, not gradual walking. This is a genuine, organic proof
  of the owner-forwarding fix working across two real processes.
- **ShieldSkill: RPC path confirmed reached and executed** (`ShieldSkill.OnFireUpdate` invoked from the
  client's own turn with no exception) — the damage-reduction *correctness* itself was verified separately
  and atomically offline (above), not re-derived live over the network in this pass (would need a
  `TakeDamage` call timed exactly during the client's shielded window, not attempted this session).
- **BatHammerSkill: now fully confirmed live, cross-machine, with the most direct evidence possible.** Redone
  in a clean pass (all test scripts written *before* entering Play Mode this time, avoiding the previous
  session's domain-reload mistake): host-side script repositioned the client-owned `Player_1` next to the
  host's `Player_0` via `GravityBody.Teleport` (itself re-confirmed working — `Player_1` moved from
  `(0.00, -16.92)` to exactly the requested `(1.15, 16.29)`), then swung `Player_0`'s `BatHammerSkill`
  (`DetectTargets` found `Player_1`, `ApplyKnockback` invoked). A temporary `Debug.Log` added to
  `GravityBody.ApplyForceClientRpc` (removed after) proved the point beyond any inference: **the standalone
  client's own log file** (not the host's) reads `[APPLYFORCE] Player(Clone) received ClientRpc, applying
  force=(9.32, -3.64) velocityBefore=(0.00, 0.00)` → `velocityAfter=(9.32, -3.64)` — the actual remote OS
  process received the targeted `ClientRpc` and applied real force to its own authoritative rigidbody. This
  closes the one open item from the previous pass; all four Milestone 3 abilities are now verified end-to-end
  across two real processes, not just reasoned about.
- **Tooling gotcha reconfirmed this session, cost a retest:** creating a **new** `.cs` file while Play Mode is
  active (not just editing an existing one) triggers the same domain-reload-mid-session problem as
  Milestone 2 documented, and this time it appears to have also disrupted live NGO RPC dispatch for
  already-spawned `NetworkObject`s (not just resetting `Instance` singletons as previously seen) — avoid
  writing *any* new Editor test script once a host+join session is already live; write every script needed
  for a pass *before* entering Play Mode, same rule as before but now confirmed to also apply to RPC
  reliability, not just singleton state.
- Session also hit one Relay/Lobby session expiring between hosting and actually launching the client (a
  `SessionNotFound` join failure) after a ~6-minute gap while the standalone client exe was rebuilding —
  cancelled and re-hosted for a fresh join code immediately before launching the client; not a code bug, just
  a reminder that a hosted session has a real, fairly short TTL if nothing joins it promptly.

**Cleanup done (2026-07-05, twice — once per pass):** `AutoJoinFromCmdLine.cs` (+ its `NetworkBootstrap`
component) deleted after each pass; all `Assets/Editor/Temp_*.cs` helper scripts (`Temp_ClickButton`,
`Temp_TestSkills`, `Temp_TestNetSkills`, `Temp_BuildClient`, `Temp_SaveScene`) deleted; the temporary
`Debug.Log` added to `GravityBody.ApplyForceClientRpc` for the BatHammer proof removed; `MenuScene.unity`
re-saved in place. Nothing test-only remains in the tree.

**Done (2026-07-06) — ShieldSkill's last open item closed.** Fresh host+join pass, client activated Shield on
its own turn; host polled `Player_1.CharacterHealth.isShielded` (the `NetworkVariable`, readable by everyone)
until it flipped `true`, then called `TakeDamage(20)` immediately (had to react fast — turns cycle roughly
every ~20s here, and the shield resets on the shielded character's own *next* turn start, so the window isn't
huge; a first attempt reacted too slowly after the client's own script fired and the shield had already reset
by the time it checked, redone cleanly). Result: `Player_1 isShielded=True, damage test before=100 after=90
delta=10` — exactly the 50% `shieldDamageReduction`, live, genuinely networked. All four Milestone 3 abilities
are now fully verified end-to-end, nothing left open from that milestone.

### Done (2026-07-06) — Milestone 4: mid-match reconnect support
Requested explicitly (mobile ships first, Steam may never happen, so multiplayer robustness matters more than
Steam-specific polish right now). Scope: a player whose connection drops mid-match can relaunch and rejoin with
the same code, reclaiming their exact character — not spawning a duplicate, not losing the match immediately.
Host migration (the *host* disconnecting) stays explicitly out of scope, same as always — no reconnect target
exists for that case, the whole session ends.

- **Root prerequisite fix: `Player.prefab`'s `NetworkObject.DontDestroyWithOwner` was `false`** (the default) —
  meaning NGO destroyed a player's character the instant their owning client disconnected, before any reconnect
  logic could ever run. Changed to `true`; the character now survives disconnect (frozen, uncontrolled) so it
  can actually be reclaimed later.
- **`NetworkPlayerSpawner`** now tracks `clientId -> NetworkObject` for the two initial spawns, and subscribes
  to `OnClientConnectedCallback`/`OnClientDisconnectCallback` *after* the initial two-player spawn (so those two
  callbacks don't interfere with `SpawnAllConnectedClients`'s one-time setup). On disconnect: the character is
  marked "orphaned" (not destroyed, per the fix above) and a `reconnectTimeout` countdown starts (**90s**
  default). On a *later* connection while the match is already running: if there's exactly one orphaned slot,
  `NetworkObject.ChangeOwnership(newClientId)` hands the existing character back — no new spawn. If nobody
  reclaims it within the timeout, the character is despawned, and `TurnManager`'s existing `characters.Count<2`
  check ends the match naturally (no special-casing needed there — it already declares a winner correctly).
- **`NetworkBootstrap`** gained a persistent (`DontDestroyOnLoad`) status banner (`ShowStatus`/`HideStatus`,
  built once in `Awake()`, sorting order 100 so it's always on top) and a client-side auto-reconnect loop
  (`OnUnexpectedDisconnect`): on an unexpected disconnect (not a self-initiated `LeaveSessionAsync`, tracked via
  an `_intentionalLeave` flag) while we were the client (not host — host losing connection ends the whole
  session, no retry target), it retries `JoinSessionAsync(LastJoinCode)` a few times (**6 attempts, 5s apart**
  by default) before giving up and returning to the menu.
  - **Also removed `OnlineLobbyPanelUI`'s old disconnect-handling entirely** (`OnClientDisconnected`,
    `_disconnectedRoot`/`BuildDisconnectedOverlay`, `_matchStarted`). Audited why it existed and concluded it
    was **already dead code, not just superseded**: that panel's `GameObject` is MenuScene-local (no
    `DontDestroyOnLoad`), and `NetworkManager.SceneManager.LoadScene(Game, Single)` unloads MenuScene the moment
    the match starts — so its `OnClientDisconnected` handler could only ever fire in the split-second window
    before that scene swap finished, never for a genuine mid-match disconnect. It was also structurally
    one-sided: `_matchStarted` was only ever set `true` on the *host's* instance (inside `OnClientConnected`),
    never on the joining client's own instance, so the client-side copy of the same handler always no-op'd via
    an early return regardless of timing. Leaving both old and new systems subscribed to the same
    `OnClientDisconnectCallback` risked a race where the old handler's unconditional `LeaveSessionAsync()` call
    could end the session before the new orphan-tracking logic got a chance to run.
- **Major mid-session discovery that explains almost all the time this took: NGO's disconnect callback only
  tears down the Netcode *transport* connection — it does nothing to the underlying UGS Session/Lobby
  membership, which is a completely separate service-side record.** Live-tested extensively: killed the
  standalone client process (simulating a crash/force-quit — no graceful `LeaveAsync`), then tried rejoining
  with the same code. Every attempt failed with `SessionException: [SessionConflict] player is already a member
  of the lobby` — including after waiting **over 250 seconds**, which conclusively ruled out "just needs to
  time out on its own." Root cause found by reading the `com.unity.services.multiplayer` package source
  directly (`SessionHandler.cs`): `IHostSession` exposes `RemovePlayerAsync(string playerId)` and a `Players`
  list — nothing evicts a vanished player automatically, the **host** has to explicitly remove them. Added
  `NetworkBootstrap.RemoveDisconnectedPeerAsync()` (finds the session player whose `Id` isn't
  `AuthenticationService.Instance.PlayerId`, i.e. "not me" — valid for this 2-player game without needing a
  clientId-to-UGS-playerId mapping — and calls `IHostSession.RemovePlayerAsync` on them), called from
  `NetworkPlayerSpawner.OnClientDisconnectedMidMatch` the moment a disconnect is detected. **Confirmed this was
  the actual fix, not a coincidence:** immediately after adding it, a kill-and-rejoin succeeded on the very
  first attempt, within about 30 seconds — a night-and-day difference from the 250+ second failures just
  before. `reconnectTimeout`/`reconnectAttempts` were only ever inflated (up to 300s at one point) to work
  around this symptom; dialed back down to sane production defaults (90s / 6 attempts × 5s) once the root cause
  was actually fixed.
- **Verified live, end-to-end, genuinely two separate processes** (same MCP-driven host+join workflow as every
  prior milestone): host+join → kill the client process outright → host log confirms `clientId=1 koptu — Player_1
  sahipsiz bırakıldı` (character survives, not destroyed) → relaunched the standalone client with the *same*
  join code → **`[NET] Reconnect: clientId=2 Player_1 karakterini geri kazandı (eski clientId=1)`** — a brand
  new NGO connection id (2, as expected — NGO doesn't reuse the old one) correctly reclaimed the *exact same*
  `Player_1` `GameObject` via `ChangeOwnership`, not a duplicate spawn, with both `Player_0` and `Player_1` still
  present in the hierarchy afterward. Also independently confirmed the timeout path works correctly on its own
  merits (from before the `RemovePlayerAsync` fix, still valid): letting the window expire fires `[NET]
  Reconnect window expired ... despawning` at exactly the configured duration and `TurnManager` correctly
  declares the remaining player the winner via its existing, unmodified game-over logic.
- **Not implemented, explicitly out of scope:** host disconnecting/migrating (no reconnect target, whole
  session ends — unchanged from every prior milestone's stated scope), and a manual regional Relay selection
  **UI** (the third item from the original "Host/Join UI polish" backlog entry) — deliberately not built, no UI
  complexity/player confusion added for little real benefit in a turn-based (not latency-sensitive) game.
  - **Clarified 2026-07-06, re-checked against the SDK source directly (not just inferred):** this does NOT mean
    "no region selection happens." `NetworkBootstrap.HostSessionAsync` calls `.WithRelayNetwork()` with no
    `region` argument — per `SessionOptionsExtensions.WithRelayNetwork`'s own doc comment in
    `com.unity.services.multiplayer`: *"the region is optional; the default behavior is to perform quality of
    service (QoS) measurements and pick the lowest latency region."* So automatic QoS-based region selection is
    already active today, for free, with zero extra code — what's absent is only a manual override UI letting a
    player force a specific region, which is the part judged not worth building.
- **Cleanup done:** `AutoJoinFromCmdLine.cs` (+ its `NetworkBootstrap` component) deleted; `Temp_ClickButton.cs`,
  `Temp_BuildClient.cs`, `Temp_SaveScene.cs` deleted; `MenuScene.unity` re-saved in place.

### Done (2026-07-06) — Milestone 5: Quick Match (automatic matchmaking, no code needed)
Requested explicitly ("quick match zaten kesinlikle olmalı oyunun ana olayı zaten bu" — quick match is the
core of the game and had to exist). Superseded the originally-planned "matchmaking pools (Steam/mobile split)"
backlog item — investigating it surfaced that the game had **no matchmaking at all** yet (only manual
host/join-by-code), so "split into pools" wasn't actually buildable before the pool-less version existed.
**Also, the user explicitly reversed the earlier stated pool-separation policy**: Steam and mobile players are
now allowed to match with each other (Steam's own status is uncertain anyway) — so this is deliberately **one
single unified matchmaking pool**, not split by platform. If platform separation is ever wanted later, it's a
`FilterOption` added to `QuickJoinOptions.Filters` plus a `crossplayGroup` session property — not built, since
it isn't wanted right now.

- **`NetworkBootstrap.QuickMatchAsync()`** — one call to the SDK's own built-in
  `MultiplayerService.Instance.MatchmakeSessionAsync(QuickJoinOptions, SessionOptions)`. No custom lobby-browsing
  code needed: `QuickJoinOptions.CreateSession = true` means it searches the public pool for a waiting session
  and joins it if one exists, or — if none does — creates its own new public session and becomes host,
  entirely within the one SDK call. `NetworkBootstrap.IsHostAfterQuickMatch` (just checks
  `NetworkManager.Singleton.IsHost` after the call resolves) tells the caller which of the two happened.
- **`HostSessionAsync()`** (the existing manual friend-code flow) now sets `IsPrivate = true` on its
  `SessionOptions` — this is what keeps a "host a private game for a specific friend" session out of the public
  Quick Match pool; without it, a stranger's Quick Match could stumble into and join a session someone meant to
  share only via a private code.
- **`OnlineLobbyPanelUI`** gained a new, prominently-placed `QuickMatchCard` (top-center, above the existing
  Host/Join cards which shrank and moved down to become the secondary "or invite a friend by code" section,
  matching how the user described Quick Match as the primary/main flow). `OnQuickMatchClicked()` calls
  `QuickMatchAsync()`; if the result is "we became host" (no one was waiting), it reuses the exact same
  `_waitingForOpponent`/`OnClientConnected`/cancel plumbing the Host flow already had — no duplicated logic, this
  is genuinely the same wait-for-second-player state regardless of how the session was created. If the result is
  "we joined someone else's session" (the common case once any player pool exists), the host's own
  `OnClientConnected` already triggers the scene load, exactly like the existing Join-by-code flow.
- **Verified live, end-to-end, genuinely two separate processes, no code ever typed anywhere:** clicked "OYNA"
  in the Editor with no one else in the pool — host log confirmed `[NET] QuickMatch succeeded, becameHost=True,
  code=BDCL8B` (created its own public session, waiting). Launched the standalone client with a temporary
  `AutoQuickMatchFromCmdLine.cs` test harness (calls `QuickMatchAsync()` on startup, no join code passed in at
  all) — its log confirmed `[NET] QuickMatch succeeded, becameHost=False, code=BDCL8B`, i.e. it found and joined
  the *exact same* session purely through the public pool, with the two processes never having exchanged a code.
  Match then proceeded normally: `[NET] Spawned player for clientId=0`/`clientId=1` both fired, confirmed via
  `Player_0`/`Player_1` both present in the running scene. This is the actual bar for "quick match works" — not
  just that the API call succeeds, but that two independent, code-less processes actually found each other.
- **Cleanup done:** `AutoQuickMatchFromCmdLine.cs` (+ its `NetworkBootstrap` component) deleted; `Temp_ClickButton.cs`,
  `Temp_BuildClient.cs`, `Temp_SaveScene.cs` deleted; `MenuScene.unity` re-saved in place.

## Test-only local bots
Restored 2026-07-03 — a previous commit (`0f3316f`, 2026-07-02) deliberately removed this exact system;
brought back at the user's request specifically for local testing convenience, not as a shipped feature.

- `LobbyData.BotCount` (int, default `0`), `BotSpawner.cs` (recreated, slimmed to just spawn — the
  surface/position math it used to own now lives in `Utilities/SpawnPositioning.cs` and is shared with
  `GameInitializer`/`SpawnDebugger`, so it wasn't duplicated back in), `GameInitializer` spawns
  `1 + BotCount` characters and registers all of them with `TurnManager`, `LobbyPanelUI` has the
  Bot Count `[-]`/`[+]` selector back (capped at 3, matches the old cap).
- **Deliberate difference from the old (removed) version:** bots are NOT inert dummies this time — the old
  code disabled `PlayerController2D` on spawned bots (`ctrl.enabled = false`). This version leaves every
  component enabled, so a bot is mechanically identical to the human player. Since `AbilityBase`/movement
  already gate all input on `GravityBody.isActive` (only the character whose turn it is responds to
  anything), this makes bots fully hot-seat-controllable by the same local tester once `TurnManager` makes
  them active — verified in Play mode with `BotCount=2`: `Bot_1`/`Bot_2` spawned correctly, both had
  `PlayerController2D.enabled=true` and all 8 abilities enabled, and `GravityBody.isActive` correctly
  flipped to the active turn's character. No AI logic exists or is planned here; this is purely "let one
  tester drive both sides of a match locally."
  - **Bug caught in review before commit, fixed:** the old code also tagged spawned bots `"Bot"` instead of
    leaving the prefab's own `"Player"` tag. Grepped the whole project — the only place that tag is read is
    `BatHammerSkill.cs:121` (`if (onlyAffectTaggedPlayers && !hit.CompareTag("Player")) continue;`), which
    would have made bots silently immune to the bat/hammer melee weapon while still being fully hittable by
    every projectile weapon (those filter by `attachedRigidbody` presence, not tag) — an inconsistency that
    directly undermines "bots are equivalent test opponents." `BotSpawner.SpawnBots()` no longer overwrites
    the tag, so bots keep `"Player"` inherited from the prefab.

## Controls
Done — Move Left / Move Right / Jump plus the 9 ability hotkeys are considered sufficient as-is. No further
rebinding work planned.

## Save / sync — cross-platform online backend
**Done and live.** Unity Cloud Project is linked (org `eren-zcan`, project `CosmicRumble`, project ID
`3165363e-befa-4137-8a10-ea7978e902d9`), Authentication + Cloud Save enabled, and a real push/pull round-trip
against the live UGS backend has been verified (see below) — not just local-only fallback.

- Installed `com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.cloudsave`.
- Added `Assets/Scripts/Cloud/CloudSaveManager.cs` (namespace `CosmicRumble.Cloud`): initializes UGS, signs in
  anonymously, and syncs `currency.json`, `progress.json`, `unlocks.json`, `quests.json`, `chests.json`,
  `streak.json`, `costumes.json` to Cloud Save under matching keys (`currency`, `progress`, etc.).
  - **`achievements_<username>.json` and `users.json`/`profiles/` are deliberately NOT synced.** The local
    username system (`AuthManager`) is separate from UGS Authentication's player identity, and syncing a
    per-username-named file to a per-UGS-identity cloud slot isn't safe until that relationship is decided
    (does UGS Auth replace local guest accounts entirely, or link to them? — a bigger question than "add cloud
    save", left for a dedicated pass).
  - `MainMenuUI.Awake()` was changed from a synchronous `EnsureSingletons()` call to a coroutine
    (`BootstrapSequence`): core singletons (`GameConfig`, `AuthManager`, `AudioManager`, `CloudSaveManager`)
    first, then `CloudSaveManager.InitializeAndPull()` (pulls all 7 keys from the cloud and overwrites the
    matching local files, so the *other* progress managers' own `Awake()`-time `Load()` reads already-synced
    data — ordering matters here), capped at a 6s timeout so a slow/unreachable network can never hang the
    menu, then the 7 progress managers + achievements are created as before.
  - Each of the 7 managers' `Save()` now also calls `CloudSaveManager.Instance?.QueuePush("<key>", SavePath)`
    (fire-and-forget) right after writing the local file, mirroring the `AchievementEvents`/`AudioManager`
    wiring pattern already used elsewhere in this codebase.
  - `InitializeAndPullAsync()` retries once (1s delay) before giving up — the very first Play-mode entry right
    after linking the Cloud Project hit a transient `UnityProjectNotLinkedException` even with a genuinely
    correct link (UGS's internal service registry warming up), which without a retry would've silently
    disabled cloud sync for that entire session. 5 subsequent Play-mode entries all succeeded cleanly without
    needing the retry — the registry stays warm once the project's been linked and used a few times, so this
    mainly protects the first session after (re-)linking, not everyday play.
  - **Verified against the real backend, not just local-only fallback:** pushed `currency.json` via
    `CurrencyManager.Save()`, independently confirmed the identical JSON landed in the live Cloud Save
    backend via `CloudSaveService.Instance.Data.Player.LoadAsync`; then deleted the local file entirely,
    re-entered Play mode, and confirmed it was recreated from the cloud with matching content — full
    push-then-restore cycle proven, not assumed.
  - Also verified the pre-linking fallback path still holds: with no Cloud Project linked,
    `UnityServices.InitializeAsync()` fails fast (caught internally), `IsReady` correctly reports `false`,
    every push/pull call becomes a safe no-op, and the game runs exactly as before — additive, not breaking,
    whether or not cloud is configured.

**Login/Register screen (`LoginPanelUI.cs`/`AuthManager.cs`) now uses real UGS accounts, not the old fully
local system — this is what makes progress actually portable across devices/reinstalls, not just backed up.**

- The old system stored a local username + SHA256 password hash in `users.json` with zero server component —
  it only let one device distinguish between multiple local profiles, it never enabled cross-device play.
  Replaced with Unity Gaming Services' Username & Password identity provider (enabled in the Unity Cloud
  Dashboard under Player Authentication → Identity Providers). Old local accounts do not carry over (the
  plaintext password was never stored anywhere, only an irreversible hash, so there's nothing to migrate from
  — not a concern here since none were real players).
- **Register** calls `AuthenticationService.Instance.AddUsernamePasswordAsync()` — this *adds* credentials to
  whatever session is already active (normally the anonymous session `CloudSaveManager` established
  automatically at boot), rather than creating a brand new identity from scratch. This means playing as a
  guest first and registering later keeps that guest progress — the intended mobile pattern ("play now, save
  your progress by making an account later"), not a reset to zero.
- **Login** switches to a genuinely different account (different Player ID, different cloud data), so unlike
  Register it can't just keep using the already-loaded local files — `AuthManager.ReloadSessionScene()`
  destroys the 8 `DontDestroyOnLoad` progress-manager GameObjects (Currency/PlayerLevel/Unlock/Quest/Chest/
  Streak/Achievement/AchievementTracker — the ones whose `Awake()`-time `Load()` only ever reads local files
  once) and reloads the scene, so `MainMenuUI`'s `BootstrapSequence` runs fresh: `CloudSaveManager` re-pulls
  under the new identity, then the progress managers are recreated reading the newly-synced files. Logout
  does the same (signs out, reloads — the fresh boot then re-establishes a clean anonymous session
  automatically, same as a first-ever launch). Register does NOT reload — same identity, no data changed.
- **Bug found and fixed during testing:** `Login()` signs out of the previous session *before* attempting the
  new sign-in (has to — can't hold two sessions at once). If the new sign-in then fails, that sign-out can't
  be undone (credentials already cleared), but the old code left `IsLoggedIn`/`CurrentUsername` still
  reporting the now-invalid previous account as active. Fixed: on failure, local state resets to signed-out
  if a session had been active (`ResetIfSessionLost`) — verified a failed login now correctly leaves
  `IsLoggedIn=false`, and the app can recover into a normal guest session immediately after.
- **Verified against the real backend** (test account `cosmictest02`): Register (no reload, instance IDs
  unchanged), Logout (reload, instance IDs changed), Login with correct credentials (reload, succeeds), Login
  with wrong password (fails cleanly, correct state reset), Register with a taken username (fails cleanly,
  `ENTITY_EXISTS`), Guest login/switch. Zero Editor hangs, zero unhandled exceptions across the full matrix.
- **Tooling gotcha hit twice during this work, for future reference:** testing `async`/`Task`-returning code
  via Unity Editor script execution must never synchronously block the calling thread on an incomplete Task
  (`.Result`/`.Wait()`/`.GetAwaiter().GetResult()`) — this deadlocks Unity's main thread (the awaited
  continuation needs that same thread's `SynchronizationContext` to resume) and freezes the entire Editor
  solid, requiring a manual restart. Safe pattern: fire-and-forget an `async void` wrapper that does a real
  `await`, log the outcome, and check back via the console log in a separate, later call — never poll a
  blocking call in the same script.
- **Done (2026-07-03) — `achievements_<username>.json` now syncs to Cloud Save.** `CloudSaveManager` gained a
  dedicated `"achievements"` key handled separately from the fixed-filename `SyncedFiles` dict (its local
  filename varies by username, so it can't live in that dict) — `CurrentAchievementsFileName` computes
  `achievements_<username>.json` or `achievements_guest.json` from `AuthManager.Instance` at pull time, same
  logic `AchievementManager.SavePath` already used. `AchievementManager.Save()` now also calls
  `CloudSaveManager.Instance?.QueuePush("achievements", SavePath)`, mirroring the other 7 files.
  - **Bug found and fixed while wiring this up:** `MainMenuUI.EnsureProgressSingletons()` created a fresh
    `AchievementManager` after a Login-triggered scene reload but never called `LoadForUser()` on it — its
    `Awake()` defaults `_currentUsername` to `null`, so it silently loaded `achievements_guest.json` even for
    a logged-in user (existing achievement progress just wasn't visible/tracked against the right file, not
    lost). Fixed: `EnsureProgressSingletons()` now calls `LoadForUser(...)` immediately after creating it,
    reading the real identity from `AuthManager.Instance`.
  - **Verified against the real live UGS backend:** called `AchievementManager.UpdateProgress("ROKETCI", 1)`
    in Play mode, confirmed `Save()` → `QueuePush` fired with no exceptions, then independently read the
    `"achievements"` key straight from `CloudSaveService.Instance.Data.Player.LoadAsync` — the cloud copy
    showed `ROKETCI` at `currentProgress: 1`, matching exactly, full round-trip proven not assumed.

**How the developer set it up (for reference / repeating on another machine):**
1. Sign in with a Unity ID in the Editor: **Edit → Project Settings → Services** (or the cloud icon in the
   toolbar) → sign in → create or select an organization → create a new Unity Cloud project (or link this
   Unity project to an existing one) → note the **Project ID**.
2. In the Unity Cloud Dashboard (`cloud.unity.com`), open that project → **Authentication** service →
   **Identity Providers** → **Add Identity Provider** → add both **Username & Password** (needed for
   `AuthManager`'s Register/Login) and confirm Anonymous is available (no separate toggle needed, it's the
   SDK default) → **Cloud Save** service → enable it. All free-tier, no payment method required.
3. Back in the Editor, once Project Settings → Services shows the project as linked, just enter Play mode —
   `CloudSaveManager` will pick it up automatically, no code changes needed. Ask to have it re-verified once
   linked and I'll play-test an actual push/pull round-trip (write local progress → confirm it appears in the
   Unity Cloud Dashboard's Cloud Save data browser → clear local files → confirm they're restored from cloud).

**Stated goal (updated 2026-07-03):** Development happens Steam-first, but the actual release order is
inverted — mobile (Android + iOS) ships first, Steam release is uncertain and may happen later or never.
Single Unity project either way (no forking into separate Steam/mobile project copies — see reasoning below),
same as the existing platform-conditional pattern used by the achievement providers (`STEAMWORKS_INSTALLED`/
`GPGS_INSTALLED` define symbols, `LocalAchievementProvider` as the always-on source of truth).

**Why one project, not two:** Unity natively builds one project to multiple platforms via Build Settings
platform switching — no engine-level reason to fork. Forking means duplicating every future bugfix/feature
into two codebases forever. The sequencing uncertainty (mobile ships first, Steam maybe never) is itself an
argument *for* one project: if a forked "Steam version" never ships, that's wasted duplication for nothing.
The only case forking would make sense is if Steam and mobile became genuinely different games (different
economy/core loop) — nothing here suggests that; `TurnManager`, abilities, and `CurrencyManager` are shared
core across both.

**Backend choice holds regardless of Steam's fate:** UGS Relay + Lobby is needed for mobile multiplayer on its
own merits — that requirement doesn't come from wanting a shared Steam+mobile backend, it comes from needing
*any* multiplayer transport at all, and the mobile matchmaking pool (Android+iOS combined) needs Relay/Lobby
whether or not Steam ever exists. So even in the "Steam never ships" branch, UGS is still correct: Cloud
Save/Auth ride along on the same vendor at zero extra integration cost. Firebase would only be worth switching
to if the multiplayer transport decision changed away from NGO+Relay — it hasn't. Storefront-agnostic
auth-linking (Steam ticket + Google/Apple sign-in → one player ID) is a nice bonus if Steam does eventually
ship, not the driving reason to pick UGS.

**Practical implication:** don't invest further effort in Steam-specific polish (e.g. `SteamAchievementProvider`
activation, Steamworks App ID registration) until a Steam release is actually greenlit — it's already isolated
behind a define symbol at near-zero ongoing cost, so there's no rush. Conversely, start Apple/Google developer
account enrollment (identity verification, any required registrations) and IAP/monetization model decisions
now, in parallel with feature work — those have long, code-independent lead times and directly affect
`CurrencyManager` economy balance, so deciding late means redesigning the economy twice.

**Researched options for the shared backend:**
- **Unity Gaming Services — Authentication + Cloud Save + Relay + Lobby (recommended).** One SDK, works
  unmodified from the Steam desktop build and a mobile build (no storefront dependency), and Relay/Lobby is
  already the multiplayer transport pick above — so networking, matchmaking, and save data all come from one
  vendor with one integration instead of three. Authentication supports linking platform identities (Steam
  ticket auth, Google/Apple sign-in) to one underlying player ID, which is exactly what "same account, either
  platform" needs. Relay's free tier is confirmed at 50 avg monthly CCU (2,160,000 connectivity-minutes/month)
  before per-CCU billing kicks in. Lobby's free tier is a monthly data-volume allowance whose exact GB Unity
  no longer publishes in a fixed number — check Unity's pricing estimator at build time rather than relying on
  a hardcoded figure. Cloud Save and Authentication are also free-tier-first, no-payment-method-required to
  start. All fine for an indie launch and scale with revenue rather than requiring upfront infrastructure spend.
- **PlayFab — no longer recommended.** Microsoft cut PlayFab's free tier hard in March 2026 (Dev Mode capped
  at 1,000 lifetime accounts; free "Foundation Mode" requires shipping on Xbox). Was a strong default before
  this change; skip it now unless an Xbox release is also planned.
- **Firebase (Auth + Firestore) — solid fallback**, especially if the team ever leans mobile-first: Google-scale
  hosting, generous Spark free tier, fully storefront-agnostic (plain REST/SDK, works from Steam desktop just
  as well as mobile). Downside vs. UGS: it's a second vendor separate from whatever handles multiplayer
  transport, so two integrations instead of one.
- **Self-hosted Nakama — best if avoiding per-CCU vendor billing matters more than avoiding ops work.**
  Open-source game server (auth, storage, matchmaking, leaderboards, turn-based match support out of the box),
  Unity client SDK, cost is just your own server hosting instead of metered usage. Same
  storefront-agnostic property as the others. Worth revisiting if UGS costs become unpredictable at scale, but
  more setup/maintenance burden upfront than the managed options.

Revisit Nakama only if UGS billing becomes a real concern post-launch.

## Mobile gaps — priority work (mobile ships first, not Steam)
Previously filed as "only matters once mobile work starts" on the assumption Steam ships first — that
assumption was wrong given the actual release order (see "Save / sync" above). This is now near-term priority
work, not deferred backlog. Audited the codebase against a mobile release; backend (UGS) and the achievement
providers already cover mobile — everything below is mobile-only work not yet started:

- **Done — aiming/firing now shares one pointer code path for mouse and touch.** Added `PointerWorldPosition`/
  `PointerDown`/`PointerHeld`/`PointerUp` to `AbilityBase.cs` (`Assets/Scripts/Abilities/AbilityBase.cs`), backed
  by `UnityEngine.InputSystem.Pointer.current` (falls back to legacy `Input.mousePosition` only if no pointer
  device has produced input yet). `Pointer.current` auto-tracks whichever pointer device was last used — Mouse
  on desktop, the primary touch on a touchscreen — so the exact same drag-to-aim code drives both with zero
  `Application.platform` branching. Migrated all 8 live drag-to-aim abilities (`Pistol`, `Shotgun`, `Rpg`,
  `HandGrenade`, `Bomb`, `BlackHoleSkill`, `Teleport`, `BatHammerSkill`) off raw `Input.mousePosition`/
  `Input.GetMouseButton*`. `activeInputHandler` was already `2` ("Both") in Project Settings, so no Player
  Settings change was needed.
  - **Left alone, confirmed dead:** `AbilityController.cs` and `ObjectSpawnSkill.cs`
    (`Assets/Scripts/Abilities/`) still read raw mouse input, but neither is referenced by any other script,
    scene, or prefab (grepped the whole project) — leftover from the pre-`AbilityBase` architecture, superseded
    by the `WeaponBase`→`AbilityBase` refactor. Not migrated since they don't run.
  - **Verified in-editor** via Unity Editor MCP: compiled clean, entered Play mode, simulated taps by invoking
    `Button.onClick.Invoke()` directly on a skill icon — confirmed select (tap 1) → `isSelected=true,
    awaitingConfirmation=true`, confirm (tap 2) → `awaitingConfirmation=false, fireAllowed=true`, and tray
    collapse → cancels the live selection. No console errors.
- **Done — ability selection now has a touch/mouse UI path, not just keyboard.** `IAbilitySelectable` gained
  `Confirm()` (mirrors the existing Enter-key confirm step — `AbilityBase.Confirm()` sets `fireAllowed=true`,
  `awaitingConfirmation=false`, same as the keyboard path, which now just calls `Confirm()` too instead of
  duplicating the logic). `CharacterAbilities.ConfirmSkill(idx)` exposes that without a keyboard.
  `UIManager.OnSkillIconTapped(idx)` is the single entry point a UI `Button.onClick` calls: first tap on a slot
  selects it (same as pressing its number key), a second tap on the *same already-selected* slot confirms it
  (same as pressing Enter) — no separate "confirm" button needed, and tapping a different slot mid-confirmation
  switches straight to it, same as the keyboard already allowed. All 10 `SkillIcon{1-10}` GameObjects in
  `Canvas/SkillPanel/SkilssContainer` (`Assets/Scenes/SampleScene.unity`) got a `Button` component wired to it
  (`targetGraphic` = the icon's own `Image`). The scene's `EventSystem` already used
  `InputSystemUIInputModule` with `pointerBehavior: "Single Mouse Or Pen But Multi Touch And Track"` — i.e. uGUI
  buttons already responded to touch with zero extra code once wired.
  - **`ToggleSkillPanel.cs` rewritten into a real expand/collapse tray** (previously dead: `Update()` was
    empty and the component was disabled in the scene). Added `Toggle()`/`IsOpen`, a new always-visible
    `SkillTrayToggleButton` (bottom-right corner, sibling of the tray so it stays visible when the tray is
    closed) collapses/expands `SkillPanel`. Collapsing calls the new `UIManager.CancelSelection()` (→
    `currentAb.DeselectAll()`) — closing the tray reads as "put the weapon away," matching the
    TurnManager-confirmation-gated action rule rather than leaving a silently-armed weapon behind an invisible
    panel.
  - **Tooling gotcha hit this session:** the Coplay MCP `add_persistent_listener` tool is unreliable in this
    project — it failed to find methods taking an `int` parameter even when reflection confirmed they existed
    (`Type.GetMethod` found them fine), and separately threw a hard exception
    (`System.ExecutionEngineException: Illegal byte sequence`) wiring a zero-arg method, traced to
    `Assembly.GetCodeBase()` choking on the non-ASCII `ü` in this project's Windows path
    (`...Masaüstü\projects\CosmicRumble`). Worked around entirely by writing small one-off Editor scripts run
    via `execute_script` that call `UnityEditor.Events.UnityEventTools.Add{Int,Void}PersistentListener`
    directly — 100% reliable, same net result. Also hit: `save_scene` with just a name does a "Save As" into
    `Assets/` root instead of saving the currently-open scene in place (silently changes the scene's own
    `.path`) — had to fix via `EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity")` to
    restore the correct path and discard the stray duplicate. Don't use either MCP tool blindly again in this
    project; prefer `execute_script` for scene/event wiring.
- **Done — safe-area handling.** `Assets/Scripts/UI/SafeArea.cs` (standard `Screen.safeArea`-driven
  RectTransform shrink, orientation-agnostic) + a `SafeAreaRoot` wrapper under the main Canvas in
  `SampleScene.unity` protecting `SkillPanel`, `SkillTrayToggleButton`, `TurnTimerCircle`, and `CurrencyHUD`'s
  own runtime-built Canvas. Verified visually against a real notched device profile (iPhone 12 via Unity's
  Device Simulator) — HUD/timer and the skill tray no longer sit flush against the screen edges, matching the
  device's real notch/home-indicator geometry. Game is also now locked landscape-only in Player Settings
  (`allowedAutorotateToPortrait`/`PortraitUpsideDown` = 0, both landscape directions stay enabled) since the
  game is only ever played in landscape — confirmed with the user.
  - **Done (2026-07-03) — Canvas Scaler match-mode fix for landscape phone aspect ratios.** Checked
    `CameraController.cs` first: its projectile-framing zoom math already divides by `_cam.aspect`, so it
    correctly adapts to any aspect ratio — no camera code bug. The real issue was `Canvas` (`SampleScene`,
    the gameplay HUD) had `CanvasScaler.matchWidthOrHeight = 0` (pure width match). For a landscape-only game,
    width varies far more across real devices (16:9 up to 21:9+) than height, so width-matching means the HUD
    (tray, timer, currency badge) scales up/down with device WIDTH instead of staying a consistent size
    relative to the actually-fixed vertical budget — on a wide phone the bottom tray would eat a
    disproportionately large vertical fraction of the screen, cramping gameplay view, worse as aspect gets
    wider (backwards from what's desirable). Changed to `matchWidthOrHeight = 1` (match height) so the HUD's
    vertical footprint stays constant regardless of device width; extra width just reveals more world/background,
    which is fine for a Worms-style game. `CurrencyHUD.cs` (own runtime-built Canvas, previously left at the
    unset default of 0) got the same fix for consistency. The menu-side modal panels (`LobbyPanelUI`,
    `QuestsPanelUI`, `ShopPanelUI`, etc.) were left at their existing `0.5f` — they're fixed-size
    center-anchored dialogs, not edge-anchored HUD chrome, so match-mode only affects overall panel size, not
    functional squeezing; no bug there.
    - **Verified with a before/after screenshot comparison** at a real landscape phone resolution
      (2532×1170, iPhone 12's actual landscape pixel dimensions): before the fix the bottom skill tray icons
      rendered visibly larger (~22% oversized, matching the math: width-match scale factor 2532/1920=1.32 vs.
      height-match 1170/1080=1.08); after the fix they render at the correct reference-accurate size. No
      clipping/overlap was visible in either version at this specific aspect, but the fix removes the
      growing-with-width risk for wider aspects than this test covered.
- **Done — IAP infrastructure (placeholder product catalog).** Installed `com.unity.purchasing` (5.4.0, new
  v5 `StoreController` API, not the deprecated `IStoreListener`/`IDetailedStoreListener` surface). Added
  `Assets/Scripts/Economy/IAP/GemPackDefinition.cs` (5 consumable packs: 100/550/1200/2500/6000 Gem) and
  `IAPManager.cs` (connects, fetches products, purchases, confirms, awards Gem via `CurrencyManager.Add`),
  plus `Assets/Scripts/UI/ShopPanelUI.cs` (a "SHOP" button on the main menu opens it, lists all 5 packs with
  live localized price from the store and a BUY button per row).
  - **Product IDs (`gem_pack_100`, `gem_pack_550`, etc.) are placeholders** — they don't correspond to a real
    SKU yet. Once Play Console / App Store Connect products are created, their IDs must match these exactly
    (or the IDs in `GemPacks` updated to match whatever was actually registered there) — no other code change
    needed, `IAPManager` fetches by whatever `productId` strings are in the array.
  - **Verified in Play mode:** with no store configured, Unity IAP automatically falls back to FakeStore —
    all 5 products fetched successfully, `ShopPanelUI` correctly displayed live prices ($0.01, FakeStore's
    default placeholder) for every pack, confirmed visually via screenshot.
  - **Not verified: a full purchase completing end-to-end in the Editor.** `BuyGemPack()` calls
    `PurchaseProduct()` correctly and the store's `ConnectionState` does reach `Connected`, but
    `OnPurchasePending` never fires against FakeStore in this environment — traced to a documented,
    Unity-acknowledged bug where FakeStore's UI is unresponsive when the new Input System is active (this
    project uses `com.unity.inputsystem`), not a defect in this code. Tried the documented workaround
    (`IAP_FAKE_STORE_DEVELOPER_USER` scripting define for FakeStore "developer" no-UI mode) — made things
    worse (`Connect()` itself stopped completing), so it was reverted; `ProjectSettings.asset` scripting
    define symbols are back to empty, matching before this session. Real purchase-completion testing needs
    an actual Android/iOS build against a real (or sandboxed) store — appropriate anyway, since FakeStore was
    only ever going to validate the wiring, not real purchase behavior.
  - Gem package pricing/tiers were chosen as reasonable placeholders (not a business decision) — revisit
    before shipping.
- **Store-side setup (account/config work, not code):** Play Console (min API level, Data Safety form, Play
  Games Services resource XML), App Store Connect (App Privacy nutrition label, ATT prompt if ads/analytics are
  added, TestFlight), age rating, privacy policy URL.

## Yerçekimi düzeltmeleri + Online Leaderboard (kupa sistemi) — Done (2026-07-06)

### Yerçekimi — mermilerin gezegene çekilmeme bug'ı (kök neden + fix, canlı doğrulandı)
- **Kök neden:** `Projectile.prefab` (Pistol/RPG/Grenade mermilerinin base'i), `PF_BlackHoleProjectile.prefab`
  ve `TeleportOrbProjectile.prefab` üzerindeki `NetworkRigidbody2D.Awake()` body'yi koşulsuz Kinematic'e
  zorluyor ve bunu yalnızca `OnNetworkSpawn()` geri alıyor — offline'da spawn hiç olmadığı için mermiler
  kalıcı Kinematic kalıyor, `GravitySource`'un `AddForce` çekimi sessizce no-op oluyor ve mermiler dümdüz
  gidiyordu. Milestone 3'te karakterler için bulunan regresyonun (bkz. `GravityBody.Start()`) birebir aynısı,
  mermi tarafı o zaman gözden kaçmış. **Fix:** `Assets/Scripts/Utilities/NetworkPhysicsGuard.cs`
  (`EnsureDynamicWhenNotSpawned`) + tüm mermi scriptlerinin `Init()`/`Start()` yollarına çağrı
  (KineticProjectile, Projectile, HandGrenadeProjectile, BlackHoleProjectile, TeleportOrbProjectile,
  ProjectileBase). Silahların SpawnAndInit sırası her yerde Spawn()→Init() olduğu için online'da guard
  kendiliğinden devre dışı (IsSpawned=true → NGO otoriteyi yönetir).
- **İkinci yapısal sorun:** `GravitySource` script'i Planet_Interior'da, geniş çekim trigger'ı ise child
  `GravityTrigger`'da ve hiyerarşide Rigidbody2D yok — Unity trigger callback'lerini parent'a İLETMEZ, yani
  o child'ın `OnTriggerStay2D`'si GravitySource'a hiç ulaşmıyordu. Çekim bugüne kadar yalnızca sahnede
  Planet_Interior'ın kendi collider'ının elle trigger yapılmış olması sayesinde (ve gravityRadius'tan farklı
  bir yarıçapta) çalışıyordu. **Fix:** kuvvet uygulaması `GravitySource.FixedUpdate()`'e taşındı —
  gravityRadius içindeki tüm dinamik Rigidbody2D'lere `OverlapCircle` ile (body başına tek kez, uyuyanlara
  dokunmadan, `rb.mass` ile ölçekleyerek) uygulanıyor. Böylece etki alanı tam olarak gravityRadius, ivme
  kütleden bağımsız `gravityForce` ve `TrajectoryDots`/`IGravityStrategy` tahminiyle birebir aynı.
- `SinglePlanetGravity`/`MultiPlanetGravity` artık `gravityRadius`'a saygı duyuyor (tahmin ile gerçek fizik
  sınırı aynı).
- **Canlı doğrulama (Editor Play Mode, SampleScene):** spawn edilen pistol mermisi bodyType=Dynamic, gezegene
  doğru ölçülen ivme |a|=19.84 ≈ gravityForce(20), 0.84 sn'de yüzeye çarpıp doğru şekilde yok oldu. (İlk
  ölçümdeki ~34 değeri duvar-saati/fizik catch-up artefaktıydı; Time.time bazlı ikinci ölçüm doğru çıktı.)

### Online Leaderboard — Clash Royale tarzı kupa sistemi (kullanıcı isteğiyle galibiyet sayacından çevrildi)
- Paket: `com.unity.services.leaderboards` 2.3.4 (Coplay MCP ile kuruldu; araç derleme hatası varken
  çalışmadığı için önce leaderboard referansları geçici yorumlanıp kurulum sonrası geri açıldı).
- `Assets/Scripts/Cloud/LeaderboardManager.cs`: kupa mantığı — online maç galibiyeti **+30**, mağlubiyet
  **−20** (0'ın altına inmez), beraberlikte değişim yok; güncel toplam UGS Leaderboards'a gönderilir.
  Kupa aralığına göre lig adları (`GetLeagueName`: Asteroid/Moon/Planet/Star/Nebula/Galaxy League).
  Kayıtlı kullanıcı adı `UpdatePlayerNameAsync` ile leaderboard'a yansıtılır (misafirde no-op).
- **Önemli bulgu (canlı teşhisle bulundu):** statik `LeaderboardsService.Instance` bu projede HİÇ set
  edilmiyor — core, paketleri instance-tabanlı yolla (`IInitializablePackageV2.InitializeInstanceAsync`)
  başlatıyor ve o yol statici atlamıyor... atlıyor (paket kaynağında görülüyor: `Initialize()` statici set
  eder, `InitializeInstanceAsync()` etmez). Servis CoreRegistry'de kayıtlı ve sağlıklıyken statik erişim
  `ServicesInitializationException` atıyordu. **Doğru erişim:** `UnityServices.Instance.GetLeaderboardsService()`
  (LeaderboardManager.Service property'si; statik yalnızca yedek). Bu gotcha diğer UGS paketleri için de
  geçerli olabilir.
- `TurnManager.TriggerGameOver` → yeni `AnnounceMatchResultClientRpc(winnerClientId)`: online maç sonucunu
  TÜM client'lara duyurur (TriggerGameOver yalnızca server'da çalıştığı için client'lar kazanıp
  kaybettiklerini ancak böyle öğrenebilir; beraberlik = ulong.MaxValue → kupa değişimi yok). Offline maçlar
  kupa vermez (leaderboard yalnızca online).
- `Assets/Scripts/UI/LeaderboardPanelUI.cs`: ana menüde yeni "LEADERBOARD" butonu (buton kartı 8 butona göre
  büyütüldü) → sıra/isim/lig/kupa sütunlu, kendi satırı vurgulu, REFRESH'li panel (AchievementsPanelUI
  kalıbı). `MainMenuUI.EnsureProgressSingletons` bootstrap'ine LeaderboardManager + panel eklendi.
- **Doğrulama:** Editor Play Mode'da panel açılıyor, servis çağrısı UGS'ye ulaşıyor; beklenen tek uyarı
  `Leaderboard config could not be found` — çünkü **dashboard'da leaderboard henüz oluşturulmadı**.
- **KALAN MANUEL ADIM (kod yok):** cloud.unity.com → proje → Leaderboards → Add leaderboard:
  ID **`cosmic_trophies`**, Sort order **High to low**, Update type **Latest submission** (kupa
  düşebildiği için "Keep best" DEĞİL). Oluşturulana kadar panel boş liste + editor'da uyarı gösterir,
  oyun kırılmaz.

### Genel kontrol — bilinen kalan pürüzler (bu oturumda düzeltilmedi)
- `GravityBody.DominantSource = AllSources[0]` — çok gezegenli sahnede zıplama yönü "ilk" gezegene göre,
  en yakın/baskın gezegene göre değil; ikinci gezegen üzerindeyken yanlış yöne zıplama riski.
- `ProjectileBase.OnBecameInvisible` / `Projectile.destroyOnInvisible` ekran dışına çıkan mermiyi anında
  yok ediyor — uzun yörüngeli atışları erken öldürebilir (kamera mermiyi takip ettiği için pratikte nadir).
- Ana menü buton ikonları (▶ ⇄ ★ ◆ ♛ ⚙ ✕) LiberationSans SDF'te yok, hepsi □ görünüyor (önceden beri var;
  fallback font eklenmeli).
- Online maç sonunda game-over UI yalnızca host'ta görünüyor (TriggerGameOver server-only; kupa RPC'si sonucu
  client'lara iletiyor ama UI gösterimi ayrı, kapsam dışı bırakıldı).

## Kalan pürüzler çözüldü + Dereceli/Dostluk maç ayrımı — Done (2026-07-06, 2. tur)

- **Leaderboard dashboard DOĞRULANDI (uçtan uca):** `cosmic_trophies` kullanıcı tarafından oluşturuldu
  (High to low / Latest). Canlı test: fetch uyarısız boş liste ✓; `ReportOnlineMatchResult(true)` → +30
  kupa gönderildi → tabloda rank #1, score=30 göründü ✓; test verisi 0'a sıfırlanıp geri gönderildi
  (tablo temiz bırakıldı). Not: editor oturumu misafir olduğu için isim anonim UGS adı
  ("EasyAstonishedOstrich#26782") göründü — kayıtlı kullanıcıda `SyncPlayerNameAsync` gerçek adı yazar.
- **DominantSource düzeltildi** (`GravityBody.FixedUpdate`): körlemesine `AllSources[0]` yerine en yakın
  gezegen (gravityRadius içindekiler öncelikli). Zıplama yönü ve `CameraController` rotasyonu artık çok
  gezegenli sahnede doğru gezegene göre.
- **Ekran dışı mermi ölümü yumuşatıldı:** `ProjectileBase`/`Projectile.OnBecameInvisible` artık anında yok
  etmiyor — `offscreenGraceTime` (3 sn) içinde `OnBecameVisible` gelirse iptal; gezegen arkasından dolanan
  yörünge atışları yaşıyor, dönmeyenler yine temizleniyor (TTL de ayrıca duruyor).
- **Ana menü ikon glyph'leri kaldırıldı** (▶ ⇄ ★ ◆ ♛ ⚙ ✕ → yalnız metin): LiberationSans SDF'te olmayan
  karakterler □ görünüyordu; mobil için temiz metin-only butonlar. Ekran görüntüsüyle doğrulandı.
  (Kalıcı ikon istenirse ileride fallback font asset'i eklenebilir.)
- **Online maç sonu artık HER İKİ makinede işleniyor:** `TurnManager.TriggerGameOver` yeniden yapılandırıldı —
  online'da tüm maç-sonu yerel işleri (game-over UI, XP/Gold/sandık, başarım event'leri, ses, kupa) yeni
  `AnnounceMatchResultClientRpc(winnerClientId, winnerName, matchDuration, totalShots)` içinden
  `FinishMatchLocally()` ile her makinede kendi yerel sonucuna göre çalışıyor (host çift ödül almaz, client
  artık game-over ekranını ve ödüllerini görür). Offline hotseat eski davranışıyla `FinishMatchLocally`'yi
  doğrudan çağırıyor. Beraberlik = winnerClientId=ulong.MaxValue → kupa değişimi yok, iki taraf da "kaybetti"
  akışını görür (eski host davranışıyla tutarlı).
- **Dereceli/Dostluk ayrımı (Clash Royale kuralı):** `NetworkBootstrap.IsRankedMatch` — `QuickMatchAsync`
  true, `HostSessionAsync`/`JoinSessionAsync` false, `LeaveSessionAsync` sıfırlar; client reconnect'i maçın
  dereceli durumunu korur. Kupa yalnızca dereceli maçlarda değişir. Quick Match beklerken katılım kodu artık
  GÖSTERİLMİYOR (koda katılan arkadaş taraflar arasında dereceli/dostluk uyuşmazlığı yaratırdı).
- **Online lobi yeniden çerçevelendi (mobil ana akış = Quick Match):** üstte büyük "HIZLI EŞLEŞME — DERECELİ"
  kartı (+30/−20 kupa ipucu), altta "ARKADAŞINLA OYNA — dostluk maçı, kupa değişmez" başlığı ile
  "KOD OLUŞTUR" (kodu arkadaşına gönder) ve "KODA KATIL" kartları; "← BACK" → "GERİ". Ekran görüntüsüyle
  doğrulandı.

## Mobil görsel yenileme (menü + online lobi + leaderboard) — Done (2026-07-06, 3. tur)

- **`Assets/Scripts/UI/UiKit.cs` (yeni):** projede hiç UI sprite asset'i olmadığı için yuvarlatılmış köşe
  sprite'ı runtime'da SDF ile üretilir (antialias'lı, 9-slice, cache'li). `UiKit.Round(img, cornerScale)`,
  `UiKit.Shadow(go)` (yumuşak alt gölge) ve `UiKit.ButtonColors(normal)` (hover/press renklerini normal
  renkten türetir) tüm yeni stil yüzeylerinde kullanılıyor.
- **Ana menü mobil yatay düzene geçti:** 8 butonluk dar dikey liste yerine geniş yuvarlatılmış kart içinde
  2 büyük birincil buton (PLAY / ONLINE, 388×92) + 2 sütun × 3 satır ikincil grid (376×72) — dokunma
  hedefleri büyüdü, yatay ekran alanı kullanılıyor. Başlık arka planı ve settings kartı/sekmeleri/back
  butonu da yuvarlatıldı; settings başlığındaki □ (⚙) kaldırıldı; eski butonlardaki sol şerit (yuvarlatmayla
  çakışıyordu) kaldırıldı.
- **Online lobi:** kartlar yuvarlatık+gölgeli; OYNA birincil eylem olarak büyük ve YEŞİL (320×68); KOD
  OLUŞTUR/KATIL 290×62'ye büyütüldü; İPTAL/GERİ nötr koyu renge alınıp büyütüldü; kod input'u yuvarlatıldı.
- **Leaderboard:** kart 760 genişliğe çıkıp yuvarlatıldı+gölgelendi, satırlar yuvarlatık (52 yükseklik,
  6 aralık), CLOSE/REFRESH büyütüldü.
- Hepsi Editor Play Mode ekran görüntüleriyle doğrulandı (menü, lobi, leaderboard). Achievements/Quests/Shop
  panelleri ESKİ düz stilde kaldı — aynı UiKit çağrılarıyla geçirilebilir, ayrı iş.

## Brawl Stars tarzı lobi ana ekranı — Done (2026-07-06, 4. tur)

Araştırma (Brawl Stars UI analizleri + mobil lobi kalıpları) sonrası ana menü "buton listesi"nden
"lobi/hub" düzenine geçirildi:
- **Sağ-alt: BÜYÜK SARI OYNA** (420×124, koyu yazı) → OnlineLobbyPanelUI; üstünde "DERECELİ • HIZLI
  EŞLEŞME" bilgi çipi; solunda ikincil "YEREL MAÇ" (hotseat). Yatay tutuşta sağ başparmak bölgesi.
- **Üst bar:** solda profil çipi (kullanıcı adı/Misafir + canlı kupa sayısı + lig adı; tıklayınca
  Sıralama açılır), sağda Gem + Gold çipleri (CurrencyManager'a canlı abone, tıklayınca Market;
  MainMenuAuthButton'ın sağ-üst kartıyla çakışmayacak şekilde konumlandı).
- **Sol ray:** MARKET / GÖREVLER / BAŞARIMLAR / SIRALAMA "chunky" butonlar; alt-sol: AYARLAR + ÇIKIŞ (nötr).
- **MakeChunkyBtn:** Brawl Stars görünümü için 3B kenarlı buton (koyu alt kenar 6px + yüz plakası).
- **Dekor:** UiKit.CircleSprite (yeni, AA daire) ile alt ufukta gezegen + mavi atmosfer halesi + küçük ay.
  Başlık bloğu küçültüldü (52pt) — merkez artık ferah.
- AchievementsPanelUI'nin sol-üstteki eski kısayol butonu kaldırıldı (sol rayla çakışıyor + mükerrerdi).
- Etiketler Türkçe'ye çevrildi (lobi paneliyle tutarlı). Ekran görüntüleriyle doğrulandı; derleme temiz.

## Yeni nesil UI turu: panel restyle + sessiz giriş + OYNA v2 — Done (2026-07-08)

Kapsam (kullanıcı isteği): (1) Achievements/Quests/Shop panellerini UiKit stiline geçir, (2) Brawl Stars /
modern mobil UI kalıplarını araştır, (3) OYNA butonunu farklılaştır, giriş/kayıt akışını mobil kalıba çevir
(görünür login yok — bir kere sessiz giriş), UI'ı "yeni nesil" yap. Oyun artık **mobil-only** kabul ediliyor
(Steam yok gibi davranılıyor — kullanıcı açıkça söyledi; TODO'daki Steam bölümleri tarihçe olarak duruyor).

- **UiKit büyüdü (yeni araçlar, hepsi runtime-üretimli sprite/bileşen):** `Stroke` (ince açık kontur — cam
  kenar hissi, `RoundedOutlineSprite`), `Gradient` (dikey vertex gradyanı, `UiVerticalGradient:BaseMeshEffect`),
  `Press` (`UiPressScale` — dokununca %94'e küçülme), `Pop` (`UiPopIn` — panel açılışında ölçek+alfa,
  ease-out-back), `Pulse` (`UiPulse` — birincil buton nefes animasyonu), `CloseButton` (mobil standart:
  kartın sağ-üst köşesinden taşan kırmızı X), `GlowSprite` (radyal solan leke — MainMenuUI'ın nebula
  "Glow"ları düz Image'dı ve dikdörtgen görünüyordu, artık yumuşak).
  - Gotcha: `UiPulse`/`UiPressScale` ikisi de localScale yazar — aynı objeye ikisini birden ekleme (OYNA'da
    yalnız Pulse var).
- **Achievements/Quests/Shop panelleri yeniden stillendi** (820×600 yuvarlatık+konturlu+pop kart, köşe X,
  yuvarlatık satırlar/ilerleme çubukları, Türkçe metinler): Başarımlar'da nadirlik renkli ikon dairesi
  (eski kare şerit yerine) ve Sıradan/Nadir/Epik/Efsanevi etiketleri; Görevler'de pill sekmeler
  (GÜNLÜK/HAFTALIK/AYLIK); **Market tamamen yeni düzen** — satır listesi yerine 5 yan yana gradyanlı paket
  kartı (gem dairesi + miktar + fiyatlı yeşil SATIN AL), 1200'de "POPÜLER", 6000'de "EN İYİ DEĞER" rozeti.
  LeaderboardPanelUI de aynı dile getirildi (SIRALAMA başlık, YENİLE, köşe X, Türkçe durum metinleri).
- **Sessiz tek seferlik giriş (Supercell kalıbı):** `MainMenuUI.BootstrapSequence` artık cloud init'ten sonra
  oturum yoksa `LoginAsGuest()`'i sessizce bekliyor → `IsLoggedIn=True IsGuest=True` hiçbir UI göstermeden
  (canlı doğrulandı). Sağ-üstteki kalıcı LOG IN kartı (`MainMenuAuthButton.cs`) **silindi** (+ MenuScene'deki
  GO'su MCP ile kaldırıldı); para çipleri gerçek sağ-üst köşeye taşındı. `LobbyPanelUI`'daki "LOG IN AND
  START" kapısı kaldırıldı. Hesap bağlama tek yerde: Ayarlar → HESAP sekmesi ("Misafir olarak oynuyorsun...
  hesabını bağla" + HESAP BAĞLA/ÇIKIŞ YAP) → `LoginPanelUI` yeniden çerçevelendi: "HESABINI BAĞLA" başlığı,
  ilerleme-taşıma değer önerisi metni, GİRİŞ YAP / YENİ HESAP OLUŞTUR (misafir ilerlemesini devralır
  ipucuyla), PLAY AS GUEST butonu kalktı (misafir zaten varsayılan), UiKit stiline geçti.
- **OYNA v2:** açık→koyu altın dikey gradyanlı yüz + beyaz cam kontur + koyu altın 3B kenar + sürekli hafif
  nefes (Pulse) + butona gömülü "HIZLI EŞLEŞME • DERECELİ" alt satırı (ayrı mod çipi kaldırıldı). Profil
  çipine baş harfli avatar dairesi eklendi. Tüm chunky buton/çiplere Press mikro-etkileşimi, panellere Pop.
- Ayarlar Türkçeleşti (AYARLAR, SES/GRAFİK/KONTROLLER/HESAP, GERİ/UYGULA...); slider etiketlerinin slider
  altında ezilmesi düzeltildi (öncesinde de vardı: "Master Volume" → "Ana Se" gibi kesiliyordu).
- **Doğrulama:** Editor Play Mode ekran görüntüleriyle: hub (yumuşak nebula + yeni OYNA + avatar çipi),
  GÖREVLER, BAŞARIMLAR, MARKET, SIRALAMA (canlı UGS verisiyle), HESABINI BAĞLA diyaloğu, AYARLAR/HESAP
  sekmesi. Derleme temiz. `AuthState` script'iyle sessiz misafir girişi doğrulandı.
- **Tooling gotcha (yeni):** Coplay `save_scene(scene_name)` aktif sahneyi `Assets/<ad>.unity`'ye **save-as**
  yapıyor (`Assets/Scenes/...` yolunu korumuyor) — MenuScene yanlışlıkla köke kopyalandı; `execute_script`
  ile `EditorSceneManager.SaveScene(scene, "Assets/Scenes/MenuScene.unity")` + stray asset silme ile
  düzeltildi. Bundan sonra sahne kaydetmek için save_scene yerine execute_script kullan.

## Brawl Stars menü revizyonu: Misafir kalktı, giriş kapısı, eğik butonlar — Done (2026-07-08, 2. tur)

Kullanıcı geri bildirimi üzerine ("beğenmedim; misafir olmayacak — sadece test için; çıkış butonu ana menüde
olmayacak; Brawl Stars menüsünü iyi incele; giriş ekranı yalnızca hesabı bağlı olmayana, girince ana menüye"):

- **"Misafir/Guest" UI'dan tamamen kalktı.** Yeni `Assets/Scripts/Utilities/PlayerIdentity.cs`: görünen ad
  tek kaynaktan — bağlı hesapta kullanıcı adı, değilse cihazda bir kez üretilip PlayerPrefs'e yazılan kozmik
  takma ad (ör. "Pulsar630"; ASCII prefix listesi — UGS UpdatePlayerNameAsync özel karakter reddediyor).
  Kullanan yerler: ana menü profil çipi, `GameInitializer` (hotseat karakter adı — eskiden misafirde "Guest"
  görünüyordu), `LobbyPanelUI`, `LeaderboardManager.SyncPlayerNameAsync` (artık misafir no-op DEĞİL — anonim
  oturumda da takma adı UGS'ye yazar, leaderboard'da "EasyAstonishedOstrich" tarzı ad kalmaz).
- **Giriş kapısı (tek giriş ekranı):** `MainMenuUI.BootstrapSequence` sonunda hesap bağlı değilse
  `LoginPanelUI.Show(dismissable:false)` — cihazda kapatılamaz (X/ESC yok, başlık "GİRİŞ"); giriş/kayıt
  sonrası panel kapanır, oyuncu ana menüde. Editor'da `dismissable:true` (bağlantısız oturum yalnızca test
  için — hotseat testleri girişe takılmasın). Ayarlar → HESAP BAĞLA aynı paneli kapatılabilir halde açar
  (başlık "HESABINI BAĞLA"). Arka plandaki sessiz anonim UGS oturumu duruyor (Register'ın ilerlemeyi
  devralması için şart) ama bir UI durumu değil.
- **ÇIKIŞ butonu ana menüden kaldırıldı** (`OnQuit` ile birlikte — mobil oyunda çıkış butonu olmaz).
- **Brawl Stars menü imzaları:** `UiKit.Skew` (`UiSkew:BaseMeshEffect`, yatay shear 0.10 — tüm chunky
  butonlar + OYNA paralelkenar) ve `UiKit.BrawlText` (kalın italik + koyu SDF kontur — tüm buton yazıları,
  OYNA yazısı beyaz-konturlu). Düzen Brawl Stars'a hizalandı: sol ray 3 buton (MARKET/BAŞARIMLAR/SIRALAMA),
  **GÖREVLER alt-sol** (BS'nin quest slotu), **AYARLAR üst-sağda küçük nötr çip** (para çiplerinin altında),
  alt-sağ OYNA + solunda YEREL MAÇ. OYNA yüzündeki Stroke kaldırıldı (child stroke skew'lenmiyordu,
  uyumsuz görünürdü).
- **Bug fix (loglardan yakalandı):** `UiPopIn.OnEnable`'daki `GetComponent<CanvasGroup>() ?? AddComponent`
  Unity fake-null yüzünden `MissingComponentException` fırlatıyordu — açık `== null` kontrolüne çevrildi.
  (Play Mode çıkışındaki `NetworkManager.OnDestroy` NullRef'leri NGO paketinin kendi bilinen kapanış
  gürültüsü, bizim kod değil.)
- **Doğrulama:** Editor Play Mode — açılışta GİRİŞ ekranı (X'siz) otomatik geldi; arkada ana menü yeni
  düzende ("Pulsar630" profil, eğik butonlar, ÇIKIŞ yok); OYNA'nın sağ marjı `GetWorldCorners` ile ölçüldü
  (1884/1920 — capture aracının kırpması taşma gibi göstermişti, gerçek taşma yok); görev paneli pop
  animasyonuyla hatasız açıldı. Derleme temiz.

## Brawl Stars görsel dili — gerçek referansla tam revizyon — Done (2026-07-08, 3. tur)

Kullanıcı gerçek Brawl Stars ekran görüntüleri paylaştı ("tasarım hala kötü", "önemli olan yerleşim",
"3. resimdeki sağdan açılır pencere çok mantıklı") — tasarım artık tahmin değil, birebir referansla yapıldı:

- **Font (en büyük fark buydu):** `Assets/Fonts/TitanOne-Regular.ttf` (Google Fonts, OFL) indirildi;
  editor script ile dinamik TMP font asset'i üretilip (`Assets/Fonts/TitanOne SDF.asset`) **TMP Settings
  varsayılan fontu yapıldı** — tüm programatik UI otomatik geçti. `UiKit.BrawlText` artık: beyaz dolgu +
  kalın koyu SDF kontur, DÜZ (italik/fake-bold kapatıldı — Titan One zaten kalın display font).
  - **Glif gotcha:** Titan One'da büyük **'İ' yok** (U+0130) — LiberationSans fallback'e düşer, ince
    sırıtır. Lilita One denendi: daha kötü (ğşĞŞİ yok), silindi. Çare: alt etiketlerde küçük harf kullan
    ("Hızlı Eşleşme • Dereceli"); başlıklardaki GERİ/GRAFİK gibi İ'ler fallback ile kalıyor (kabul edildi).
  - MakeCycler'ın ◀▶ okları "<" ">" yapıldı (iki fontta da glif yok, □ görünüyordu).
- **Buton dili düzeltildi:** renkli yüzlü butonlar yerine BS'deki gibi **koyu füme plakalar**
  (`MakePlate`/`MakeBrawlBtn`: PlateDark + koyu alt kenar + hafif skew + solda renkli daire-harf ikon
  rozeti + beyaz konturlu yazı). Renk yalnız vurguda: MARKET ve OYNA sarı (BS DÜKKAN/OYNA), fiyatlar yeşil.
- **Yerleşim (BS ana ekranından birebir):** üst-sol: [avatar+ad plakası][kupa kutusu: K rozeti + sayı +
  lig] iki ayrı plaka (ikisi de Sıralama açar); üst-sağ: [gold][gem] plakaları + **☰ menü butonu**
  (3 beyaz çubuk, Image ile çizili); sol kolon: MARKET (sarı) + BAŞARIMLAR; alt-sol: GÖREVLER; alt-orta:
  **mod plakası** ("HIZLI EŞLEŞME / Dereceli • Galibiyet +30 kupa" — BS'nin harita kutusu konumu, tıklayınca
  online lobi); alt-sağ: büyük sarı OYNA. Footer tamamen kaldırıldı (sürüm Ayarlar'ın altına taşındı).
- **☰ Çekmece (kullanıcının özellikle istediği):** sağdan 0.14s ease-out ile kayan koyu plaka listesi —
  AYARLAR / SIRALAMA / YEREL MAÇ / HESAP (HESAP → Ayarlar'ın Hesap sekmesi); dışına tıklayınca kapanır
  (şeffaf karartma butonu). `SetDrawer(bool)` + `SlideDrawer` coroutine, MainMenuUI içinde.
- **Arka plan:** simsiyah uzay yerine canlı BS lobisi: mor dikey gradyan + solda mavi/sağda pembe dev
  radyal glow + **soluk desen dokusu** (`BuildPattern`: alfa 0.022, döndürülmüş yuvarlatık karolar —
  BS kurukafa deseninin karşılığı) + mevcut yıldız alanı/gezegen ufku.
- **Ayarlar ekranı BS mavisine geçti** (2. referans görüntü): tam ekran mavi gradyan + desen, beyaz
  konturlu AYARLAR başlığı, mavi sekme plakaları, koyu GERİ.
- **Doğrulama:** Play Mode ekran görüntüleri — hub (yeni yerleşim, yumuşak desen, OYNA alt yazısı düzgün),
  çekmece açık hali (BS 3. görüntüdeki dizilişle aynı), mavi AYARLAR, MARKET paneli yeni fontla. Derleme
  temiz, konsolda yeni hata yok.

## Oyun Modları (1v1 / FFA / Takım) + Parti Lobisi
Done (2026-07-10) — kullanıcı isteği: "8 arkadaşımla oynayabilmeliyim, lobide toplanıp mod seçilmeli".
Proje daha önce tamamen 1v1'e kilitliydi (session `MaxPlayers=2` sabit, `TurnManager` "son karakter
kazanır" mantığı, davet akışı tek arkadaşa özel). Şimdi 1v1, FFA (3-8 kişi), 2v2, 3v3, 4v4, 2v2v2v2
hepsi destekleniyor — lobi kapasitesi kullanıcı kararıyla **max 8** ile sınırlandı, bu yüzden 9 oyuncu
gerektiren 3v3v3 kapsam dışı bırakıldı (yalnızca 2 takımlı + 2v2v2v2 dörtlü mod var, 3 takımlı mod yok).

- **`GameModeDefinition.cs`** (yeni): `GameModeType` enum + her modun takım sayısı/boyutu. `LobbyData`
  atıl `GameMode` string'i yerine `SelectedMode`/`FfaPlayerCount`/`PartyMembers` aldı.
- **`GravityBody.teamId`** (yeni `NetworkVariable<int>`, `isActive` ile aynı desen) — takımsız modlarda
  (Duel1v1/Ffa) her oyuncu kendi tekil takımı, takım modlarında round-robin paylaşılan id. İsim etiketi
  takım rengiyle boyanıyor (**gövde sprite'ı BİLEREK boyanmadı** — `ShieldSkill.cs` aynı SpriteRenderer'ı
  kalkan efekti için kullanıyor, çakışsaydı kalkan kapanınca renk silinirdi).
- **`TurnManager.CheckGameOver`**: "son karakter" yerine "hayatta kalan farklı takım sayısı ≤1" —
  takımsız modlarda eski davranışla birebir aynı, takım modlarında bütün takım elenene kadar bitmiyor.
  **Bulunup aynı geçişte kapatılan kritik regresyon**: teamId varsayılan 0 olduğu için bu değişiklik
  tek başına HER maçı ilk karede bitirirdi — `GameInitializer`/`NetworkPlayerSpawner` artık spawn
  anında gerçek takım ataması yapıyor.
- **`PartyLobbyPanelUI.cs`** (yeni, eski `FriendLobbyPanelUI`'nin — sabit 2 slot — yerine): host mod
  seçer (FFA için 3-8 stepper), PARTİYİ KUR özel bir UGS session açar (MaxPlayers moda göre), 3x3 roster
  ekranından istediği kadar arkadaşı aynı session koduna davet edebilir (`FriendsManager` üzerinden,
  tek tek). Misafir tarafı yalnızca canlı "X/N katılımcı" sayacı görüyor — **isim bazlı tam roster
  senkronu yok** (clientId↔PlayerId eşlemesi/NetworkList gerektirir, kapsam dışı bırakıldı).
- **Bilinçli kapsam kararı**: Quick Match (herkese açık, dereceli, +30/−20 kupa) **1v1 olarak kaldı** —
  yeni modlar yalnızca özel parti lobisinden (hep dostluk maçı) erişiliyor. Bu sayede `DUELLO_SAMPIYONU`,
  `REKABETCI` ve kupa formülü (`LeaderboardManager`) hiç değiştirilmeden doğru kalıyor.
- **Dostane ateş filtresi**: `CombatEventReporter`, `TurnManager.CurrentShooter` (zaten var olan
  "şu an ateş eden karakter" takibi) ile hedefin takımını karşılaştırıp `HERKESE_MEYDAN`/`SOSYAL_KELEBEK`
  gibi "N farklı rakip" başarımlarının takım arkadaşı/kendine isabetle şişmesini engelliyor.
- **`INTIKAM` düzeltildi**: artık maçın nihai kazananını değil, `FireDefeatedBy` ile gerçek öldürücüyü
  hedef alıyor (FFA/takımda bunlar farklı kişiler olabilir) — yeni `AchievementEvents.OnDefeatedBy`.
- **Play-tested (Coplay MCP)**: offline hotseat Duel1v1 (2 karakter, ilk karede bitmiyor — regresyon
  testi), Team2v2 (4 karakter, round-robin takım ataması, 2 takım ayrıyken bitmiyor), parti lobisi host
  akışı (mod seç → FFA stepper → PARTİYİ KUR → gerçek UGS session kuruldu, hatasız). Bu geçişte bir
  gerçek bug bulundu ve düzeltildi: `PartyLobbyPanelUI.BuildRosterRoot()` inaktif hiyerarşide
  `UiKit.BrawlText()` çağırıyordu (WardrobePanelUI'da daha önce görülenle aynı TMP outline/OnEnable
  bug'ı) — sıra düzeltildi.
- **Kalan/bilinçli ertelenen işler**:
  - Gerçek çok-cihazlı online FFA/takım testi (tek geliştirici ortamı, hiç yapılamadı — arkadaş
    daveti/presence testiyle aynı kısıtlama, bkz. yol haritası madde 2).
  - Parti lobisinde host'un takımları elle sürükle-bırak ile yeniden atayabilmesi yok — şu an takım
    ataması yalnızca katılım sırasına göre otomatik (round-robin).
  - `KOZMIK_EKIP` hâlâ tek arkadaş id'si takip ediyor (parti akışında yalnızca ilk davet edilen arkadaş
    için ilerliyor) — gerçek grup takibi ayrı bir iş.
  - `KARA_DELIK_USTASI` (Black Hole çoklu-çekim) hâlâ kendi ayrı sayaç yolunda, dostane ateş filtresi
    almadı.
  - Misafir tarafında diğer katılımcıların isim bazlı roster senkronu yok (yukarıda açıklandı).

## Terrain yıkımı performans düzeltmesi + Kara Delik VFX yenileme — Done (2026-07-14)

### DestructiblePlanet patlama maliyeti (~60-87ms/patlama → ~7-10ms/patlama)
- Play Mode'da gerçek ölçümle bulundu: her `ExplodeWithForce` çağrısı (mermi/RPG/bomba/el bombası
  isabeti) 60-87ms sürüyordu — 60fps'te 16.6ms'lik frame bütçesinin 4-5 katı, her isabette gerçek
  bir donma. Kök neden ayrıştırılarak (piksel döngüsü / `Apply` / `Sprite.Create` / collider rebuild
  ayrı ayrı `Stopwatch` ile ölçüldü) `Sprite.Create(..., generateFallbackPhysicsShape: true)`'ın
  patlama yarıçapından BAĞIMSIZ sabit ~90-140ms harcadığı bulundu — bu API her çağrıldığında TÜM
  1280x1280 texture'ın alfa hattını yeniden tarıyor (dirty-region boyutundan bağımsız). `GetPixel`/
  `SetPixel` döngüsü de büyük yarıçaplarda (RPG/bomba) ayrıca pahalıydı (149ms'e kadar).
- **Fix (`Assets/Scripts/Planet/DestructiblePlanet.cs`):**
  1. `GetPixel`/`SetPixel` → `Start()`'ta bir kez alınan `Color32[] pixels` cache'i üzerinden dizi
     indeksleme, döngü sonunda tek seferlik `SetPixels32`+`Apply()`.
  2. Görsel güncelleme artık Sprite'ı hiç yeniden OLUŞTURMUYOR — `Apply()` zaten aynı Sprite'ın
     referans aldığı texture'ı günceller, yeni Sprite şart değildi (eski kod her patlamada
     gereksiz yere yeniden yaratıyordu).
  3. Collider artık ayrı, `physicsDownsampleFactor` (varsayılan 8x, Inspector'dan ayarlanabilir,
     [1,12] aralığı) kadar küçültülmüş tek seferlik bir yardımcı texture'dan üretiliyor
     (`RebuildColliderFromAlpha`, eski `RebuildCollider`'ın yerini aldı) — küçültülmüş sprite'ın
     `pixelsPerUnit`'i de aynı oranda küçültülerek (`ppu/factor`) elle ölçekleme gerekmeden doğru
     local-unit uzayında şekil üretiliyor. Görsel kalite hiç etkilenmiyor (runtimeTex/sr.sprite'a
     dokunulmuyor), yalnızca collider'ın köşe hassasiyeti düşüyor (karakter ölçeğinde fark edilmiyor).
- **Ölçüldü (Play Mode, gerçek gezegen, aynı yöntemle önce/sonra):** r=0.3: 65.8→7.1ms, r=1.0:
  60.7→7.0ms, r=2.0: 79.2→8.8ms, r=3.0: 86.8→10.5ms — ortalama ~8-9x kazanç, 60fps bütçesinin
  rahatça altında.
- **Doğrulama:** derleme temiz, Play Mode'da runtime hatası yok, collider fonksiyonel
  (`pathCount=4`, 124 nokta, `isTrigger=false`) patlama sonrası, ekran görüntüsüyle görsel
  (dairesel çentikler temiz, bozulma yok) doğrulandı. Diğer patlama çağıran sistemler
  (BlackHoleZone/BlackHoleProjectile'ın `dp.ExplodeWithForce`, Bomb/Grenade/RPG/Kinetic) aynı yoldan
  geçtiği için otomatik faydalanıyor, ayrı bir iş gerekmedi.

### Kara Delik VFX — indirilmiş GIF yerine kullanıcının kendi ürettiği sprite sheet
- Eski `BlackHoleGif` görseli internetten indirilmiş bir Tenor GIF'ten çıkarılmış 90 ayrı kare
  PNG'ydi (belirsiz lisans, dosya adı `...gTCLX76XXbiwlrts...` bir Tenor GIF ID'si) — kullanıcının
  sağladığı 8 kareli (4x2) el çizimi tarzı vortex sprite sheet ile değiştirildi.
- Siyah arkaplan alfa-key ile şeffaflaştırıldı (`alpha = max(R,G,B)`, Python/Pillow) — kenarlarda
  halo/leke yok, ekran görüntüsüyle doğrulandı.
- `Assets/Art/Sprites/BlackHoleVortex/BlackHoleVortex_Sheet.png` olarak içe aktarılıp Unity'de 8
  sprite'a dilimlendi (4x2 grid, ppu=50). Mevcut `BlackHoleGif.anim` (12fps, 0.67s loop) bu yeni
  karelerle güncellendi — `BlackHoleProjectile`/`BlackHoleZone`/prefab/controller tarafında hiçbir
  kod değişikliği gerekmedi (aynı `gifPrefab` referansı, `Assets/Art/Sprites/Projectiles/
  BlackHoleGif.prefab`).
- Eski 90 kare PNG (+ meta) silindi.

### Sıradaki adımlar
- `physicsDownsampleFactor` şu an tüm gezegenler için sabit Inspector değeri (8) — çok küçük/çok
  büyük gezegen varyantları eklenirse yarıçap/çözünürlük oranına göre ayrıca ayarlanabilir.
- Kara delik görseli için üretim akışı (alfa-key + grid dilimleme) tekrarlanabilir — ileride başka
  görsel efektler (patlama, kalkan vb.) için de kullanıcı kendi ürettiği görseli aynı yöntemle
  eklettirebilir.
- Genel proje kontrolünde (aynı geçiş) tespit edilen ama henüz dokunulmayan diğer eksikler için
  yukarıdaki "YAYIN YOL HARİTASI" bölümüne bak (Google Play Console kurulumu, gerçek cihaz build
  testi, 150 kostüm sprite'ı, yasal metin onayı gibi kod-dışı kalemler hâlâ öncelikli).

## Güvenlik denetimi + düzeltmeler — Done (2026-07-14, 2. tur)

Kod tabanı sistematik olarak tarandı (ekonomi/IAP client-authority, multiplayer RPC yetkilendirmesi,
auth/credential yönetimi, hardcoded secret, save-data bütünlüğü). Bulunanların kod-seviyesinde
düzeltilebilecek kısmı bu geçişte çözüldü; sunucu/backend altyapısı gerektirenler (Cloud Code vb.)
açıkça "yapılamadı, şu şekilde yapılmalı" diye işaretlendi — sahte/yarım bir "çözüldü" izlenimi
verilmedi.

### Düzeltildi

1. **Ability-fire [ServerRpc]'lerinde sunucu tarafı tur kontrolü eksikti.** `Pistol`/`Rpg`/
   `Shotgun`/`HandGrenade`/`Teleport`/`BlackHoleSkill` (`FireServerRpc`/`FirePelletsServerRpc`),
   `BatHammerSkill` (`SwingServerRpc`), `ShieldSkill` (`ActivateShieldServerRpc`) — "sırası bende
   mi" kontrolü yalnızca client-side `AbilityBase.Update()`'teydi; sunucu tarafındaki RPC handler
   hiçbir doğrulama yapmadan direkt çalışıyordu. Değiştirilmiş bir client, rakibin sırasında ateş
   edebilir veya aynı mermi çözülmeden art arda ateşleyebilirdi. **Fix:** `AbilityBase.cs`'e
   `protected bool ServerCanAct => gravityBody != null && gravityBody.isActive.Value;` eklendi,
   tüm 8 RPC handler'ın ilk satırına `if (!ServerCanAct) return;` kondu — bu tek kontrol hem sıra
   dışı ateşlemeyi hem de `TurnManager.NotifyProjectileLaunched`'ın ilk ateşten hemen sonra
   `isActive.Value`'yu senkron false yapması sayesinde art arda ateşlemeyi engelliyor.
   - **Takip edildi ve kapatıldı (2026-07-15, 3. tur) — `CharacterAbilities` network-authoritative
     yapıldı.** `MonoBehaviour` → `NetworkBehaviour`; tüm cephane sayaçları (`superJumps`, `rpgAmmo`,
     `pistolAmmo`, `shotgunAmmo`, `grenades`, `shields`) tek bir `AmmoState` (`INetworkSerializable`)
     struct'ında toplanıp `NetworkVariable<AmmoState>` (Server-write) yapıldı; `HasUsedSkillThisTurn`
     de `NetworkVariable<bool>` (Server-write) oldu. Yeni `ServerTryConsume(int slotIndex)` —
     `netHasUsedSkill` true iken HERHANGİ slotu reddeder (turda tek yetenek kuralı, ayrıca
     silaha-özel server-side cooldown zamanlayıcısı gerekmeden "art arda farklı silah" açığını da
     kapatıyor) ve ilgili slotun cephanesini kontrol edip düşürür — her 9 ability'nin (Pistol/
     Shotgun/Rpg/HandGrenade/Teleport/BlackHoleSkill/BatHammerSkill/ShieldSkill/SuperJumpSkill)
     `[ServerRpc]` handler'ından `ServerCanAct`'ten HEMEN SONRA çağrılıyor. `SuperJumpSkill`'in
     kendi `[ServerRpc]`'si yoktu (yalnızca `gravityBody.nextJumpIsSuper` client-side bayrağını
     set ediyordu) — yeni `ConsumeServerRpc`/`ApplySuperJumpClientRpc` çifti eklendi (aynı
     `GravityBody.ApplyForce`'taki owner-hedefli ClientRpc deseni). Public API (getter'lar,
     event'ler, `HasUsedSkillThisTurn`) hiç değişmedi — `WeaponUIManager`/`SkillUIManager`'da
     sıfır değişiklik gerekti. Offline hotseat davranışı bilerek birebir korundu (her `Use*()`
     metodu `!IsSpawned` dalında eskisiyle aynı doğrudan mutasyonu yapıyor) — `CharacterHealth.
     Awake()/OnNetworkSpawn()`'daki (zaten test edilmiş) "offline'da doğrudan, online'da yalnızca
     server" deseni birebir taklit edildi, yeni bir mimari icat edilmedi.
     **Not — Unity Editor bu geçişte kapalıydı, canlı/iki-process doğrulama yapılamadı**; değişiklik
     dikkatli statik inceleme + mevcut, zaten doğrulanmış `CharacterHealth` desenine birebir
     sadakatle yazıldı, ama bir sonraki Unity oturumunda hem offline hem online (iki client) bir
     maçta ateşleme/cephane/tur geçişini test etmek şart.
   - **Kalıcı bir mimari sınır (kod ile çözülemez):** bu proje host'u oyunculardan biri olan P2P
     Relay/NGO kullanıyor ("server" = bir oyuncunun kendi makinesi), yetkili/tarafsız bir dedicated
     server yok. Yukarıdaki fix hileli bir NON-HOST client'a karşı korur; hileli bir HOST kendi
     `isActive`/`teamId` gibi server-write NetworkVariable'larını istediği gibi yazabilir — bunu
     çözmek dedicated/cloud-hosted authoritative server yatırımı gerektirir, bu bir bug-fix
     oturumunun kapsamı dışında.
2. **`BatHammerSkill.SwingServerRpc`** client'tan gelen `aimDir`'i normalize etmeden kullanıyordu
   (birim olmayan bir vektör koni-içi hedef tespitini bozabilirdi). **Fix:** server artık
   `aimDir.normalized` kullanıyor, sıfıra yakın vektörler reddediliyor.
3. **`NetworkBootstrap.cs`'te 13 korumasız `Debug.Log`/`LogWarning`/`LogError`** (join code'lar,
   bağlantı durumu) production build'lerde de cihaz loguna yazılıyordu — projenin geri kalanındaki
   `#if UNITY_EDITOR` kuralına aykırıydı. **Fix:** hepsi `#if UNITY_EDITOR` ile sarıldı.
4. **`CurrencyManager`'ın `currency.json`'ı düz metin, imzasız/kurcalamaya açıktı** (save-editor ile
   Gold/Gem/XP direkt değiştirilebiliyordu). **Fix:** dosya artık `{data, hmac}` zarfında —
   `HMACSHA256(gömülü anahtar + SystemInfo.deviceUniqueIdentifier)` ile imzalanıyor;
   `Load()` HMAC uyuşmazlığında kurcalamayı tespit edip güvenli varsayılana dönüyor ve dosyayı temiz
   +imzalı olarak yeniden yazıyor. Eski (zarfsız) formattaki mevcut oyuncu dosyaları geriye dönük
   uyumlu okunup ilk fırsatta yeni formatta yeniden yazılıyor (ilerleme silinmiyor).
   - **Dürüst sınır (yorumlarda da belirtildi):** anahtar client binary'sinde gömülü olduğu için bu
     save-editor gibi yaygın araçları engeller, ama reverse-engineering yapabilen gelişmiş bir
     saldırganı DURDURMAZ — kriptografik bir garanti değil, bir caydırıcı/tespit katmanı.
5. **IAP satın almaları hiçbir makbuz doğrulaması olmadan Gem veriyordu.** **Fix:**
   `IAPManager.cs`'e Unity IAP'ın `CrossPlatformValidator`'ı ile makbuz doğrulama eklendi —
   `STEAMWORKS_INSTALLED`/`GPGS_INSTALLED` ile aynı "kod hazır, gerçek anahtar bekliyor" deseni:
   `IAP_RECEIPT_VALIDATION` define'ı tanımlı değilken (şu an durum bu) `IsReceiptValid()` her zaman
   `true` döner — Tangle sınıfları (`GooglePlayTangle`/`AppleTangle`) üretilmeden bu define
   açılırsa derleme hatası olurdu, bu yüzden bilinçli olarak kapalı bırakıldı.
   **Kalan manuel adım (kod yok):** Play Console → Uygulamanız → Bütünlük → Lisanslama'daki Base64
   RSA public key'i al → Unity Editor'de Window → Unity IAP → Receipt Validation Obfuscator'a
   yapıştır → Player Settings → Scripting Define Symbols'a `IAP_RECEIPT_VALIDATION` ekle.

### Çözülemedi — backend/altyapı yatırımı gerektiriyor (bilerek yarım bırakılmadı, açıkça işaretlendi)

6. **`CloudSaveManager.PushAsync`** yerel dosyaların (currency/progress/unlocks/quests/chests/
   streak/costumes) ham içeriğini hiçbir sunucu-taraflı doğrulama olmadan doğrudan UGS Cloud
   Save'e yazıyor — HMAC fix'i (madde 4) yalnızca YEREL dosyayı korur, buluta giden veri hâlâ
   client'ın o an bellekte tuttuğu (dolayısıyla bir bellek-hackleme aracıyla değiştirilebilecek)
   değerdir. Gerçek çözüm: economy mutasyonlarını (Add/Spend) UGS Cloud Code fonksiyonlarına
   taşımak (client sadece "bu maçı kazandım" gibi bir isteği sunucuya iletir, gerçek bakiye
   hesaplaması ve Cloud Save yazımı orada olur) — bu bir Cloud Code yazma/deploy işi, bu Unity
   client projesinin kapsamı/araçları dışında.
7. **`LeaderboardManager.ReportOnlineMatchResult`** public bir metod, gerçekten bir maç olduğunu
   doğrulayan bir sunucu/Cloud Code kontrolü olmadan doğrudan `AddPlayerScoreAsync` çağırıyor —
   hileli bir client'ın kupayı keyfi şişirmesini engelleyen hiçbir şey yok.
   - **Neden bu geçişte de yapılmadı (madde 6 ile aynı gerekçe + fazlası):** `ugs` CLI bu makinede
     kurulu değil, `com.unity.services.cloudcode` paketi projeye eklenmemiş, ve gerçek deploy
     (Dashboard'a giriş/CLI login) yalnızca kullanıcının kendi Unity kimliğiyle yapılabilecek
     interaktif bir adım — bu oturumdan fiilen imkansız. ÖNEMLİSİ: bu, madde 1'deki host-güven
     sınırı yüzünden basit bir "client Cloud Code'u çağırsın" fix'i değil — cheating HOST da
     zaten sunucu rolünde olduğu için, gerçek bir düzeltme iki taraf arasında ÇAPRAZ DOĞRULAMA
     (dual-attestation) gerektiriyor: her iki client de maç sonucunu (matchId + winnerId/loserId,
     her ikisinin de gerçek UGS PlayerId'siyle) BAĞIMSIZ olarak aynı Cloud Code fonksiyonuna
     bildirir; fonksiyon iki bildirim UYUŞMUYORSA veya yalnızca biri gelmişse kupa vermez, yalnızca
     ikisi de aynı sonucu bildirirse (tek bir hileli taraf artık tek başına yeterli olmuyor,
     rakibiyle de anlaşması gerekiyor) kupa güncellenir. Bu, şu an client'larda bilinmeyen
     rakibin gerçek PlayerId'sinin (Quick Match'te — arkadaş daveti akışında zaten var) karşılıklı
     değişimini de gerektiriyor; yani bu yalnızca bir Cloud Code scripti değil, yeni bir
     cross-client protokol. Canlı iki-process test edilemeden (bu oturumda Unity Editor kapalıydı)
     bunu doğrudan test edilmiş, çalışan dereceli maç akışına kabloyu bağlamak riskli — "sorunsuz"
     hedefiyle çelişirdi, bu yüzden bilerek yapılmadı.
   - **Sonraki oturum için somut plan:** (1) `NetworkPlayerSpawner`/`TurnManager`'a match başlangıcında
     her iki client'ın kendi `AuthenticationService.Instance.PlayerId`'sini bir `[ServerRpc]` ile
     sunucuya bildirmesi + sunucunun ikisini de her iki client'a bir `[ClientRpc]` ile geri
     yayması (host taraflı bir `Dictionary<ulong,string>` clientId→PlayerId eşlemesi); (2)
     `com.unity.services.cloudcode` paketini ekleyip bir `SubmitMatchResult(matchId, winnerId,
     loserId)` Cloud Code modülü yazmak (JS, Cloud Save Data API ile iki tarafın bildirimini
     `match_<matchId>` anahtarında biriktirip karşılaştıran); (3) `LeaderboardManager`'ı bunu
     çağıracak, ama BAŞARISIZ olursa (henüz deploy edilmemiş/ağ hatası) sessizce mevcut doğrudan
     `AddPlayerScoreAsync` yoluna düşecek şekilde yazmak (geriye dönük kırılmaz); (4) Unity Editor
     açıkken gerçek iki-process bir dereceli maçla uçtan uca doğrulamak (bu projenin tüm
     multiplayer milestone'larında izlenen standart, bkz. yukarıdaki "Multiplayer" bölümü).

Madde 22 (üstteki YAYIN YOL HARİTASI) zaten bunu "gelir başlayınca öncelik" olarak not etmişti; bu
denetim somut mekanizmaları (hangi dosya/satır, tam olarak ne kadar açık) doğruladı, bu turda ek
olarak `CharacterAbilities`'i (madde 1'in takibi) network-authoritative yaptı, kalan iki maddeyi
(6-7) somut bir uygulama planıyla backend-bağımlı olarak netleştirdi.
