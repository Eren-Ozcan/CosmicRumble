# CosmicRumble — Art Generation Master Prompt (v2)

> Difference from v1: Coplay (paid) was dropped — all prompts are written tool-agnostically and
> negative-prompt + consistency techniques were added so they work with free tools. The base
> character design was nailed down: **species-neutral mascot astronaut** (not human/animal/insect).
> Costume production was re-planned around hue-shift economics, and the priority order changed
> (base character first).

---

## 0. How to Use This Document

1. Give the art tool **Section 1** first (the game context) — the tool should know the answer to
   "why is it like this".
2. Add the **Section 2.1** style block to every prompt; if the tool supports it, add the
   **Section 2.2** negative block too.
3. First task is **Section 3** (base character). Produce nothing else until the base is approved —
   costumes, icons and avatars will all derive from the base's shape language.
4. For costumes, follow the production strategy in **Section 4.3**: most Common/Uncommon items are
   not generation work, they are hue-shift/layer work.

### 0.1 Tool selection (free options)

Coplay will not be used (paid). Alternatives with a free tier (access/pricing terms may change over
time, verify before using):

- **Bing / Microsoft Copilot Designer (DALL·E)** — completely free, has a daily "boost" limit.
  Doesn't support negative prompts; write unwanted elements into the positive prompt as "no ...".
- **Leonardo.ai** — daily free tokens; supports img2img + reference images (the most useful one for
  consistency).
- **Ideogram / Recraft** — free tier; Recraft's "vector/flat" style produces output that fits this
  project very well.
- **Stable Diffusion (local, e.g. A1111/ComfyUI)** — completely free and unlimited; dressing the
  same silhouette in costumes via ControlNet (Section 4.3) works most reliably here. Requires a GPU.

### 0.2 Character consistency (making 150 costumes look like the same character)

- Once the base character is approved, **one reference PNG** is locked in
  (`Assets/Art/Reference/base_mascot.png`).
- All subsequent character generations feed this image as an **img2img / image reference** input
  (Leonardo "Image Guidance", SD "img2img/ControlNet lineart"; not possible in Copilot — use Copilot
  only for silhouette-independent work like icons/planets).
- Add this sentence to the prompt every time:
  `same character as the reference image, identical silhouette, identical helmet and eyes, only the costume/outfit changes`
- Selection: take 4+ variants from each generation, pick the most consistent one, and if needed do
  small touch-ups in Krita/GIMP/Photopea (free).

### 0.3 Post-process pipeline (for every asset)

1. Clean up the background (if the generation tool doesn't output transparency: Photopea / rembg /
   any free bg-removal tool).
2. Edge cleanup: no semi-transparent pixel residue outside the outline (Unity turns it into a white
   halo).
3. Resize (Section 0.4) and place under `Assets/Art/Sprites/...` using the naming scheme
   (costume: `costume_{id}.png`, avatar: `avatar_{id}.png`, icon: `icon_{name}.png`).
4. Unity import: Sprite (2D and UI) · PPU **100** for all in-game sprites (UI icons are free-form) ·
   character pivot = **Bottom Center** (radial alignment rotates the head to face outward from the
   planet center while the feet stay planted on the surface) · all costumes/icons/avatars in a
   single Sprite Atlas.

### 0.4 Target resolutions

| Asset | Generation | In-game |
|---|---|---|
| Base character & costume (character) | 1024×1024 | ~512px tall |
| Costume (weapon) | 1024×1024 | ~256–384px wide |
| Avatar | 512×512 | 256×256 |
| Weapon/ability HUD icon | 512×512 | 128–256 |
| Planet | 2048×2048 | 1024+ (1/3 of the screen, no zoom) |
| UI/economy icons | 512×512 | 128–256 |

---

## 1. What Kind of Game This Is (context — give this to the art tool first)

**In one sentence:** A funny, colorful mobile arena combat game where players take turns shooting
at each other on top of small, fist-sized round planets, feeling like a mix of Worms + Angry Birds
Space + Brawl Stars.

**Scene (camera/scale):** Camera is fixed, side-on; the whole planet and all characters are on
screen at once. A small planet in the center (~1/4–1/3 of the screen), with tiny but big-headed
characters on it; deep space + stars + distant planet silhouettes in the background. Characters
stick to the planet's curved surface, their heads always facing outward from the planet's center —
**two opposing characters can appear upside down relative to each other.** This is the game's
signature visual and the constraint that defines all character design: **the silhouette must read
from every angle, even flipped upside down.**

**Tone:** Funny, toylike, childlike-energetic. Never realistic/bloody/dark. Explosions are big and
flashy but cartoon-style ("I took damage and got dazed" — no blood, just soot smudges and dizzy
eyes). Palette: saturated candy tones — neon purple/cyan space + warm orange/yellow explosions.
Never a gray-brown realistic military palette.

**Reference games:** Brawl Stars (character proportions + UI — the project's UI was approved by
comparing it one-to-one against real BS screenshots: dark anthracite panels, Titan One font, thick
outlined white headings, yellow highlights), Worms (weapon variety + destructible terrain + humor),
Angry Birds Space (small round planet gravity feel), Clash Royale / King of Thieves (mobile f2p
meta-economy feel).

**Gameplay:** Turn-based; projectiles trace curved trajectories around the planet with custom
gravity (they can wrap around behind the planet), and the impacted surface is destroyed piece by
piece (destructible sphere). Weapons: Pistol, Shotgun, RPG, Grenade, Bomb. Abilities: BlackHole,
Teleport, Shield, BatHammer, SuperJump. Meta: level/prestige, Gold/Gem, 150 cosmetic costumes, 50
achievements, quests, chests, leaderboard, friends, 7 languages. Platform: Android → iOS.

**Mood-board keywords:**

```
tiny round cartoon planet, whimsical space arena battle, toylike chibi space creature warriors,
candy-colored sci-fi palette, comedic oversized explosions, playful not gory,
Worms-meets-Brawl-Stars, zero-gravity planetoid combat, punchy saturated colors,
family-friendly cartoon violence
```

---

## 2. Universal Style Blocks

### 2.1 Positive style block — add to EVERY prompt

```
chunky cartoon mobile game art style, thick bold dark outlines, flat saturated vibrant colors,
simplified stylized proportions (Brawl Stars / Clash Royale aesthetic), clean vector-like shapes,
soft two-tone cel shading with punchy rim light, rounded friendly shape language (circles and
capsules, no sharp realistic detail), centered composition, transparent background,
high-quality 2D game asset, no text, no watermark
```

### 2.2 Negative prompt block — add if the tool supports it

```
photorealistic, realistic human anatomy, painterly soft rendering, gritty, grimdark, blood, gore,
military camouflage realism, muddy desaturated colors, thin delicate limbs, tiny intricate details,
3D render, depth of field, background scenery, drop shadow on ground, text, letters, logo,
watermark, signature, frame, border
```

(On tools that don't accept negative prompts — e.g. Copilot Designer — add the critical ones to the
positive prompt as "no photorealism, no text, no background".)

### 2.3 Shape language rule (the design compass for all assets)

Every visual in the game derives from the same geometry: the **circle**. Planet round → helmet round
→ body capsule → explosion round → avatar frame round. Sharp/realistic detail enters only as rarity
increases, and in moderation (Epic/Legendary silhouette pieces). If an asset can't be built out of
circles, it's off-style — redesign it.

---

## 3. Base Character — "Mascot Astronaut" (DO THIS FIRST; everything builds on it)

### 3.1 Design decision and rationale

The character is **not human, not animal, not insect** — it's a deliberately species-ambiguous,
chubby **mascot astronaut creature** with a round glass helmet. Only two huge eyes are visible
inside the helmet (no nose, mouth, hair or skin).

Rationale (can also be given to the art tool as the answer to "why"):

1. **360° readability:** The character also stands upside down under the planet. A round helmet +
   capsule body reads the same from every angle; a thin-limbed human/insect loses its silhouette
   when flipped.
2. **150 costumes on a single silhouette:** On a species-neutral creature the costume becomes the
   character *itself* (the one wearing the Phoenix costume "is a phoenix"); on a specific
   human/animal it just looks like "a guy in an outfit". Also, since the face (eye layer) never
   changes, color-variation costumes come for free (Section 4.3).
3. **Small-screen scale:** Only the eyes and head proportion can carry personality; detail can't.
4. **IP ownership:** The current placeholder (`player_15.png`) looks like Luigi — unpublishable. The
   mascot must be entirely original; it's the face of the game from the app icon to store artwork.

### 3.2 Anatomy specification

- **Helmet/head:** ~55% of total height; transparent round glass dome; a single bright reflection
  streak on the glass.
- **Eyes:** two large oval eyes inside the helmet (~40% of the helmet); these are the emotion engine
  (Section 3.4).
- **Body:** short chubby capsule; snug space suit; a small round panel/badge on the chest (the theme
  emblem goes here on costumes).
- **Arms:** short and stubby; oversized round gloves; one hand holds the weapon.
- **Boots:** exaggeratedly large "moon boots" — both cuteness and the "sticking to a curved surface"
  fantasy.
- **Weapon scale:** the same size as the body, not realistic — the selected weapon must be
  identifiable at a glance.
- **Color (default/Gray Soldier):** light gray-white suit, dark anthracite details, one warm accent
  (orange) — the base stays neutral so costume colors pop on top of it.

### 3.3 Main prompt (idle pose)

```
a small chubby species-neutral mascot astronaut creature, 2D side-view mobile game character
sprite, oversized round transparent glass dome helmet taking up more than half of its body,
two big expressive oval eyes floating inside the helmet (no nose, no mouth, no hair, species
deliberately ambiguous), short plump capsule-shaped body in a snug light-gray space suit with
dark charcoal accents and one small round chest badge, stubby arms with oversized round gloves,
very large chunky moon boots, standing in a confident idle combat stance holding a compact
sci-fi pistol aimed sideways at arm's length, single warm orange accent color,
[+ 2.1 style block] [+ 2.2 negative block]
```

### 3.4 Pose and expression set (variants to produce for the base)

The following variants are produced from the same reference image (Section 0.2) — since animation
will be done on the Unity side with squash&stretch/rotation, a frame-by-frame sprite sheet is NOT
required; one image per pose is enough:

| Variant | Prompt addition | Usage |
|---|---|---|
| idle | (as in 3.3) | scene, wardrobe preview |
| aim | `aiming carefully, one eye squinted, arm extended` | aiming turn |
| hurt | `dizzy knocked-back pose, swirly dazed eyes, small soot smudges, comedic, not gory` | damage |
| victory | `cheering with both arms up, star-shaped sparkling happy eyes` | match-end winner |
| defeat | `slumped sitting pose, big teary sad eyes, cracked helmet glass (small comedic crack)` | match-end loser |
| panic | `being pulled sideways, panicked wide eyes, gripping the ground` | BlackHole pull |

**Eye expressions can also be cropped out as a separate layer** (Section 3.5) — which reduces the
number of poses needed.

### 3.5 Layered production (the key to costume economics)

Once the base is approved, the single sprite is split in Photopea/Krita into the following layers
and stacked in Unity with separate SpriteRenderers:

1. **body_base** — suit+boots+gloves (the layer costumes recolor/replace)
2. **helmet_glass + eyes** — never changes in any costume (character identity)
3. **costume_overlay** — armor/wing/cape pieces added on Rare+ costumes
4. **weapon** — the held weapon, fully independent (weapon costumes change only this)

Gain: Common/Uncommon character costumes = hue-shift/tint on body_base (NO generation); eyes/
expressions are automatically identical across all costumes; weapon skins are produced independently
of the character.

### 3.6 Base approval checklist (don't start Section 4 before it passes)

- [ ] Silhouette and eyes still read when scaled down to 128px
- [ ] Still recognizable as a character when rotated 180° (upside down)
- [ ] Tested on top of the real planet sprite in a screenshot
- [ ] Doesn't get ugly in the hue-shift test (recolored to 3-4 different colors)
- [ ] Nobody says it "looks like" any existing IP (Mario/Luigi, Among Us, Fall Guys, etc.) —
      especially the Among Us check: the eyes + inside of the helmet glass must be visible, NOT a
      single visor

---

## 4. Costumes — 150 items

### 4.1 Rarity visual language (colors already coded in the UI)

| Rarity | Hex | Material/detail language | Prompt phrase |
|---|---|---|---|
| Common | `#9EA6B2` gray | flat color, matte, no ornaments | `plain flat recolor, matte fabric, no ornaments` |
| Uncommon | `#4DD966` green | simple pattern, slight sheen | `simple pattern or texture detail, slight sheen` |
| Rare | `#4088FF` blue | distinct silhouette detail, blue rim light | `distinct silhouette accessory, soft blue rim light` |
| Epic | `#A659FF` purple | dramatic silhouette, energy effects | `dramatic silhouette pieces, glowing purple energy particles` |
| Legendary | `#FFCC33` gold | gold/prismatic plating, aura | `ornate golden prismatic plating, dynamic radiant energy aura` |

### 4.2 Type and rarity distribution

| Rarity | Character | Weapon | Total |
|---|---|---|---|
| Common | 24 | 16 | 40 |
| Uncommon | 19 | 16 | 35 |
| Rare | 18 | 17 | 35 |
| Epic | 16 | 9 | 25 |
| Legendary | 9 | 6 | 15 |
| **TOTAL** | **86** | **64** | **150** |

### 4.3 Production strategy — NOT 150 generations, ~70 generations + hue-shift

| Layer | Scope | Method | Actual generations |
|---|---|---|---|
| Common character (24) | all color variations | hue-shift/tint on body_base (Unity material or Photopea) | **0** |
| Common weapon (16) | color variation | first generate 5 base weapon sprites, then tint | **5** (base weapons) |
| Uncommon (35) | color + simple pattern | hue-shift + generate 8-10 pattern overlays (camo, ice crack, circuit, leaf...) and mix | **~10** (overlays) |
| Rare (35) | unique detail | generate one by one (img2img with reference) | 35 |
| Epic (25) | dramatic silhouette | generate one by one | 25 |
| Legendary (15) | fully unique | generate one by one, 2-3 attempts if needed | 15 |

Total actual generations ≈ **90 images** (instead of 150), and Common/Uncommon come out instantly,
for free, and 100% consistent. Hue-shift color targets are obvious from the costume name (e.g. c005
Yellow Storm → yellow).

### 4.4 Theme descriptor dictionary (the English block that replaces {THEME})

| Theme | Descriptor |
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

### 4.5 Prompt templates

**Character costume (Rare+):**

```
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "{NAME}" costume, theme: {THEME DESCRIPTOR},
{RARITY PROMPT PHRASE}, accent color {RARITY_HEX}, full-body side-view 2D game sprite,
[+ 2.1 style block] [+ 2.2 negative block]
```

**Weapon costume:**

```
sci-fi cartoon {pistol|shotgun|rocket launcher|grenade|time bomb} weapon skin, "{NAME}",
theme: {THEME DESCRIPTOR}, {RARITY PROMPT PHRASE}, accent color {RARITY_HEX}, side-view 2D game
asset, chunky oversized toylike proportions, bold silhouette, [+ 2.1] [+ 2.2]
```

**Filled-in examples:**

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

### 4.6 Full list (id · Name · Type · Theme)

**COMMON (40)** — characters are hue-shifts, weapons are 5 base + tint (Section 4.3):
c001 Gray Soldier · C · Other (starter) — c002 Standard Blue · C · Other (starter) — c003 Red
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

**UNCOMMON (35)** — hue-shift + pattern overlay:
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

**RARE (35)** — generated one by one:
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

**EPIC (25)** — generated one by one:
e001 Nebula Warrior · C · Space — e002 Dragon Lord · C · Fantasy — e003 Cyber God · C · Cyber —
e004 Death Spirit · C · Dark — e005 Volcano God · C · Fire — e006 Ice Storm · C · Ice — e007
Forest Deity · C · Nature — e008 Titan Armor · C · Mech — e009 Olympian God · C · Myth — e010
Quantum Shadow · C · Cyber — e011 Galactic Emperor · C · Space — e012 Ancient Dragon · C ·
Fantasy — e013 Neon Demon · C · Dark — e014 Plasma God · W · Cyber — e015 Dragon Breath · W ·
Fantasy — e016 Dark Star · W · Dark — e017 Volcano Cannon · W · Fire — e018 Ice Crystal · W ·
Ice — e019 Nano Swarm · W · Mech — e020 Rune Burst · W · Fantasy — e021 Nebula Bomb · W · Space —
e022 Titan Laser · W · Mech — e023 Mythic Armor · C · Myth — e024 Crystal Golem · C · Ice — e025
Crow King · C · Dark

**LEGENDARY (15)** — generated one by one, with the highest care:
l001 Cosmic Master · C · Space — l002 Dragon Emperor · C · Fantasy — l003 Dark God · C · Dark —
l004 Doom Lord · C · Dark — l005 Time Master · C · Myth — l006 Universe Warrior · C · Space —
l007 Ancient Giant · C · Myth — l008 Bionic God · C · Mech — l009 Phoenix Warrior · C · Fire —
l010 Cosmic Destroyer · W · Space — l011 God Sword · W · Myth — l012 Dragon Heart · W · Fantasy —
l013 Black Hole Cannon X · W · Space — l014 Doom Hammer · W · Dark — l015 Creator's Power · W · Myth

---

## 5. Profile Avatars — 16 items

Prompt template:

```
circular game profile icon, {concept}, dominant color {hex}, cosmic space phenomenon,
simple bold iconic shape readable at very small size, flat design with subtle glow,
[+ 2.1 style block] [+ 2.2 negative block]
```

| id | Name | Hex | Concept (English, goes into the prompt) |
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

Note: since avatars contain no character, no reference image is needed — they can be produced with
reference-free free tools like Copilot Designer.

---

## 6. Weapon & Ability HUD Icons — 10 items

Prompt template:

```
square game HUD ability icon, {concept}, sci-fi space combat gear, bold silhouette readable
at 64px, subtle dark vignette inside icon frame, [+ 2.1] [+ 2.2]
```

| id | Concept (English) |
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

The current icons (`Assets/Art/Sprites/UI/*_icon.png`) are random placeholders — all of them will be
replaced by this set. `fly_icon.png` corresponds to SuperJump; migrate it to the new naming scheme.

---

## 7. Planet / Map Art — 4 themes

A destructible planet needs two pieces: a **surface sprite** + an **inner texture** that gets exposed
in craters (a darker rock cross-section). The prompt should produce both for each theme, or the
inner texture can be a single shared image.

Prompt template:

```
small round destructible cartoon planet, perfect circular silhouette, side-view 2D game asset,
{theme}, chunky surface details on the rim (craters/rocks/vegetation reading in silhouette),
slightly darker core color hinting at the inner cross-section, deep space starfield behind,
[+ 2.1] [+ 2.2]
```

| Theme | {theme} block |
|---|---|
| Rocky/Neutral (existing) | `gray-brown rocky asteroid surface with big cartoon craters` |
| Ice | `white-cyan glacier surface, crystal ice spikes on the rim, frosty glow` |
| Lava | `dark crimson cracked surface with glowing orange lava veins and small eruptions` |
| Forest | `lush green mossy surface with giant mushrooms and tiny cartoon trees on the rim` |

Extra: the background starfield can be produced separately as a wide, scrollable (parallax) image:

```
deep space starfield background, distant silhouetted planets, purple-cyan nebula haze,
subtle vignette, wide seamless game background, [+ 2.1 but with "full-bleed background"
INSTEAD OF transparent background] [+ 2.2]
```

---

## 8. UI / Economy Icons

All with the 2.1 style block + the suffix `square game UI icon, bold silhouette readable at 64px`:

| Asset | Concept |
|---|---|
| XP icon | `yellow-white star chevron badge` |
| Gold icon | `shiny gold coin with star emboss` |
| Gem icon | `purple-blue faceted crystal gem` |
| Chest Common | `simple wooden chest with bronze bands` |
| Chest Rare | `silver-blue metal chest with glowing seams` |
| Chest Epic | `ornate gold-purple chest with sparkle particles` |
| Achievement badge ×4 | `circular achievement badge frame` + the rarity colors from Section 4.1 |
| Trophy | `golden trophy cup with tiny planet on top` |
| **App icon** | `tiny round planet with the mascot astronaut standing on top waving,
  bold readable at 48px, app icon composition` — produced once the mascot is approved, using the reference |

Store artwork (Play Console feature graphic, screenshot frames) is separate marketing work — don't
start it before the base + 2-3 costumes + 1 planet are ready.

---

## 9. Audio

Recorded as completed in TODO.md (20 SFX + menu music) — out of scope for this document,
reference: v1 Section 9.

---

## 10. Priority Order (v2 — changed)

1. **Base mascot character** (Section 3) — start nothing before the approval checklist passes; the
   Luigi-like placeholder is a release blocker.
2. **5 base weapon sprites** (Section 4.3) — both the in-hand weapon art and the tint source for the
   16 Common weapon costumes.
3. **Weapon/ability HUD icons** (Section 6) — on screen in every match, the current ones are random.
4. **16 avatars** (Section 5) — the code side is ready, only the art is missing; can be produced
   without a reference, independent/parallel work.
5. **Costumes** (Section 4) — order: Common (hue-shift, ~1 day) → Uncommon (overlay) →
   Legendary (15, highest showcase value) → Epic → Rare.
6. **Planet variety** (Section 7) — 2-3 new themes.
7. **App icon + store artwork** (Section 8) — mandatory for Play Console registration, but can't be
   done before the mascot is finalized.
