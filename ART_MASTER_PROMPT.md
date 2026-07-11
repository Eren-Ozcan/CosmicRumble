# CosmicRumble — Görsel Üretim Master Prompt (v2)

> v1'den farkı: Coplay (paralı) çıkarıldı — tüm promptlar araç bağımsız yazıldı ve ücretsiz
> araçlarla çalışacak şekilde negative prompt + tutarlılık teknikleri eklendi. Base karakter
> tasarımı netleştirildi: **tür-nötr maskot astronot** (insan/hayvan/böcek değil). Kostüm
> üretimi hue-shift ekonomisiyle yeniden planlandı, öncelik sırası değişti (önce base karakter).

---

## 0. Bu Doküman Nasıl Kullanılır

1. Görsel üretim aracına önce **Bölüm 1**'i (oyun context'i) ver — araç "neden böyle" sorusunun
   cevabını bilsin.
2. Her üretimde **Bölüm 2.1** stil bloğunu prompta ekle; araç destekliyorsa **Bölüm 2.2**
   negative bloğunu da ekle.
3. İlk iş **Bölüm 3** (base karakter). Base onaylanmadan başka hiçbir şey üretme — kostümler,
   ikonlar, avatarlar hepsi base'in şekil dilinden türeyecek.
4. Kostümlerde **Bölüm 4.3**'teki üretim stratejisine uy: Common/Uncommon'ların çoğu üretim
   değil, hue-shift/katman işi.

### 0.1 Araç seçimi (ücretsiz seçenekler)

Coplay kullanılmayacak (ücretli). Ücretsiz katmanı olan alternatifler (erişim/fiyat koşulları
zamanla değişebilir, kullanmadan önce doğrula):

- **Bing / Microsoft Copilot Designer (DALL·E)** — tamamen ücretsiz, günlük "boost" limiti var.
  Negative prompt desteklemez; istenmeyenleri pozitif promptta "no ..." olarak yaz.
- **Leonardo.ai** — günlük ücretsiz token; img2img + reference image destekler (tutarlılık için
  en kullanışlısı).
- **Ideogram / Recraft** — ücretsiz katman; Recraft "vector/flat" stilinde bu projeye çok uygun
  çıktı verir.
- **Stable Diffusion (yerel, ör. A1111/ComfyUI)** — tamamen ücretsiz ve sınırsız; ControlNet ile
  aynı silüet üzerine kostüm giydirmek (Bölüm 4.3) en sağlıklı burada çalışır. GPU ister.

### 0.2 Karakter tutarlılığı (150 kostümün aynı karaktere ait görünmesi)

- Base karakter onaylanınca **1 adet referans PNG** sabitlenir (`Assets/Art/Reference/base_mascot.png`).
- Sonraki tüm karakter üretimlerinde bu görsel **img2img / image reference** girdisi olarak verilir
  (Leonardo "Image Guidance", SD "img2img/ControlNet lineart", Copilot'ta mümkün değil — Copilot'u
  yalnız ikonlar/gezegenler gibi silüet bağımsız işlerde kullan).
- Prompta her seferinde şu cümle eklenir:
  `same character as the reference image, identical silhouette, identical helmet and eyes, only the costume/outfit changes`
- Denominasyon: her üretimden 4+ varyant al, en tutarlı olanı seç, gerekirse Krita/GIMP/Photopea
  (ücretsiz) ile küçük rötuş yap.

### 0.3 Post-process boru hattı (her asset için)

1. Arka planı temizle (üretim aracı şeffaf vermezse: Photopea / rembg / birdsiz ücretsiz bg-removal).
2. Kenar temizliği: outline dışında yarı saydam piksel artığı kalmasın (Unity'de beyaz hale yapar).
3. Boyutlandır (Bölüm 0.4) ve `Assets/Art/Sprites/...` altına adlandırma şemasıyla koy
   (kostüm: `costume_{id}.png`, avatar: `avatar_{id}.png`, ikon: `icon_{ad}.png`).
4. Unity import: Sprite (2D and UI) · PPU tüm oyun sprite'larında **100** (UI ikonları serbest) ·
   karakter pivot = **Bottom Center** (radyal hizalama kafayı gezegen merkezinden dışarı bakacak
   şekilde döndürürken ayak yüzeye oturur) · tüm kostüm/ikon/avatarlar tek Sprite Atlas'ta.

### 0.4 Hedef çözünürlükler

| Asset | Üretim | Oyun içi |
|---|---|---|
| Base karakter & kostüm (character) | 1024×1024 | ~512px boy |
| Kostüm (weapon) | 1024×1024 | ~256–384px en |
| Avatar | 512×512 | 256×256 |
| Silah/yetenek HUD ikonu | 512×512 | 128–256 |
| Gezegen | 2048×2048 | 1024+ (ekranın 1/3'ü, yakınlaşma yok) |
| UI/ekonomi ikonları | 512×512 | 128–256 |

---

## 1. Oyun Nasıl Bir Şey (context — üretim aracına önce bunu ver)

**Tek cümlede:** Küçük, yumruk büyüklüğünde yuvarlak gezegenlerin üzerinde oyuncuların sırayla
birbirine ateş ettiği, Worms + Angry Birds Space + Brawl Stars karışımı gibi hissettiren, komik
ve renkli bir mobil arena dövüş oyunu.

**Sahne (kamera/ölçek):** Kamera sabit, yandan; tüm gezegen ve karakterler aynı anda ekranda.
Ortada küçük bir gezegen (ekranın ~1/4–1/3'ü), üzerinde minik ama iri kafalı karakterler; arka
planda derin uzay + yıldızlar + uzak gezegen siluetleri. Karakterler gezegenin eğri yüzeyine
yapışık durur, kafaları hep gezegen merkezinden dışarı bakar — **karşılıklı iki karakter
birbirine baş aşağı görünebilir.** Bu, oyunun imza görseli ve tüm karakter tasarımını belirleyen
kısıt: **siluet her açıda, ters çevrilmiş halde bile okunmalı.**

**Ton:** Komik, oyuncak gibi, çocuksu-enerjik. Asla gerçekçi/kanlı/karanlık değil. Patlamalar
büyük ve gösterişli ama çizgi film usulü ("hasar aldım ama sersemledim" — kan yok, is karası ve
şaşkın gözler var). Palet: doygun şekerleme tonları — neon mor/camgöbeği uzay + sıcak
turuncu/sarı patlama. Asla gri-kahve realistik askeri palet değil.

**Referans oyunlar:** Brawl Stars (karakter oranları + UI — projenin UI'sı gerçek BS ekran
görüntüleriyle birebir kıyaslanarak onaylandı: koyu antrasit paneller, Titan One font, kalın
outline'lı beyaz başlıklar, sarı vurgular), Worms (silah çeşitliliği + yıkılabilir zemin +
mizah), Angry Birds Space (küçük yuvarlak gezegen yerçekimi hissi), Clash Royale / King of
Thieves (mobil f2p meta-ekonomi hissi).

**Oynanış:** Sıra tabanlı; mermi custom yerçekimiyle gezegenin etrafında eğri yörünge çizer
(gezegenin arkasına dolanabilir), isabet eden yüzey parça parça yok olur (destructible küre).
Silahlar: Pistol, Shotgun, RPG, Grenade, Bomb. Yetenekler: BlackHole, Teleport, Shield,
BatHammer, SuperJump. Meta: seviye/prestij, Gold/Gem, 150 kozmetik kostüm, 50 başarım,
görevler, sandıklar, leaderboard, arkadaşlar, 7 dil. Platform: Android → iOS.

**Mood-board anahtar kelimeleri:**

```
tiny round cartoon planet, whimsical space arena battle, toylike chibi space creature warriors,
candy-colored sci-fi palette, comedic oversized explosions, playful not gory,
Worms-meets-Brawl-Stars, zero-gravity planetoid combat, punchy saturated colors,
family-friendly cartoon violence
```

---

## 2. Evrensel Stil Blokları

### 2.1 Pozitif stil bloğu — HER prompta ekle

```
chunky cartoon mobile game art style, thick bold dark outlines, flat saturated vibrant colors,
simplified stylized proportions (Brawl Stars / Clash Royale aesthetic), clean vector-like shapes,
soft two-tone cel shading with punchy rim light, rounded friendly shape language (circles and
capsules, no sharp realistic detail), centered composition, transparent background,
high-quality 2D game asset, no text, no watermark
```

### 2.2 Negative prompt bloğu — araç destekliyorsa ekle

```
photorealistic, realistic human anatomy, painterly soft rendering, gritty, grimdark, blood, gore,
military camouflage realism, muddy desaturated colors, thin delicate limbs, tiny intricate details,
3D render, depth of field, background scenery, drop shadow on ground, text, letters, logo,
watermark, signature, frame, border
```

(Negative prompt almayan araçlarda — ör. Copilot Designer — kritik olanları pozitif prompta
"no photorealism, no text, no background" olarak ekle.)

### 2.3 Şekil dili kuralı (tüm assetler için tasarım pusulası)

Oyunun her görseli aynı geometriden türer: **daire**. Gezegen yuvarlak → kask yuvarlak → gövde
kapsül → patlama yuvarlak → avatar çerçevesi yuvarlak. Sivri/gerçekçi detay yalnız rarity
yükseldikçe ve ölçülü girer (Epic/Legendary siluet parçaları). Bir asset dairelerle
kurulamıyorsa stile aykırıdır, yeniden tasarla.

---

## 3. Base Karakter — "Maskot Astronot" (ÖNCE BU; her şey bunun üzerine)

### 3.1 Tasarım kararı ve gerekçesi

Karakter **insan değil, hayvan değil, böcek değil** — türü kasten belirsiz, yuvarlak cam kasklı,
tombul bir **maskot astronot yaratık**. Kaskın içinde yalnız iki kocaman göz görünür (burun,
ağız, saç, ten yok).

Gerekçe (üretim aracına da verilebilir, "neden" sorusunun cevabı):

1. **360° okunabilirlik:** Karakter gezegenin altında baş aşağı da durur. Yuvarlak kask + kapsül
   gövde her açıda aynı okunur; ince uzuvlu insan/böcek ters döndüğünde silüeti dağılır.
2. **150 kostüm tek silüete giyecek:** Tür-nötr bir yaratıkta kostüm, karakterin *kendisi* olur
   (Phoenix kostümü giyen "phoenix'tir"); belirli bir insan/hayvanda ise "kıyafet giymiş adam"
   gibi durur. Ayrıca yüz (göz katmanı) hiç değişmediği için renk-varyasyonu kostümler bedavaya
   çıkar (Bölüm 4.3).
3. **Küçük ekran ölçeği:** Kişiliği yalnız gözler ve kafa oranı taşıyabilir; detay taşımaz.
4. **IP sahipliği:** Mevcut placeholder (`player_15.png`) Luigi'ye benziyor — yayınlanamaz.
   Maskot tamamen orijinal olmalı; app icon'dan mağaza görseline kadar oyunun yüzü bu.

### 3.2 Anatomi spesifikasyonu

- **Kask/kafa:** toplam boyun ~%55'i; şeffaf yuvarlak cam kubbe; camda tek parlak yansıma çizgisi.
- **Gözler:** kask içinde iki iri oval göz (kaskın ~%40'ı); duygu motoru bunlar (Bölüm 3.4).
- **Gövde:** kısa tombul kapsül; dar uzay tulumu; göğüste küçük yuvarlak panel/rozet
  (kostümlerde tema amblemi buraya gelir).
- **Kollar:** kısa güdük; iri yuvarlak eldivenler; tek el silah tutar.
- **Botlar:** abartılı iri "ay botu" — hem sevimlilik hem "eğri yüzeye yapışma" fantezisi.
- **Silah ölçeği:** gövdeyle aynı boyutta, gerçekçi değil — seçili silah bir bakışta anlaşılmalı.
- **Renk (default/Gray Soldier):** açık gri-beyaz tulum, koyu antrasit detaylar, tek sıcak vurgu
  (turuncu) — kostümlerin renkleri üstünde patlasın diye base nötr durur.

### 3.3 Ana prompt (idle poz)

```
a small chubby species-neutral mascot astronaut creature, 2D side-view mobile game character
sprite, oversized round transparent glass dome helmet taking up more than half of its body,
two big expressive oval eyes floating inside the helmet (no nose, no mouth, no hair, species
deliberately ambiguous), short plump capsule-shaped body in a snug light-gray space suit with
dark charcoal accents and one small round chest badge, stubby arms with oversized round gloves,
very large chunky moon boots, standing in a confident idle combat stance holding a compact
sci-fi pistol aimed sideways at arm's length, single warm orange accent color,
[+ 2.1 stil bloğu] [+ 2.2 negative bloğu]
```

### 3.4 Poz ve ifade seti (base için üretilecek varyantlar)

Aynı referans görselle (Bölüm 0.2) şu varyantlar üretilir — animasyon Unity tarafında
squash&stretch/döndürme ile yapılacağı için kare-kare sprite sheet GEREKMEZ, poz başına tek görsel yeter:

| Varyant | Prompt eki | Kullanım |
|---|---|---|
| idle | (3.3'teki hali) | sahne, gardırop önizleme |
| aim | `aiming carefully, one eye squinted, arm extended` | nişan alma turu |
| hurt | `dizzy knocked-back pose, swirly dazed eyes, small soot smudges, comedic, not gory` | hasar |
| victory | `cheering with both arms up, star-shaped sparkling happy eyes` | maç sonu kazanan |
| defeat | `slumped sitting pose, big teary sad eyes, cracked helmet glass (small comedic crack)` | maç sonu kaybeden |
| panic | `being pulled sideways, panicked wide eyes, gripping the ground` | BlackHole çekimi |

**Göz ifadeleri ayrı katman olarak da kırpılabilir** (Bölüm 3.5) — o zaman poz sayısı da düşer.

### 3.5 Katmanlı üretim (kostüm ekonomisinin anahtarı)

Base onaylandıktan sonra Photopea/Krita'da tek sprite şu katmanlara ayrılır ve Unity'de ayrı
SpriteRenderer'larla üst üste bindirilir:

1. **body_base** — tulum+botlar+eldivenler (kostümün boyayacağı/değiştireceği katman)
2. **helmet_glass + eyes** — hiçbir kostümde değişmez (karakter kimliği)
3. **costume_overlay** — Rare+ kostümlerde eklenen zırh/kanat/pelerin parçaları
4. **weapon** — eldeki silah, tamamen bağımsız (silah kostümleri yalnız bunu değiştirir)

Kazanç: Common/Uncommon karakter kostümleri = body_base'e hue-shift/tint (üretim YOK);
göz/mimik tüm kostümlerde otomatik aynı; silah skinleri karakterden bağımsız üretilir.

### 3.6 Base onay checklist'i (geçmeden Bölüm 4'e başlama)

- [ ] 128px'e küçültüldüğünde silüet ve gözler hâlâ okunuyor
- [ ] 180° döndürüldüğünde (baş aşağı) karakter olduğu anlaşılıyor
- [ ] Gerçek gezegen sprite'ının üstüne konup ekran görüntüsünde denendi
- [ ] Hue-shift testinde (3-4 farklı renge boyayınca) çirkinleşmiyor
- [ ] Hiçbir mevcut IP'ye (Mario/Luigi, Among Us, Fall Guys vb.) "benziyor" denmiyor —
      özellikle Among Us kontrolü: gözler + kask camı içeriden görünür olmalı, tek vizör DEĞİL

---

## 4. Kostümler — 150 adet

### 4.1 Rarity görsel dili (UI'da zaten kodlanmış renkler)

| Rarity | Hex | Materyal/detay dili | Prompt cümlesi |
|---|---|---|---|
| Common | `#9EA6B2` gri | düz renk, mat, süsleme yok | `plain flat recolor, matte fabric, no ornaments` |
| Uncommon | `#4DD966` yeşil | basit desen, hafif parlaklık | `simple pattern or texture detail, slight sheen` |
| Rare | `#4088FF` mavi | belirgin siluet detayı, mavi rim light | `distinct silhouette accessory, soft blue rim light` |
| Epic | `#A659FF` mor | dramatik siluet, enerji efektleri | `dramatic silhouette pieces, glowing purple energy particles` |
| Legendary | `#FFCC33` altın | altın/prizmatik kaplama, aura | `ornate golden prismatic plating, dynamic radiant energy aura` |

### 4.2 Tip ve rarity dağılımı

| Rarity | Character | Weapon | Toplam |
|---|---|---|---|
| Common | 24 | 16 | 40 |
| Uncommon | 19 | 16 | 35 |
| Rare | 18 | 17 | 35 |
| Epic | 16 | 9 | 25 |
| Legendary | 9 | 6 | 15 |
| **TOPLAM** | **86** | **64** | **150** |

### 4.3 Üretim stratejisi — 150 üretim DEĞİL, ~70 üretim + hue-shift

| Katman | Kapsam | Yöntem | Gerçek üretim |
|---|---|---|---|
| Common character (24) | hepsi renk varyasyonu | body_base hue-shift/tint (Unity material veya Photopea) | **0** |
| Common weapon (16) | renk varyasyonu | önce 5 base silah sprite'ı üret, sonra tint | **5** (base silahlar) |
| Uncommon (35) | renk + basit desen | hue-shift + 8-10 adet desen overlay'i üret (kamuflaj, buz çatlağı, devre, yaprak...) ve karıştır | **~10** (overlay) |
| Rare (35) | özgün detay | tek tek üret (img2img referansla) | 35 |
| Epic (25) | dramatik siluet | tek tek üret | 25 |
| Legendary (15) | tam özgün | tek tek üret, gerekirse 2-3 deneme | 15 |

Toplam gerçek üretim ≈ **90 görsel** (150 yerine) ve Common/Uncommon anında, ücretsiz, %100
tutarlı çıkar. Hue-shift renk hedefleri kostüm adından bellidir (ör. c005 Yellow Storm → sarı).

### 4.4 Tema descriptor sözlüğü ({THEME} yerine geçecek İngilizce blok)

| Tema | Descriptor |
|---|---|
| Space | `cosmic starfield pattern, swirling nebula colors, glowing constellation accents` |
| Fantasy | `medieval fantasy armor, dragon scale texture, glowing runes, ornate engravings` |
| Cyber | `neon circuit lines, holographic panels, glowing tech visor, cyberpunk color glow` |
| Nature | `leafy vines, moss and bark textures, organic shapes, mushroom/flower accents` |
| Dark | `shadowy black-purple wisps, ominous glow, smoky dark aura` |
| Fire | `living flames, glowing ember cracks, molten lava veins, heat glow` |
| Ice | `crystalline ice shards, frost patterns, frozen mist, cold blue glow` |
| Mech | `riveted metal armor plates, hydraulic joints, exhaust vents, robotic parts` |
| Myth | `ancient god motifs, laurel and gold ornaments, marble and divine radiant glow` |
| Other | `clean bold single-color design` |

### 4.5 Prompt şablonları

**Character kostümü (Rare+):**

```
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "{NAME}" costume, theme: {TEMA DESCRIPTOR},
{RARITY PROMPT CÜMLESİ}, accent color {RARITY_HEX}, full-body side-view 2D game sprite,
[+ 2.1 stil bloğu] [+ 2.2 negative bloğu]
```

**Weapon kostümü:**

```
sci-fi cartoon {pistol|shotgun|rocket launcher|grenade|time bomb} weapon skin, "{NAME}",
theme: {TEMA DESCRIPTOR}, {RARITY PROMPT CÜMLESİ}, accent color {RARITY_HEX}, side-view 2D game
asset, chunky oversized toylike proportions, bold silhouette, [+ 2.1] [+ 2.2]
```

**Doldurulmuş örnekler:**

```
(e002 Dragon Lord — Epic Character)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Dragon Lord" costume, theme: medieval
fantasy armor, dragon scale texture, glowing runes, ornate engravings, dramatic silhouette
pieces with small dragon-wing back ornaments and horned helmet rim, glowing purple energy
particles, accent color #A659FF, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(l013 Black Hole Cannon X — Legendary Weapon)
sci-fi cartoon rocket launcher weapon skin, "Black Hole Cannon X", theme: cosmic starfield
pattern, swirling nebula colors, glowing constellation accents, ornate golden prismatic plating,
dynamic radiant energy aura, a tiny swirling black hole visible inside the barrel, accent color
#FFCC33, side-view 2D game asset, chunky oversized toylike proportions, [+ 2.1] [+ 2.2]
```

### 4.6 Tam liste (id · İsim · Tip · Tema)

**COMMON (40)** — karakterler hue-shift, silahlar 5 base + tint (Bölüm 4.3):
c001 Gray Soldier · C · Other (başlangıç) — c002 Standard Blue · C · Other (başlangıç) — c003 Red
Warrior · C · Other — c004 Green Camo · C · Nature — c005 Yellow Storm · C · Other — c006 Orange
Ember · C · Fire — c007 Purple Night · C · Dark — c008 White Snow · C · Ice — c009 Brown Earth ·
C · Nature — c010 Sky Blue · C · Space — c011 Steel Gray · W · Mech — c012 Rust Brown · W · Mech —
c013 Forest Green · W · Nature — c014 Lava Red · W · Fire — c015 Ice Blue · W · Ice — c016 Night
Black · W · Dark — c017 Sun Yellow · W · Other — c018 Coral Pink · C · Other — c019 Sea Teal · C ·
Other — c020 Lavender · C · Other — c021 Bright Copper · W · Mech — c022 Desert Sand · C · Nature —
c023 Pistachio Green · C · Nature — c024 Sea Foam · W · Ice — c025 Fog Gray · C · Dark — c026
Sunset · C · Fire — c027 Stardust · W · Space — c028 Ocean Depths · W · Other — c029 Chalk White ·
C · Other — c030 Anthracite · C · Dark — c031 Mint Green · W · Nature — c032 Candy Pink · C ·
Other — c033 Thunder · W · Other — c034 Golden Yellow · W · Other — c035 Emerald · C · Nature —
c036 Hedgehog Brown · C · Nature — c037 Titan Gray · W · Mech — c038 Maroon · C · Dark — c039
Cobalt · W · Space — c040 Indigo Blue · C · Other

**UNCOMMON (35)** — hue-shift + desen overlay:
u001 Forest Warrior · C · Nature — u002 Ice Mage · C · Ice — u003 Flame Dancer · C · Fire — u004
Night Watcher · C · Dark — u005 Lightning Runner · C · Other — u006 Sandstorm · C · Nature — u007
Deep Space · C · Space — u008 Iron Fist · C · Mech — u009 Wind Spirit · C · Nature — u010 Cosmic
Purple · C · Space — u011 Dragon Fang · W · Fantasy — u012 Space Rifle · W · Space — u013 Ice
Sword · W · Ice — u014 Flame Spear · W · Fire — u015 Shadow Blade · W · Dark — u016 Fog Pistol ·
W · Dark — u017 Plasma Tube · W · Cyber — u018 Nature Shield · W · Nature — u019 Lightning Orb ·
W · Other — u020 Iron Shield · W · Mech — u021 Crystal Warrior · C · Ice — u022 Volcano Man · C ·
Fire — u023 Cyber Ninja · C · Cyber — u024 Stone Golem · C · Nature — u025 Neon Jacket · C ·
Cyber — u026 Foam Sailor · C · Other — u027 Steppe Soldier · C · Nature — u028 Silver Knight · C ·
Fantasy — u029 Blue Crocodile · W · Nature — u030 Ember Blade · W · Fire — u031 Hologram Weapon ·
W · Cyber — u032 Steel Dragon · W · Mech — u033 Crystal Bomb · W · Ice — u034 Root Texture · W ·
Nature — u035 Storm Sail · C · Other

**RARE (35)** — tek tek üretim:
r001 Galaxy Wanderer · C · Space — r002 Black Knight · C · Dark — r003 Neon Samurai · C · Cyber —
r004 Dragon Hunter · C · Fantasy — r005 Ice God · C · Ice — r006 Lava Giant · C · Fire — r007
Quantum Armor · C · Cyber — r008 Forest God · C · Nature — r009 Dark Sorcerer · C · Dark — r010
Meteor Warrior · C · Space — r011 Plasma Rifle · W · Cyber — r012 Dragon Flame · W · Fantasy —
r013 Black Hole Cannon · W · Space — r014 Ice Shield · W · Ice — r015 Ember Bomb · W · Fire —
r016 Nano Blade · W · Cyber — r017 Rune Spear · W · Fantasy — r018 Shadow Arrow · W · Dark — r019
Emerald Dragon · W · Fantasy — r020 Star Sword · W · Space — r021 Titanium Golem · C · Mech —
r022 Light Speed · C · Space — r023 Sea Monster · C · Nature — r024 Storm God · C · Myth — r025
Crimson Shaman · C · Myth — r026 Cyber Samurai · C · Cyber — r027 Bionic Warrior · C · Mech —
r028 Vortex Rifle · W · Space — r029 Shaman Staff · W · Myth — r030 Titan Hammer · W · Mech —
r031 Wind Blade · W · Nature — r032 Crystal Staff · W · Fantasy — r033 Laser Rifle · W · Cyber —
r034 Dark Rune · W · Dark — r035 Mythic Archer · C · Myth

**EPIC (25)** — tek tek üretim:
e001 Nebula Warrior · C · Space — e002 Dragon Lord · C · Fantasy — e003 Cyber God · C · Cyber —
e004 Death Spirit · C · Dark — e005 Volcano God · C · Fire — e006 Ice Storm · C · Ice — e007
Forest Deity · C · Nature — e008 Titan Armor · C · Mech — e009 Olympian God · C · Myth — e010
Quantum Shadow · C · Cyber — e011 Galactic Emperor · C · Space — e012 Ancient Dragon · C ·
Fantasy — e013 Neon Demon · C · Dark — e014 Plasma God · W · Cyber — e015 Dragon Breath · W ·
Fantasy — e016 Dark Star · W · Dark — e017 Volcano Cannon · W · Fire — e018 Ice Crystal · W ·
Ice — e019 Nano Swarm · W · Mech — e020 Rune Burst · W · Fantasy — e021 Nebula Bomb · W · Space —
e022 Titan Laser · W · Mech — e023 Mythic Armor · C · Myth — e024 Crystal Golem · C · Ice — e025
Crow King · C · Dark

**LEGENDARY (15)** — tek tek üretim, en yüksek özen:
l001 Cosmic Master · C · Space — l002 Dragon Emperor · C · Fantasy — l003 Dark God · C · Dark —
l004 Doom Lord · C · Dark — l005 Time Master · C · Myth — l006 Universe Warrior · C · Space —
l007 Ancient Giant · C · Myth — l008 Bionic God · C · Mech — l009 Phoenix Warrior · C · Fire —
l010 Cosmic Destroyer · W · Space — l011 God Sword · W · Myth — l012 Dragon Heart · W · Fantasy —
l013 Black Hole Cannon X · W · Space — l014 Doom Hammer · W · Dark — l015 Creator's Power · W · Myth

---

## 5. Profil Avatarları — 16 adet

Prompt şablonu:

```
circular game profile icon, {konsept}, dominant color {hex}, cosmic space phenomenon,
simple bold iconic shape readable at very small size, flat design with subtle glow,
[+ 2.1 stil bloğu] [+ 2.2 negative bloğu]
```

| id | İsim | Hex | Konsept (İngilizce, prompta girecek) |
|---|---|---|---|
| av01 | Nova | `#F24D59` | `exploding star burst, bright red radiant flash` |
| av02 | Comet | `#40B2F2` | `blue comet with a glowing curved tail` |
| av03 | Blaze | `#FF9926` | `orange fireball with dancing flames` |
| av04 | Nebula | `#A659FF` | `purple swirling gas cloud with sparkling stars` |
| av05 | Pulsar | `#33D98C` | `green pulsing light-wave rings` |
| av06 | Quasar | `#FFCC33` | `golden energy beam shooting from a bright core` |
| av07 | Meteor | `#D9408C` | `pink falling meteor with a fiery trail` |
| av08 | Orbit | `#4D8CF2` | `blue orbital ring around a small planet` |
| av09 | Solstice | `#F2732E` | `orange stylized sun symbol with rays` |
| av10 | Eclipse | `#666B7A` | `dark moon eclipse with a glowing rim ring` |
| av11 | Vortex | `#33BFCC` | `cyan spiral whirlpool` |
| av12 | Cosmos | `#8C33D9` | `purple spiral galaxy` |
| av13 | Photon | `#FFE64D` | `bright yellow light particle with sparkles` |
| av14 | Asteroid | `#99A0AD` | `gray rocky cratered asteroid` |
| av15 | Aurora | `#4DE6B2` | `turquoise northern-lights wave` |
| av16 | Zenith | `#E64DCC` | `pink-purple star peak symbol` |

Not: avatarlar karakter içermediği için referans görsel gerekmez — Copilot Designer gibi
referanssız ücretsiz araçlarla üretilebilir.

---

## 6. Silah & Yetenek HUD İkonları — 10 adet

Prompt şablonu:

```
square game HUD ability icon, {konsept}, sci-fi space combat gear, bold silhouette readable
at 64px, subtle dark vignette inside icon frame, [+ 2.1] [+ 2.2]
```

| id | Konsept (İngilizce) |
|---|---|
| weapon_pistol | `compact fast sci-fi pistol, side view` |
| weapon_shotgun | `heavy wide-barrel sci-fi shotgun, side view` |
| weapon_rpg | `shoulder-fired rocket launcher with visible rocket tip` |
| weapon_grenade | `round cartoon grenade with pin and lever` |
| weapon_bomb | `placed time bomb with mechanical timer and blinking light` |
| skill_blackhole | `dark purple-black swirling vortex pulling in light streaks` |
| skill_teleport | `cyan-blue warp energy particles forming a portal swirl` |
| skill_shield | `glowing energy bubble with hexagon panels` |
| skill_bathammer | `heavy energy-charged baseball bat / hammer hybrid` |
| skill_superjump | `energized boot sole with charge glow and speed lines pointing up` |

Mevcut ikonlar (`Assets/Art/Sprites/UI/*_icon.png`) rastgele placeholder — hepsi bu setle
değişecek. `fly_icon.png` SuperJump'a karşılık geliyor, yeni adlandırma şemasına geçir.

---

## 7. Gezegen / Harita Görselleri — 4 tema

Yıkılabilir gezegen iki parça ister: **yüzey sprite'ı** + kraterlerde ortaya çıkacak **iç doku**
(daha koyu kayaç kesiti). Prompt her tema için ikisini de üretmeli veya iç doku tek ortak
görsel olabilir.

Prompt şablonu:

```
small round destructible cartoon planet, perfect circular silhouette, side-view 2D game asset,
{tema}, chunky surface details on the rim (craters/rocks/vegetation reading in silhouette),
slightly darker core color hinting at the inner cross-section, deep space starfield behind,
[+ 2.1] [+ 2.2]
```

| Tema | {tema} bloğu |
|---|---|
| Kayalık/Nötr (mevcut) | `gray-brown rocky asteroid surface with big cartoon craters` |
| Buz | `white-cyan glacier surface, crystal ice spikes on the rim, frosty glow` |
| Lav | `dark crimson cracked surface with glowing orange lava veins and small eruptions` |
| Orman | `lush green mossy surface with giant mushrooms and tiny cartoon trees on the rim` |

Ek: arka plan yıldız alanı ayrı, kaydırılabilir (parallax) geniş görsel olarak üretilebilir:

```
deep space starfield background, distant silhouetted planets, purple-cyan nebula haze,
subtle vignette, wide seamless game background, [+ 2.1 ama transparent background YERİNE
"full-bleed background"] [+ 2.2]
```

---

## 8. UI / Ekonomi İkonları

Hepsi 2.1 stil bloğu + `square game UI icon, bold silhouette readable at 64px` ekiyle:

| Asset | Konsept |
|---|---|
| XP ikonu | `yellow-white star chevron badge` |
| Gold ikonu | `shiny gold coin with star emboss` |
| Gem ikonu | `purple-blue faceted crystal gem` |
| Sandık Common | `simple wooden chest with bronze bands` |
| Sandık Rare | `silver-blue metal chest with glowing seams` |
| Sandık Epic | `ornate gold-purple chest with sparkle particles` |
| Başarım rozeti ×4 | `circular achievement badge frame` + Bölüm 4.1 rarity renkleri |
| Trophy | `golden trophy cup with tiny planet on top` |
| **Uygulama ikonu** | `tiny round planet with the mascot astronaut standing on top waving,
  bold readable at 48px, app icon composition` — maskot onaylanınca ve referansla üretilir |

Mağaza görselleri (Play Console feature graphic, ekran görüntüleri çerçevesi) ayrı pazarlama
işi — base + 2-3 kostüm + 1 gezegen hazır olmadan başlanmaz.

---

## 9. Ses

TODO.md'de tamamlandı olarak kayıtlı (20 SFX + menü müziği) — bu doküman kapsamı dışı,
referans: v1 Bölüm 9.

---

## 10. Öncelik Sırası (v2 — değişti)

1. **Base maskot karakter** (Bölüm 3) — onay checklist'i geçmeden hiçbir şeye başlama;
   Luigi-benzeri placeholder yayın engeli.
2. **5 base silah sprite'ı** (Bölüm 4.3) — hem eldeki silah görseli hem 16 Common weapon
   kostümünün tint kaynağı.
3. **Silah/yetenek HUD ikonları** (Bölüm 6) — her maçta ekranda, mevcutlar rastgele.
4. **16 avatar** (Bölüm 5) — kod tarafı hazır, yalnız görsel bekliyor; referanssız üretilebilir,
   bağımsız/paralel iş.
5. **Kostümler** (Bölüm 4) — sıra: Common (hue-shift, ~1 gün) → Uncommon (overlay) →
   Legendary (15, vitrin değeri en yüksek) → Epic → Rare.
6. **Gezegen çeşitliliği** (Bölüm 7) — 2-3 yeni tema.
7. **Uygulama ikonu + mağaza görselleri** (Bölüm 8) — Play Console kaydı için zorunlu, ama
   maskot kesinleşmeden yapılamaz.
