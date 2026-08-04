# CosmicRumble — Art Generation Master Prompt (v2)

> Difference from v1: Coplay (paid) was dropped — all prompts are written tool-agnostically and
> negative-prompt + consistency techniques were added so they work with free tools. The base
> character design was nailed down: **species-neutral mascot astronaut** (not human/animal/insect).
> Costume production was re-planned around hue-shift economics, and the priority order changed
> (base character first).
>
> **2026-08-04 update:** Section 4 (Costumes) was rewritten. The code-side costume system was
> redesigned on 2026-07-16 (see `TODO.md` "Costume Redesign") from 150 free-standing costumes down
> to **5 characters × 3 tiers = 15 costumes total**, with weapon costumes removed entirely
> (`CostumeDefinition.costumeType` is now always `Character` — see `CostumeAssetGenerator.cs`). The
> 2026-07-30 "Galactic Rumble Show" theme decision named the 5 characters (Nova/Blitz/Titan/Scope/
> Vex). This document previously still described the old 150-item plan; it's now aligned with the
> real 15-item system, so the hue-shift/pattern-overlay production economics (old Section 4.3) no
> longer apply — 15 is small enough to just generate directly.

---

## 0. How to Use This Document

1. Give the art tool **Section 1** first (the game context) — the tool should know the answer to
   "why is it like this".
2. Add the **Section 2.1** style block to every prompt; if the tool supports it, add the
   **Section 2.2** negative block too.
3. First task is **Section 3** (base character). Produce nothing else until the base is approved —
   costumes, icons and avatars will all derive from the base's shape language.
4. For costumes, see **Section 4** — the current system is only 15 items (5 characters × 3 tiers),
   small enough to generate directly, no hue-shift/layer economy required.

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

### 0.2 Character consistency (making all 15 costumes look like the same character)

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
| Base character & costume (15 total, character-only) | 1024×1024 | ~512px tall |
| Avatar | 512×512 | 256×256 |
| Weapon/ability HUD icon | 512×512 | 128–256 |
| Planet | 2048×2048 | 1024+ (1/3 of the screen, no zoom) |
| UI/economy icons | 512×512 | 128–256 |
| Base weapon (in-hand) / orb prop | 1024×1024 | ~256–384px wide |
| Projectile (bullet/pellet/rocket/orb) | 512×512 | 32–96px, small and fast on screen |
| Impact/explosion VFX | 512×512 (vortex sheet: 512×512 per frame) | 128–512 depending on Small/Large |
| Button/nav icon glyph | 256×256 | 48–64 |

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
Teleport, Shield, BatHammer, SuperJump. Meta: level/prestige, Gold/Gem, 15 cosmetic costumes
(5 characters × 3 tiers), 50 achievements, quests, chests, leaderboard, friends, 7 languages.
Platform: Android → iOS.

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
2. **A believable 5-character roster on one silhouette:** On a species-neutral creature the
   costume becomes the character *itself* (Nova the show host actually reads as a persona, not
   just "a guy in a red outfit"). Since the face (eye layer) never changes, all 5 characters × 3
   tiers stay automatically consistent with each other and with the base (Section 4.3).
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
4. **weapon** — the held weapon (Section 6.1), fully independent and untouched by any costume

Gain: eyes/expressions are automatically identical across all 15 costumes since they never touch
the `helmet_glass + eyes` layer; the held weapon (Section 6.1) is produced fully independently of
the character, since costumes no longer touch weapons at all (weapon costumes were removed —
Section 4).

### 3.6 Base approval checklist (don't start Section 4 before it passes)

- [ ] Silhouette and eyes still read when scaled down to 128px
- [ ] Still recognizable as a character when rotated 180° (upside down)
- [ ] Tested on top of the real planet sprite in a screenshot
- [ ] Doesn't get ugly in the hue-shift test (recolored to 3-4 different colors)
- [ ] Nobody says it "looks like" any existing IP (Mario/Luigi, Among Us, Fall Guys, etc.) —
      especially the Among Us check: the eyes + inside of the helmet glass must be visible, NOT a
      single visor

---

## 4. Costumes — 15 items (5 characters × 3 tiers, no weapon costumes)

The old 150-item plan below this line was superseded by the 2026-07-16 code redesign: 5 characters,
each with 3 tiers, all `costumeType = Character` (weapon costumes were removed entirely — see
`CostumeAssetGenerator.cs` and `TODO.md` "Costume Redesign"). This is small enough to just generate
directly — no hue-shift/pattern-overlay economy needed.

### 4.1 Character roster (5) — "Galactic Rumble Show" theme (2026-07-30 decision)

The game's meta-concept: an interplanetary arena show broadcast on TV; the 5 characters are the
show's famous contestants (purely cosmetic — `characterId` isn't tied to weapon/ability choice,
every player unlocks all 9 weapons by level).

| characterId | Name | Persona | Palette |
|---|---|---|---|
| 1 | **Nova** | charismatic show host/mascot | bright, fiery, gold/red |
| 2 | **Blitz** | fast, energetic acrobat | neon blue, electric effects |
| 3 | **Titan** | heavy, armored show of force | metallic gray, coarse lines |
| 4 | **Scope** | cool-headed sharpshooter | minimal, technical, dark green |
| 5 | **Vex** | mysterious master of control | purple/black, black hole motifs |

Note: "Nova" is deliberately shared with the profile-avatar system (Section 5's `av01 Nova`) — same
name used for brand consistency, since Nova is the main mascot identity in both systems.

### 4.2 Tier structure (3 per character)

Code-internal tier names are `Standard/Advanced/Elite`; the intended user-facing names (a
`displayName`/loc-string-only change, no code change) are **Rookie → Star → Legend**. Going up a
tier does **not** change the character's persona color — it only increases ornamentation/energy
density, reusing the old rarity material language:

| Tier | Actual `CostumeRarity` (varies per character — see 4.3 table) | Prompt phrase |
|---|---|---|
| _1 Standard / Rookie | Common on all 5 | `plain flat recolor, matte fabric, no ornaments` |
| _2 Advanced / Star | Uncommon or Rare | `simple pattern detail` (Uncommon) or `distinct silhouette accessory, soft blue rim light` (Rare) |
| _3 Elite / Legend | Epic or Legendary | `dramatic silhouette pieces, glowing energy particles matching the character's OWN persona color` (Epic) or `ornate golden prismatic plating, dynamic radiant golden energy aura` (Legendary) |

### 4.3 Prompt template

```
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "{CHARACTER NAME} — {TIER NAME}" costume,
persona: {PERSONA DESCRIPTOR}, dominant color {PALETTE}, {TIER MATERIAL PHRASE}, full-body
side-view 2D game sprite, [+ 2.1 style block] [+ 2.2 negative block]
```

### 4.4 Full list — 15 filled-in prompts (copy-paste ready, matches `CostumeAssetGenerator.cs` ids exactly)

```
(c1_1 — Nova, Rookie tier, Common)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Nova — Rookie" costume, persona:
charismatic galactic show host/mascot, dominant color bright gold and red, plain flat recolor,
matte fabric, no ornaments, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c1_2 — Nova, Star tier, Rare, unlock: 800 Gold)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Nova — Star" costume, persona: charismatic
galactic show host/mascot, dominant color bright gold and red, distinct silhouette accessory (a
small showman's cape or shoulder spotlight rig), soft warm rim light, full-body side-view 2D
game sprite, [+ 2.1] [+ 2.2]

(c1_3 — Nova, Legend tier, Epic, unlock: Level 20)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Nova — Legend" costume, persona:
charismatic galactic show host/mascot at the peak of fame, dominant color bright gold and red,
dramatic silhouette pieces (a radiant spotlight-crown/antenna array), glowing orange-gold
energy particles matching Nova's own persona color, full-body side-view 2D game sprite,
[+ 2.1] [+ 2.2]

(c2_1 — Blitz, Rookie tier, Common)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Blitz — Rookie" costume, persona: fast
energetic acrobat, dominant color neon blue, plain flat recolor, matte fabric, no ornaments,
full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c2_2 — Blitz, Star tier, Uncommon, unlock: chest drop)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Blitz — Star" costume, persona: fast
energetic acrobat, dominant color neon blue, simple lightning-bolt pattern detail, slight
metallic sheen, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c2_3 — Blitz, Legend tier, Epic, unlock: 50 Gem)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Blitz — Legend" costume, persona: fast
energetic acrobat at top speed, dominant color neon blue, dramatic silhouette pieces (small
speed-fin accents on the boots/shoulders), glowing electric-blue energy particles and crackling
speed-lines matching Blitz's own persona color, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c3_1 — Titan, Rookie tier, Common)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Titan — Rookie" costume, persona: heavy
armored show of force, dominant color metallic gray, plain flat recolor, matte fabric, no
ornaments, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c3_2 — Titan, Star tier, Rare, unlock: Level 10)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Titan — Star" costume, persona: heavy
armored show of force, dominant color metallic gray, distinct silhouette accessory (thicker
shoulder plating), soft blue rim light, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c3_3 — Titan, Legend tier, Legendary, unlock: achievement EFSANE)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Titan — Legend" costume, persona: heavy
armored champion of the show, dominant color metallic gray with ornate golden prismatic
plating, dynamic radiant golden energy aura, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c4_1 — Scope, Rookie tier, Common)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Scope — Rookie" costume, persona:
cool-headed sharpshooter, dominant color dark green, plain flat recolor, matte fabric, no
ornaments, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c4_2 — Scope, Star tier, Rare, unlock: 1200 Gold)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Scope — Star" costume, persona:
cool-headed sharpshooter, dominant color dark green, distinct silhouette accessory (a small
targeting-scope visor attachment), soft blue rim light, full-body side-view 2D game sprite,
[+ 2.1] [+ 2.2]

(c4_3 — Scope, Legend tier, Epic, unlock: 80 Gem)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Scope — Legend" costume, persona:
cool-headed elite marksman, dominant color dark green, dramatic silhouette pieces (a precision
targeting-array shoulder rig), glowing lime-green energy particles matching Scope's own persona
color, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c5_1 — Vex, Rookie tier, Common)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Vex — Rookie" costume, persona: mysterious
master of control, dominant color deep purple and black, plain flat recolor, matte fabric, no
ornaments, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c5_2 — Vex, Star tier, Uncommon, unlock: chest drop)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Vex — Star" costume, persona: mysterious
master of control, dominant color deep purple and black, simple swirling void pattern detail,
slight dark sheen, full-body side-view 2D game sprite, [+ 2.1] [+ 2.2]

(c5_3 — Vex, Legend tier, Epic, unlock: Level 35)
same character as the reference image, identical silhouette, identical round glass helmet and
big oval eyes, only the space suit costume changes: "Vex — Legend" costume, persona: mysterious
master of control at full power, dominant color deep purple and black, dramatic silhouette
pieces (a small orbiting black-hole motif on the shoulder), glowing violet-purple energy
particles with tiny event-horizon wisps matching Vex's own persona color, full-body side-view
2D game sprite, [+ 2.1] [+ 2.2]
```

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

### 6.1 Base weapon in-hand art — 6 items (full-size, held by the mascot)

Distinct from Section 6's small HUD icons above: this is the **full-size weapon sprite the
character actually holds** in a match — produced once per weapon, used directly for the in-hand
combat art. There are no weapon costumes/skins in the current system (Section 4), so each of these
6 is the single, final art for that weapon — not a tint source. Layered independently from the
character body per Section 3.5 (`weapon` layer) — same held scale as Section 3.2/3.3.

Shared prompt skeleton (each entry below fills in `{weapon description}`):

```
sci-fi cartoon {weapon description}, toylike chunky oversized proportions, gripped by a stubby
round mascot glove visible only at the handle (cropped, matches the base character's glove
shape), side-view 2D game weapon sprite, same scale relationship as the base character,
neutral light-gray/anthracite body color with one warm orange accent stripe (matches the base
character's default palette so recolors and costume skins read clearly on top), bold thick dark
outline, flat color blocks, soft two-tone cel shading, [+ 2.1 style block] [+ 2.2 negative block]
```

| id | Full weapon description to drop into `{weapon description}` |
|---|---|
| weapon_pistol_base | `compact fast-draw blaster pistol, short round barrel with a glowing energy-cell window near the grip, single bright muzzle-tip ring, tiny fold-down sight` |
| weapon_shotgun_base | `heavy wide double-barrel blaster shotgun, oversized flared barrel mouths, chunky pump-grip under the barrel, a row of 3 small glowing shell-charge indicators on the side` |
| weapon_rpg_base | `shoulder-fired rocket launcher tube, wide open rear vent, a chunky rocket already loaded with its orange-striped warning nose cone peeking out of the front, small carry handle on top` |
| weapon_grenade_base | `round comedic hand grenade, oversized safety pin and lever exaggerated in size for readability, simple segmented sphere body, small pull-ring dangling` |
| weapon_bomb_base | `hand-held cylindrical time-bomb canister before it's placed, a round glass-covered analog clock-face timer on top, one blinking red light, a fold-out magnetic clamp foot tucked against the side` |
| weapon_bathammer_base | `chunky sci-fi bat/hammer hybrid, thick round barrel-shaped hitting end with a glowing horizontal energy-cell stripe, short sturdy handle wrapped in grip tape, tiny spark particles around the hitting end` |

### 6.2 Ability orb props — held before throw (BlackHole & Teleport only)

BlackHole and Teleport are thrown as a small orb (see `BlackHoleProjectile`/`TeleportOrbProjectile`),
so — like the grenade above — the character needs a held-orb sprite for the aim pose. Shield and
SuperJump are **self-cast** (no held prop; see 6.3 for their pure-VFX treatment) and need no new
character-held art beyond the existing pose set in Section 3.4.

```
(blackhole_orb_held)
a small dense dark-purple-black sphere held between two stubby round glove fingers,
swirling event-horizon rings visible on its surface, thin crackling purple energy tendrils
reaching outward a short distance, ready-to-throw pose, side-view 2D game prop sprite,
[+ 2.1 style block] [+ 2.2 negative block]

(teleport_orb_held)
a small glowing cyan-blue crystal orb held between two stubby round glove fingers,
a faint spiral portal-swirl pattern visible inside the crystal, a few loose light particles
already drifting off its surface, ready-to-throw pose, side-view 2D game prop sprite,
[+ 2.1 style block] [+ 2.2 negative block]
```

### 6.3 Projectiles, trails & impact VFX — 16 items

This is currently the biggest real gap in the project: `Assets/Art/Sprites/Projectiles/` holds only
four mismatched placeholders (`pistol_bullet.png`, `assault_bullet.png`, `grenade.png`,
`rocket grenade.png`) and one placeholder sheet (`BlackHoleVortex_Sheet.png`) — none share a style,
and Bomb/Teleport/muzzle-flash/impact-spark/explosion have no dedicated asset at all. Every
projectile must read as a small, fast, clear silhouette against the dark starfield (Section 7's
background), and every impact/explosion must stay in the game's "comedic, not gory" register
(Section 1).

**Projectiles** — shared skeleton:

```
{projectile description}, small fast-traveling 2D game projectile sprite, side-view, bold thick
dark outline, flat saturated color with a bright glowing energy core, a short motion-streak tail
suggesting speed, reads clearly as a tiny object against a dark starfield, [+ 2.1] [+ 2.2]
```

| id | `{projectile description}` | Note |
|---|---|---|
| proj_pistol_bullet | `elongated capsule-shaped energy bolt, orange-yellow glowing core with a white-hot tip` | replaces `pistol_bullet.png` |
| proj_shotgun_pellet | `tiny round energy pellet, cyan-white glowing dot with a short soft tail` | single pellet — the game instantiates a 6–8 pellet spread from this one sprite; replaces `assault_bullet.png` |
| proj_rpg_rocket | `chunky toylike rocket with small tail fins and a bright orange thruster-flame trail, red-and-white body with a bold warning-stripe nose cone` | replaces the misnamed `rocket grenade.png` |
| proj_grenade_flight | `same silhouette as weapon_grenade_base, tumbling in flight with soft curved motion-lines around it, pin already pulled, a tiny lit fuse spark on top` | in-flight variant of 6.1's held grenade |
| proj_blackhole_orb | `small dense dark-purple sphere with visible swirling event-horizon rings and thin lightning-like energy tendrils reaching outward` | in-flight variant of 6.2's held orb, pre-vortex |
| proj_teleport_orb | `glowing cyan crystal orb leaving a spiral particle trail behind it, faint translucent afterimages suggesting warp speed` | in-flight variant of 6.2's held orb |
| proj_bomb_placed | `cylindrical time-bomb canister sitting on the ground, a fold-out magnetic clamp foot gripping the surface, round glass-covered analog clock-face timer with one big blinking red light, comedic "about to go off" tension` | placed/idle state, distinct from the hand-held `weapon_bomb_base` |

**Impact & explosion VFX** — shared skeleton (single "peak frame" sprite; Unity scales/fades it in
code, no sprite-sheet animation needed unless noted):

```
{effect description}, cartoon comedic burst, radial flat spiky shapes, bright warm core fading
outward, thick dark outline, no smoke realism, no blood, big and flashy but toylike, side-view
2D game VFX sprite, [+ 2.1] [+ 2.2]
```

| id | `{effect description}` | Note |
|---|---|---|
| vfx_muzzle_flash | `small star-burst flash at a gun barrel tip, white-yellow core with a few short orange spikes` | plays once per shot, Pistol/Shotgun/RPG |
| vfx_bullet_impact | `small spark burst on impact, thin bright radiating white-cyan lines, a tiny cartoon dust puff` | plays on any bullet/pellet hit |
| vfx_explosion_small | `mid-size explosion burst, bright yellow-white core fading to orange at the tips, a few comedic soot-smudge specks flying outward, no gore` | Grenade, RPG, HandGrenade |
| vfx_explosion_large | `big screen-filling explosion burst, same yellow-orange language as the small explosion but roughly 3× larger, with an extra faint shockwave ring` | Bomb |
| vfx_blackhole_vortex | `swirling dark purple-black vortex pulling in thin light-streaks, a pulsing glowing event-horizon core at the center` | replaces `BlackHoleVortex_Sheet.png`; keep as a short 4–6 frame looping sheet, not a single frame |
| vfx_teleport_portal | `ring-shaped cyan-blue warp portal, swirling energy particles forming the rim, a bright flash at the core` | reused at both the departure and arrival point |
| vfx_shield_bubble | `translucent glowing energy bubble made of soft hexagon panel seams, a thin bright rim light, semi-transparent so whatever is inside stays visible` | wraps around the character sprite — not a standalone icon, needs alpha transparency across the whole shape |
| vfx_bathammer_impact | `a few bold cartoon star/spark shapes plus a short motion-swoosh line, bright yellow-white` | plays on melee contact |
| vfx_superjump_trail | `a soft glowing dust-puff cloud plus upward speed-lines`, warm orange-white glow | plays under the boots during the jump arc |

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

### 8.1 Navigation & action button icons — 15 items

Every button in the game is drawn procedurally in code (`UiKit.cs`: rounded-rect shape + solid
color + shadow/stroke, no image assets at all — see `MakeBrawlBtn`/`Item` in `MainMenuUI.cs`).
Today the drawer-menu buttons fall back to a **single capital letter inside a colored circle**
(e.g. `"S"` for Settings, `"L"` for Leaderboard — `MainMenuUI.cs` lines ~710–733) instead of a real
icon, and the big action buttons (`btn_wardrobe`, `btn_shop`, `btn_social`, `btn_quests`) have no
icon at all, only text. This set replaces every letter placeholder with a real glyph and adds an
icon to every icon-less action button. These are **glyphs meant to sit inside the existing
procedural button shape** — don't design a new button shell, just the icon layer.

Shared prompt skeleton:

```
square game UI icon glyph, {icon description}, bold simple silhouette readable at 48px,
flat single-color or two-tone glyph (works when tinted to any button accent color),
centered composition, transparent background, [+ 2.1 style block] [+ 2.2 negative block]
```

| id | Replaces / used by | `{icon description}` |
|---|---|---|
| btn_icon_settings | drawer `dw_settings` (was letter "S") | `mechanical gear/cog wheel, simple bold teeth` |
| btn_icon_leaderboard | drawer `dw_leaderboard` (was letter "L") | `three-step winner's podium with a small star above the tallest step` |
| btn_icon_achievements | drawer `dw_achievements` (was letter "A") | `circular medal badge with a star at its center and a short ribbon tail below` |
| btn_icon_account | drawer `dw_account` (was letter "@") | `the mascot's round glass helmet silhouette with the two big oval eyes, front-facing (identity/profile glyph, reuses Section 3 character design)` |
| btn_icon_training | drawer `dw_training` (was letter "T") | `round practice target with concentric rings and a dart stuck near the center` |
| btn_icon_party | drawer `dw_party` (was letter "P") | `two of the mascot's round helmets side by side, small "+" between them` |
| btn_icon_botmatch | drawer `dw_botmatch` (was letter "B") | `simple robot head with two square glowing eyes and a small antenna` |
| btn_icon_wardrobe | `btn_wardrobe` (text-only today) | `coat hanger with a small folded cape/costume draped over it` |
| btn_icon_shop | `btn_shop` (text-only today) | `space-themed shopping bag with a coin icon printed on the front` |
| btn_icon_social | `btn_social` (text-only today) | `two overlapping chat speech-bubbles, one with a small "+" for add-friend` |
| btn_icon_quests | `btn_quests` (text-only today) | `rolled scroll/checklist with one checkmark and one empty checkbox line` |
| btn_icon_play | `btn_play_big` main CTA (currently text-only "PLAY") | `bold solid play triangle pointing right, slightly rounded corners to match the game's shape language` |
| btn_icon_edit_avatar | `btn_edit_avatar` pencil badge | `simple pencil at a 45° angle, small sparkle at the tip` |
| btn_icon_close | `UiKit.CloseButton` (currently a text "X") | `bold rounded X mark, thick even stroke weight` |
| btn_icon_buy | `ShopPanelUI` `btn_buy` (currently text-only) | `single gold coin with a small "+" badge at its top-right corner` |

Note: no social-login provider buttons exist in the code (`LoginPanelUI`/`LoginScreenUI` have no
Google/Apple/Facebook branding), so there is nothing to generate there — if that ever changes, use
each provider's official brand asset, never an AI-generated approximation of a trademarked logo.

#### 8.1.1 Generation method — one icon per request, NOT a 15-icon grid sheet

**Don't generate all 15 as a single grid/contact-sheet image.** A first attempt at that produced a
sheet with a radial gradient background — cropping the 15 cells apart was easy, but making the
background transparent afterward was not: simple corner-color chroma-keying leaves grainy
noise and clips icon glow effects, because the gradient's color shifts across each cell (the same
"white halo" risk Section 0.3 already warns about). **Generate each icon as its own separate
request**, on a **solid flat plain white background** (not gradient, not radial, not a scene) —
a flat single color keys out to transparency reliably with a basic tool (or even Unity's own alpha
threshold), which a gradient never does. If the tool outputs true alpha transparency natively
(Recraft, some Leonardo presets), use that directly instead of white + keying.

Each prompt below is self-contained and ready to paste as-is (style block already folded in):

```
(btn_icon_settings)
mechanical gear/cog wheel icon, simple bold teeth, single isolated icon only, one object
centered in frame, no other icons, no grid, no text, no label, no caption, solid flat plain
white background (not gradient, not radial, not a scene), chunky cartoon mobile game art
style, thick bold dark outlines, flat saturated vibrant colors, clean vector-like shapes,
soft two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_leaderboard)
three-step winner's podium with a small glowing star above the tallest step, single isolated
icon only, one object centered in frame, no other icons, no grid, no text, no label, no
caption, solid flat plain white background, chunky cartoon mobile game art style, thick bold
dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft two-tone cel
shading, high-quality 2D game icon, no watermark

(btn_icon_achievements)
circular gold medal badge with a star at its center and a short red-blue ribbon tail below,
single isolated icon only, one object centered in frame, no other icons, no grid, no text, no
label, no caption, solid flat plain white background, chunky cartoon mobile game art style,
thick bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft
two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_account)
a round glass astronaut-helmet silhouette with two big glowing oval eyes inside, front-facing,
single isolated icon only, one object centered in frame, no other icons, no grid, no text, no
label, no caption, solid flat plain white background, chunky cartoon mobile game art style,
thick bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft
two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_training)
round archery practice target with concentric red-white rings and an arrow stuck near the
center, single isolated icon only, one object centered in frame, no other icons, no grid, no
text, no label, no caption, solid flat plain white background, chunky cartoon mobile game art
style, thick bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft
two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_party)
two round astronaut-helmet silhouettes side by side with glowing oval eyes, a small blue "+"
symbol between them, single isolated icon only, one object centered in frame, no other icons,
no grid, no text, no label, no caption, solid flat plain white background, chunky cartoon
mobile game art style, thick bold dark outlines, flat saturated vibrant colors, clean
vector-like shapes, soft two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_botmatch)
simple robot head with two square glowing cyan eyes and a small antenna on top, single
isolated icon only, one object centered in frame, no other icons, no grid, no text, no label,
no caption, solid flat plain white background, chunky cartoon mobile game art style, thick
bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft two-tone cel
shading, high-quality 2D game icon, no watermark

(btn_icon_wardrobe)
a coat hanger with a small folded blue-gold cape/costume draped over it, single isolated icon
only, one object centered in frame, no other icons, no grid, no text, no label, no caption,
solid flat plain white background, chunky cartoon mobile game art style, thick bold dark
outlines, flat saturated vibrant colors, clean vector-like shapes, soft two-tone cel shading,
high-quality 2D game icon, no watermark

(btn_icon_shop)
a space-themed shopping bag with a small planet and stars printed on it and a gold coin icon
on the front, single isolated icon only, one object centered in frame, no other icons, no
grid, no text, no label, no caption, solid flat plain white background, chunky cartoon mobile
game art style, thick bold dark outlines, flat saturated vibrant colors, clean vector-like
shapes, soft two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_social)
two overlapping chat speech-bubbles, one containing a small green "+" symbol for add-friend,
single isolated icon only, one object centered in frame, no other icons, no grid, no text, no
label, no caption, solid flat plain white background, chunky cartoon mobile game art style,
thick bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft
two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_quests)
a rolled parchment scroll/checklist with one green checkmark line and one empty checkbox line,
single isolated icon only, one object centered in frame, no other icons, no grid, no text, no
label, no caption, solid flat plain white background, chunky cartoon mobile game art style,
thick bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft
two-tone cel shading, high-quality 2D game icon, no watermark

(btn_icon_play)
a bold solid green play triangle pointing right, slightly rounded corners, single isolated
icon only, one object centered in frame, no other icons, no grid, no text, no label, no
caption, solid flat plain white background, chunky cartoon mobile game art style, thick bold
dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft two-tone cel
shading, high-quality 2D game icon, no watermark

(btn_icon_edit_avatar)
a simple wooden pencil tilted at a 45-degree angle with a small sparkle near the tip, single
isolated icon only, one object centered in frame, no other icons, no grid, no text, no label,
no caption, solid flat plain white background, chunky cartoon mobile game art style, thick
bold dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft two-tone cel
shading, high-quality 2D game icon, no watermark

(btn_icon_close)
a bold rounded red X mark with thick even stroke weight, single isolated icon only, one object
centered in frame, no other icons, no grid, no text, no label, no caption, solid flat plain
white background, chunky cartoon mobile game art style, thick bold dark outlines, flat
saturated vibrant colors, clean vector-like shapes, soft two-tone cel shading, high-quality 2D
game icon, no watermark

(btn_icon_buy)
a single shiny gold coin with a small dark "+" badge at its top-right corner, single isolated
icon only, one object centered in frame, no other icons, no grid, no text, no label, no
caption, solid flat plain white background, chunky cartoon mobile game art style, thick bold
dark outlines, flat saturated vibrant colors, clean vector-like shapes, soft two-tone cel
shading, high-quality 2D game icon, no watermark
```

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
2. **6 base weapon sprites + 2 held orb props** (Section 6.1–6.2) — the final in-hand weapon art
   used directly in every match (no weapon costumes/skins exist in the current system).
3. **Weapon/ability HUD icons** (Section 6) — on screen in every match, the current ones are random.
4. **Projectiles & impact VFX** (Section 6.3) — currently the biggest true gap: 4 mismatched
   placeholders and no dedicated bomb/teleport/muzzle-flash/impact/explosion art at all; every shot
   fired in a match uses these.
5. **Navigation & action button icons** (Section 8.1) — replaces the single-letter drawer
   placeholders (`"S"`, `"L"`, `"A"`...) and adds icons to the currently text-only Wardrobe/Shop/
   Social/Quests buttons; visible on every screen of the app.
6. **16 avatars** (Section 5) — the code side is ready, only the art is missing; can be produced
   without a reference, independent/parallel work.
7. **Costumes** (Section 4) — only 15 total, generate all directly: 5× Rookie tier first (Common,
   what every new player sees immediately) → 5× Legend tier (showcase/aspirational) → 5× Star tier.
8. **Planet variety** (Section 7) — 2-3 new themes.
9. **App icon + store artwork** (Section 8) — mandatory for Play Console registration, but can't be
   done before the mascot is finalized.
