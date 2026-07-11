> **TASLAK — yayınlanmadan önce bir hukukçuya (KVKK + GDPR için) gösterilmeli.**
> Bu metin, projenin kod tabanında gerçekten toplanan veriler taranarak hazırlandı (bkz. altında
> her madde), ama hukuki bir inceleme yerine geçmez. `{{...}}` ile işaretli alanlar doldurulmadan
> yayınlanmamalı. Yayınlandıktan sonra gerçek, kalıcı bir URL'de barındırılıp
> `Assets/Scripts/Utilities/LegalLinks.cs` içindeki `PrivacyPolicyUrl` sabitine yazılmalı.

# Gizlilik Politikası — CosmicRumble

Son güncelleme: {{TARİH}}

Bu Gizlilik Politikası, **{{ŞİRKET/GELİŞTİRİCİ ADI}}** ("biz") tarafından geliştirilen **CosmicRumble**
mobil oyununu ("Oyun") oynarken hangi verilerin toplandığını, nasıl kullanıldığını ve
haklarınızın neler olduğunu açıklar.

## 1. Hangi Verileri Topluyoruz

Oyun, Unity Gaming Services (UGS) altyapısını kullanır. Aşağıdaki veri kategorileri **yalnızca
kodda gerçekten aktif olan** sistemlere karşılık gelir:

| Kategori | Ne toplanır | Hangi sistem |
|---|---|---|
| Hesap kimliği | Anonim misafir ID'si, veya kullanıcı adı + e-posta (kayıtlı hesap), veya bağlanmışsa Google Play Games hesap ID'si | UGS Authentication |
| Oyun ilerlemesi | Seviye, XP, altın/gem bakiyesi, sahip olunan kostüm/avatar, görev/başarım durumu, ayarlar | UGS Cloud Save |
| Sıralama verisi | Kullanıcı adı, kupa/trophy puanı | UGS Leaderboards |
| Sosyal veri | Arkadaş listesi, çevrimiçi/maçta durumu (presence), davet bilgisi | UGS Friends |
| Kullanım/analitik | Oturum süresi, cihaz/platform bilgisi, maç tamamlama olayları (kazanma/kaybetme, dereceli olup olmadığı) | Unity Analytics |
| Çökme raporları | Uygulama çöktüğünde otomatik oluşan teknik hata/cihaz bilgisi | Unity Cloud Diagnostics (Crash Report API) |
| Satın alma | Uygulama içi satın alma (gem paketleri) işlem bilgisi — ödeme kartı bilgisi bizde **saklanmaz**, işlem Google Play / App Store üzerinden yürütülür | Unity IAP |
| Bildirimler | Yok — hatırlatma bildirimleri (seri/sandık) tamamen cihazda yerel olarak zamanlanır, sunucuya veri gönderilmez | Yerel (local) bildirim |

## 2. Verileri Nasıl Kullanıyoruz

- Oyun ilerlemenizi cihazlar arasında senkronize etmek (Cloud Save)
- Sıralama/kupa sistemini çalıştırmak (Leaderboards)
- Arkadaş ekleme, davet, çevrimiçi durum göstermek (Friends)
- Oyunu iyileştirmek için toplu/anonim kullanım istatistikleri (Analytics)
- Teknik hataları tespit edip düzeltmek (Crash Reporting)
- Satın alınan sanal içeriği (gem) hesabınıza tanımlamak (IAP)

Verileriniz reklam amacıyla üçüncü taraflarla **satılmaz**.

## 3. Veri İşleyiciler (Üçüncü Taraflar)

- **Unity Technologies** (Unity Gaming Services: Authentication, Cloud Save, Leaderboards,
  Friends, Analytics, Cloud Diagnostics) — veriler Unity'nin altyapısında işlenir.
- **Google Play** / **Apple App Store** — satın alma işlemleri ve (varsa) Google Play Games /
  Game Center girişi için.

## 4. Veri Saklama ve Silme

Hesap verileriniz, hesabınız aktif olduğu sürece saklanır. Hesabınızın ve ilişkili verilerin
silinmesini talep etmek için: **{{İLETİŞİM E-POSTASI}}**

## 5. Çocukların Gizliliği

{{YAŞ DERECELENDİRMESİ BELİRLENİNCE BU BÖLÜM DOLDURULACAK — ör. "Oyun 13 yaş altı kullanıcılardan
bilerek kişisel veri toplamaz" ya da ilgili mağaza derecelendirmesine göre COPPA/KVKK uyumlu metin.}}

## 6. Haklarınız (KVKK / GDPR)

Türkiye'de KVKK (6698 sayılı Kanun) ve AB'de GDPR kapsamında; verilerinize erişim, düzeltme, silme
ve işlemeye itiraz hakkına sahipsiniz. Talepleriniz için: **{{İLETİŞİM E-POSTASI}}**

## 7. İletişim

Sorularınız için: **{{İLETİŞİM E-POSTASI}}**

---

# Privacy Policy — CosmicRumble (English)

Last updated: {{DATE}}

This Privacy Policy explains what data **CosmicRumble** ("the Game"), developed by
**{{DEVELOPER/COMPANY NAME}}**, collects, how it's used, and your rights.

## 1. Data We Collect

The Game uses Unity Gaming Services (UGS). The table below reflects only systems that are
actually active in the codebase:

| Category | What's collected | System |
|---|---|---|
| Account identity | Anonymous guest ID, or username + email (registered account), or linked Google Play Games account ID | UGS Authentication |
| Game progress | Level, XP, gold/gem balance, owned costumes/avatars, quest/achievement state, settings | UGS Cloud Save |
| Ranking data | Username, trophy score | UGS Leaderboards |
| Social data | Friends list, online/in-match presence, invite info | UGS Friends |
| Usage/analytics | Session length, device/platform info, match-completion events (win/loss, ranked or not) | Unity Analytics |
| Crash reports | Automatic technical error/device info on app crash | Unity Cloud Diagnostics (Crash Report API) |
| Purchases | In-app purchase (gem pack) transaction info — payment card data is **never stored by us**, processed via Google Play / App Store | Unity IAP |
| Notifications | None — streak/chest reminders are scheduled entirely on-device, no data sent to a server | Local notifications |

## 2. How We Use Data

- Sync your game progress across devices (Cloud Save)
- Run the ranking/trophy system (Leaderboards)
- Enable friend requests, invites, online presence (Friends)
- Aggregate, anonymized usage statistics to improve the Game (Analytics)
- Detect and fix technical errors (Crash Reporting)
- Credit purchased virtual currency to your account (IAP)

We do **not** sell your data to third parties for advertising.

## 3. Data Processors (Third Parties)

- **Unity Technologies** (Unity Gaming Services: Authentication, Cloud Save, Leaderboards,
  Friends, Analytics, Cloud Diagnostics)
- **Google Play** / **Apple App Store** — for purchases and (if used) Google Play Games / Game
  Center sign-in

## 4. Data Retention and Deletion

Account data is retained while your account is active. To request deletion of your account and
associated data, contact: **{{CONTACT EMAIL}}**

## 5. Children's Privacy

{{TO BE FILLED IN ONCE AGE RATING IS SET — e.g. "The Game does not knowingly collect personal
data from users under 13" or wording matching the actual store age rating / COPPA compliance.}}

## 6. Your Rights (GDPR / KVKK)

Under GDPR (EU) and KVKK (Turkey, Law No. 6698), you have the right to access, correct, delete,
and object to processing of your data. Contact: **{{CONTACT EMAIL}}**

## 7. Contact

Questions: **{{CONTACT EMAIL}}**
