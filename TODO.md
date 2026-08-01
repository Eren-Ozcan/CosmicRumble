# CosmicRumble — Backlog

Deferred work identified during the economy/achievement audit and fix pass. Not started unless noted.

## RELEASE ROADMAP — everything remaining until the end of the project (general review, 2026-07-09)

Derived by scanning the entire codebase + this backlog + the master spec. The core game, online
multiplayer, economy, achievements, trophies/leaderboard, cloud save, audio, mobile input, Brawl Stars UI
and the social system + fullscreen login are all finished — the project is in the "release preparation"
phase.
**Critical path:** the Play Console closed-testing requirement (item 4) is the schedule bottleneck — it
must be started first; items 1-3 should be closed while waiting; costumes (9) + avatar icons (13) should
run in parallel as art work (both are complete on the data/code side, only waiting on real art).
Item 5 (Gem pricing) is not code but a BUSINESS DECISION — the user will make it. Item 21 (localization)
is now fully done (2026-07-10) — including the CJK font; all that remains is translating the 150 costume
names into the other 6 languages.

### 1. Continuation of open work (up next)
1. Google sign-in Console/Dashboard setup — the 7 steps in the "Google Play Games SIGN-IN" section
   below. The code is ready and waiting.
2. Two-device end-to-end test of the friend/invite flow (send/accept request, presence, invite →
   private match → match end). Verified one-sided; the two-sided flow has never been tested.
3. First real Android device build test — the new sign-in flow, real store IAP behavior,
   performance. So far the project has only run in the Editor + Device Simulator.

### 2. Mandatory before release — store/account work (not code, long lead time, start in parallel)
4. Google Play Console: app registration, the **12 test users × 14 days closed-testing requirement**
   (for new individual accounts — this determines the release schedule), Data Safety form, content
   rating, store artwork/description, AAB signing.
5. Real IAP SKUs: `gem_pack_100..6000` in the Console with exactly the same IDs; prices/tiers are
   placeholders — pricing has not been decided as a business decision.
6. Achievement ID mapping: **code side done (2026-07-11)** — `AchievementDefinition` now carries
   `steamId`/`googlePlayId`/`gameCenterId` fields (falls back to `achievementId` when empty), and
   `AchievementManager.ResolvePlatformId()` picks the right ID for the active provider and sends that
   to it. **Remaining: data entry only** — the 50 achievements must be created in the relevant
   Consoles and the opaque IDs they generate (`CgkI...` etc.) typed one by one into those three fields
   from the Inspector; no code change will be needed.
7. Legal: **draft text + code infrastructure done (2026-07-11)** — `legal/PRIVACY_POLICY.md` and
   `legal/TERMS_OF_SERVICE.md` (written based on the UGS systems actually active in the code) were
   added, and **must not be published without passing legal review** (the KVKK/GDPR clauses and the
   limitation-of-liability `{{...}}` placeholders are awaiting a lawyer's approval). "Privacy Policy"/
   "Terms of Service" links visible on every tab of the settings panel (`MainMenuUI`) were added
   (via `Application.OpenURL` through `Assets/Scripts/Utilities/LegalLinks.cs`). **Remaining**: the
   texts must pass legal review and be hosted at a real URL, then the placeholder URLs in
   `LegalLinks.cs` must be replaced with the real address; once the age rating is set in the Console,
   clause 5 (children's privacy) must be filled in.
8. iOS track (after Android): Apple Developer account, Mac/build pipeline, filling in the
   `AppleGameCenterAuthProvider` stub (Apple.GameKit +
   `SignInWithAppleGameCenterAsync`), App Privacy label, TestFlight. Right now there is nothing for iOS.
   **Attempted, and a hard platform blocker was found (2026-07-11)**: adding Apple.GameKit to Package
   Manager via its git URL (`https://github.com/apple/unityplugins.git?path=/plug-ins/Apple.GameKit`)
   failed cleanly with a "package.json not found" error (no residue left in the project, the build
   stayed clean) — the official Apple repository is not a direct UPM git package. Per Apple's own
   Quickstart documentation, the native (Xcode-requiring) libraries must first be compiled with
   `python3 build.py` to produce a `.tgz`, and the package added to Unity via "Add package from
   tarball" — and that build step **only runs on macOS**. So this is not merely an Apple Developer
   account issue: it cannot be installed on Windows at all, and there is no other route to try
   without a Mac/Xcode.

### 3. Missing game content (in the spec, not started)
9. 150 costumes (master spec Section 4): **data side done** (2026-07-09) — 150 `CostumeDefinition` +
   `CostumeDatabase` were generated, `CostumeManager` was bootstrapped, and the WARDROBE panel (owned
   items only) works and was play-tested. **Remaining: there is no real art**; all costumes are shown
   in the UI with a rarity-colored circle + initial placeholder. The largest remaining content item —
   can be shrunk with a per-tier template + color variations.
10. Map/planet variety: a single gameplay scene (SampleScene); `LobbyData.MapName` is unused.
    At least 2-3 different planet layouts (multi-planet scenes are the showcase for the gravity).
11. **Done (2026-07-10)** — Tutorial/onboarding: `Assets/Scripts/Tutorial/TutorialManager.cs`
    (new). During the first offline match played on this device (hotseat or Training — the local
    character spawned by `GameInitializer`) it shows 3 tip cards in sequence ("Move with A/D", "Jump
    with SPACE", "Pick a weapon, aim with the mouse, fire"), 4.5s each, skippable with ✕, and closes
    automatically after the last card. One-time only: `PlayerPrefs["cr_tutorial_seen"]` is persistent,
    so it is never shown again. Not a fullscreen block — a small top-center card that doesn't cover
    the movement/fire controls and doesn't affect the turn timer. Deliberately not wired into the
    online (Quick Match/private match) flow — a player who gets there has already played at least one
    offline match. Play-tested: `cr_tutorial_seen` was cleared in the Editor and TRAINING was entered
    with a guest login; the 3 cards appeared in sequence in the correct language (Spanish, which was
    set at the time), closed automatically, the `PlayerPrefs` value was verified as 1, and there were
    no console errors.
12. Bot AI: **Training mode done** (2026-07-10) — a "TRAINING" button available to real players in
    the main menu ☰ drawer opens the Game scene directly with 2 completely passive bots (they never
    move or fire — see the "Training Mode" section). Bot-filling when Quick Match has no opponent has
    still not been done (separate, optional work).
13. Profile icons/avatars: **done** (2026-07-10) — the selectable 16-avatar system works and the top
    bar updates live. **Remaining: there is no real icon art** (see the "Profile Avatars" section) —
    added to the next work list; for now a color + initial placeholder.

### 4. Known rough edges / technical debt
14. SOCIAL category achievements: **8/10 working** (2026-07-10) — `SOSYAL_KELEBEK`,
    `HERKESE_MEYDAN`, `DUELLO_SAMPIYONU` (previous pass) + `INTIKAM`, `REKABETCI`, `KOZMIK_EKIP`,
    `BIR_NUMARA`, `KOZMIK_AVCI` (this pass) are wired up; see the "Social Achievements" section. Only
    `OGRETMEN` remains out of scope — it requires cross-client notification + a separate real
    two-process test environment; the rationale is in that same section.
15. The `ui_button_hover` clip: **done** (2026-07-10) — `UiKit.Hover()` was added, wired to all
    programmatic buttons (29/30, with 1 deliberate exception), and play-tested. See "ui_button_hover
    wiring".
16. Dead code: **done** (2026-07-10) — `AbilityController.cs` and `ObjectSpawnSkill.cs` were deleted.
    Neither had a single match anywhere in the codebase (script references) or in any
    `.unity`/`.prefab`/`.asset` file (GUID-based Component references) — they were not called/used
    from anywhere outside their own files (a leftover of the old architecture that managed `IAbility`
    via a central `List<MonoBehaviour>`; the current system runs each ability independently through
    its own script). After deletion, guest login + the main menu flow were verified to run without
    errors in Play Mode (no missing-script/missing-reference errors).
17. **Done (2026-07-10)** — UGS timeout message: `CloudSaveManager.IsUnavailable` was made public, and
    if `MainMenuUI.BootstrapSequence`'s init/pull times out, `LoadingScreenUI` briefly shows a
    localized "Playing offline" message (previously it silently skipped to the next step).
18. **Done (2026-07-10)** — Invite corner cases: `OnApplicationPause`/`OnApplicationQuit` were added to
    `FriendLobbyPanelUI` and `OnlineLobbyPanelUI` — if the app is backgrounded/closed while the
    host/client is still in the lobby waiting stage (before the match starts),
    `NetworkBootstrap.LeaveSessionAsync()` is called automatically and the UGS session is cleaned up.
    It doesn't affect mid-match, because those panels are already gone/inactive once the match has
    started.

### 5. Post-release / optional
19. **Done (2026-07-10)** — Crash reporting + analytics. `com.unity.services.cloud-diagnostics`
    1.0.12 and `com.unity.services.analytics` 5.1.1 were installed (6.3.0 was tried first and turned
    out incompatible with this Unity version [6000.1.17f1] — a `RuntimePlatform.Switch2` compile
    error, it requires Unity 6000.2+; downgraded to 5.1.1). Crash reporting requires no code at all —
    `enableCrashReportAPI: 1` was enabled in `ProjectSettings.asset` (Player Settings → Other Settings
    → Crash Report API), and the native `CrashReportHandler` + the Cloud Diagnostics package handle
    the rest automatically. For analytics, the new `Assets/Scripts/Analytics/AnalyticsManager.cs`
    calls `StartDataCollection()` once UGS Core is ready in `MainMenuUI.BootstrapSequence` (automatic
    session/engagement events are collected without needing a dashboard schema), and sends a
    `match_completed` custom event (won/ranked) at the end of every match from
    `TurnManager.FinishMatchLocally` — **for that custom event to actually be recorded, a schema with
    the same name must be defined in the UGS Dashboard** (the code is ready, the dashboard step is
    separate — the same pattern as the Achievement/Leaderboard setups). The SDK's own documentation
    describes `StartDataCollection()` as "confirming that user consent has been obtained or is not
    required" — it must not be distributed in real user builds before the privacy policy (roadmap
    item 7, which does NOT exist yet) goes live; it's fine for Editor/internal testing. Play-tested:
    it was verified in the Editor with a guest login that the `AnalyticsManager` singleton is created,
    that `AnalyticsService.Instance` returns a real user/session ID, and that the
    `RecordMatchCompleted` call runs without errors.
20. **Done (2026-07-10), except device testing** — Push notifications. NOT real server-triggered UGS
    Push Notifications — this game's economy is client-authoritative and the only data the reminders
    need (login streak, daily chest allowance) is already on the device, so a server trigger isn't
    required; it was done with **local (device-scheduled) notifications**, the standard mobile game
    pattern (`com.unity.mobile.notifications` 2.4.3 was installed). The new
    `Assets/Scripts/Notifications/LocalNotificationManager.cs` is bootstrapped in
    `MainMenuUI.EnsureCoreSingletons` and calls `NotificationCenter.Initialize`; on
    `OnApplicationPause(true)` (when the player backgrounds the app) it schedules a "Don't lose your
    streak!" notification ~20 hours out if `LoginStreakManager.GetCurrentStreak() > 0`, and a "Chests
    are waiting for you!" notification ~4 hours out if `ChestManager.GetRemainingChests() > 0`; on
    `OnApplicationPause(false)` (when they come back) both are cancelled. All SDK calls are guarded
    with `#if UNITY_ANDROID || UNITY_IOS` — the package's unified API assembly
    (`Unity.Notifications.Unified`) only compiles for Android/iOS/Editor and leaves out Windows
    Standalone (the DevClient build used for online testing), so the same guarded-compilation pattern
    as `STEAMWORKS_INSTALLED`/`GPGS_INSTALLED` was used.
    **Device/platform testing could not be done**: the Editor is currently on the StandaloneWindows64
    build target (`UNITY_ANDROID` was never defined in this session), so the real notification
    scheduling/triggering behavior can only be verified once the build target is switched to Android
    and tried on a real device/emulator — just like the GPGS/Steamworks integrations. The build
    (Editor, on the current Standalone target) is clean.
21. Localization: **fully done** (2026-07-10) — English default + TR/ZH/ES/JA/KO/DE for 7 languages,
    all UI panels + achievement/quest data translated, a language picker in Settings, and the CJK font
    (Noto Sans SC/JP/KR) installed and play-tested. **Remaining: only** the 150 costume names are
    still English-only (not translated into the other 6 languages, low priority).
22. Server-side validation: economy/CloudSave are client-authoritative (a cheating vector); IAP
    receipt validation + Cloud Code for critical operations — a priority once revenue starts.
23. Steam: deliberately frozen (`STEAMWORKS_INSTALLED` is ready) — App ID + Steamworks setup if it
    gets greenlit.
24. Growth ideas (outside the spec): 2v2/4 players, seasonal league resets, battle pass.

## Costume Redesign — 5 characters × 3 tiers = 15 costumes (2026-07-16, 2nd pass)
User decision: the 150 costumes were reduced to 15 — 5 characters, each with 3 tiers
(Standard/Advanced/Elite). Weapon costumes were removed entirely. Character names are GENERIC FOR NOW
("Character 1..5") — the real names/themes will be assigned while the costume art is designed (just an
asset field, no code change needed). 8 atomic commits, play-tested live in the Editor.

- **Data**: `CostumeDefinition.characterId` (1-5) was added; `CostumeAssetGenerator` was rewritten
  (it generates 15 and deletes the old set itself); the 150 old assets were deleted and c1_1..c5_3
  generated.
- **Distribution** (the user chose "include Gold/Gem + a purchase UI"): 5× Default Standard;
  the Advanced tier is ByGold 800 (c1_2) / ByChest (c2_2, c5_2 — deliberately Uncommon, since the
  chest filter can only pick Common/Uncommon) / ByLevel 10 (c3_2) / ByGold 1200 (c4_2); the Elite tier
  is ByLevel 20 (c1_3) / ByGem 50 (c2_3) / ByAchievement EFSANE (c3_3, Legendary) /
  ByGem 80 (c4_3) / ByLevel 35 (c5_3). This makes ALL 15 costumes actually obtainable today.
- **Achievement cleanup**: 12 achievements had a `rewardCostumeId` pointing at deleted ids
  (they would have been a silent no-op) — EFSANE was rewired to c3_3 and the remaining 11 were cleared.
- **ByLevel costume auto-grant**: `CostumeManager` now does a catch-up scan on `OnLevelUp` + in Start
  (the UnlockManager pattern) — previously ByLevel costumes could never be granted at all.
- **Wardrobe rewritten**: instead of CHARACTER/WEAPON tabs, 5 character columns × 3 tiers;
  locked costumes are now VISIBLE — Gold/Gem ones get a price pill + direct purchase
  (`TryPurchase`, disabled when the balance is insufficient, refreshed via `OnCurrencyChanged`), while
  Level/Chest/Achievement ones get a condition label. A layout bug was found and fixed: a
  `childControl=false` layout group ignores `LayoutElement` and reads the RectTransform size — the
  cells had stayed at the 100×100 default, so sizeDelta was set explicitly.
- **Loc**: the 15 costume names + condition labels in 7 languages — the "150 costume names not
  translated" backlog item became moot (the new set ships fully translated).
- **Play-test (Editor, guest Lv22 profile)**: the 15 costumes were listed with the correct data;
  defaults (5) + ByLevel catch-up (c3_2, c1_3) = 7 owned at startup; the c1_2 purchase deducted −800
  Gold and equipping worked; c2_3 (50 Gem, insufficient balance) was disabled; the counter read 8/15;
  the console showed only the known harmless errors (NGO stop-play cleanup, Coplay screenshot artifact).
- **Remaining**: the costume sprites are still placeholders (color+letter); the equipped costume is
  still not reflected in the in-game character appearance — both will be done as a single piece of work
  together with the costume art (only 15 images are needed now, not 150). The c001/c002 ids in old
  player saves are harmless (IsOwned keeps them in the list, they aren't found in the db, and no code
  path breaks).

### Theme/Concept Decision — "Galactic Show Arena" (2026-07-30)
Genre market research was done with the user (Worms-style artillery games, the Brawl Stars skin
economy, 2026 mobile game trends) and a theme was picked for the generic "Character 1..5". NO CODE
CHANGES WERE MADE — this is purely a reference decision to be used once the art/naming pass begins.

- **Concept**: "Galactic Rumble Show" — an interplanetary arena show broadcast on television. The
  characters are the show's famous contestants, the weapons are part of the stage spectacle
  (RPG=rocket show, BlackHole=finale effect), and the destroyed planets are the set decoration. Tone:
  consistent with Brawl Stars' colorful/fun/spectacle-first feel (which matches the UI design
  preference, see memory `ui-design-preferences`).
- **Confirmed that the 5 characters are cosmetic-only** (`CostumeDefinition.characterId` is purely a
  visual skin line — it is not tied to weapon/ability selection; every player unlocks all 9 weapons by
  level). So the character identities were designed as personas/visual motifs and not locked to a
  specific weapon. Different names were chosen for the characters so they don't clash with the existing
  16 cosmic avatar names (Nova, Pulsar, Comet... — the profile icon system, which is separate); the
  name Nova is deliberately present in both systems — it's the main mascot character, for brand
  consistency:
  1. **Nova** — charismatic show host/mascot (bright, fiery, gold/red)
  2. **Blitz** — fast/energetic acrobat (neon blue, electric effects)
  3. **Titan** — heavy/armored show of force (metallic gray, coarse lines)
  4. **Scope** — cool-headed sharpshooter (minimal, technical, dark green)
  5. **Vex** — mysterious master of control (purple/black, black hole motifs)
- **Tier names** (they map exactly onto the existing Standard/Advanced/Elite unlock logic and require
  no code change — just `displayName`/loc strings): **Rookie → Star → Legend**.
- **Deferred alternatives** (the user did not reject them; recorded for the record): Space Outlaws/
  Bounty Hunters (a grittier/more cinematic tone — in tension with the current colorful UI goal),
  Star Species/alien faction roster (lore-heavy, higher production cost — 5 completely different
  species sprites), Cosmic Sports League (jersey-style costume production is cheap but more
  generic/less distinctive).
- **Next step**: this name/persona decision will be used as the base reference once work moves to
  `CostumeAssetGenerator`/`CostumeDatabase` (item 9) and to real sprite production (the suspended art
  generation problem in item 9) — not implemented yet.

## System Wiring Pass — the progression/economy chain (2026-07-16)
Disconnects found during a "deviation from the core idea / logic error" review: the game's three
progression systems were complete on the data side but had never been wired into gameplay. Fixed in
this pass (all play-tested live in the Editor, as separate atomic commits):

1. **Level now unlocks weapons/skills** (previously a Lv1 player could use all 10 weapons —
   `UnlockManager` was processing unlocks but nothing was reading them): the new
   `AbilitySlotCatalog` (slot ↔ itemId mapping, fail-open if `UnlockManager` is absent — the gate is
   disabled if the Game scene is opened directly in the Editor), the gate sits at the single
   selection chokepoint (`CharacterAbilities.SelectSkill/ConfirmSkill` — covering both keyboard and
   touch), and a locked slot is drawn dark in the UI with a "LvN" label instead of the ammo counter.
   Online it only restricts the local player's own input (the economy is already client-authoritative,
   item 22).
   Play-test: on a Lv22 profile the unlock list was trimmed in memory and it was verified that in a
   training match the locked slots were drawn with Lv2/6/8/10 labels and that `SelectSkill` rejected
   them.
2. **`UnlockManager` level catch-up scan**: `OnLevelUp` only ran on a live increase — a level arriving
   via cloud restore (or levels earned while UnlockManager didn't exist) never produced any unlocks.
   In `Start()` all ByLevel items up to the current level are now scanned once. (This scan also lets
   unlocks that were stripped from memory during testing repair themselves.)
3. **The first spending path in the economy** (previously no UI called `CurrencyManager.Spend` at all —
   Gold accumulated forever and there wasn't even a single flow where IAP-purchased Gem could be
   spent, which was also an outright trap from a store-review perspective): a CHESTS strip in the shop —
   Rare chest 800 Gold, Epic chest 25 Gem (`ChestManager.TryPurchaseChest`, prices in `ChestConfig`;
   completely independent of the daily win-chest limit). The button is disabled when the balance is
   insufficient; the reward drops via the existing `RewardPopupManager` toast. Play-test: both
   purchases were verified with real balance changes (Rare: −800 Gold; Epic: −25 Gem; the daily counter
   stayed at 0).
4. Minor rough edge: unlimited ammo (Pistol, -1) now shows "∞" in the tray instead of "-1".

**Deliberately deferred (together with the costume design, user decision)**: the acquisition flow for
ByLevel/ByGold/ByGem costumes (no UI calls `CostumeManager.TryPurchase`, ByLevel costumes aren't
granted) and reflecting the equipped costume in the character/weapon appearance (no in-game code reads
`GetEquipped`) — these will be handled as a single piece of work while the costume sprites are being
produced. Multi-planet scenes also remain separate work (item 10 — SampleScene has 1 planet, so the
`YÖRÜNGE` achievement is impossible on the current map).

## Security/Bug Audit — Full Pass (2026-07-15)
The entire codebase (136 scripts) was scanned for security holes/bugs/missing behavior; EVERYTHING
found was fixed and committed as 15 atomic commits. **The build/play-test debt was closed (2026-07-16)**:
the build is clean, and guest login + main menu + a training match + the shop all ran without errors in
the Editor.
Bomb.prefab's `GlobalObjectIdHash` is still 0 in the file, but that's the same pattern as the other
working projectile prefabs (NGO generates it at runtime); a real two-client online firing test of Bomb
still hasn't been done (together with the two-device test item, roadmap item 2).

Fixed (in commit order):
1. `movementLocked` permanent lock: the timer expiring while a projectile was airborne, via Tab, or
   with a confirmed-but-unfired weapon paralyzed the character until the end of the match — it is now
   released unconditionally on turn transition.
2. Cloud Save ↔ device-bound HMAC conflict: a currency.json arriving on a new device was mistaken for
   "tampering", reset, and the zero written back to the cloud — the signature was made device-independent
   (`SaveIntegrity`), and old signatures are accepted once and re-signed.
3. The trophy cache is now signed (device-bound HMAC) — inflating trophies via regedit and submitting
   them to the leaderboard was closed off (real authority is still a Cloud Code job, item 22).
4. A server-side speed clamp (`ClampFireVelocity`) on 6 weapons' Fire RPCs — a modified client cannot
   fire with unlimited power.
5. **Bomb** was the missing 10th weapon in the security pass: ServerRpc/ServerTryConsume(slot 9)/
   NetworkObject.Spawn were added, along with the prefab network components + DefaultNetworkPrefabs
   registration.
6. Local `Destroy` on spawned NetworkObjects on the client (an NGO error + desync) —
   `NetworkPhysicsGuard.DespawnOrDestroy` (hide the visual on the client, wait for the server's
   despawn); `ProjectileBase.OnDestroy→SettleOnce` was added (DeathBoundary destruction was leaking the
   turn counter).
7. Planet destruction is server-authoritative + synchronized: holes are now opened with the same
   pos/radius on every machine via `TurnManager.PlanetExplosionClientRpc` (the divergence is gone).
8. Fire sound on every machine + weapon-usage achievement credit on the shooter's machine
   (`AbilityBase.AnnounceFire`); the rocket/grenade flight loop now plays on client copies too.
9. The death effect plays on every machine via ClientRpc; `Die()` no longer disables NGO sync
   components.
10. Online player names/tags: `GravityBody.playerName` (owner-write NetworkVariable) — the real name
    instead of "Player_1 Wins!", and the name tag + team color are now set up online as well.
11. Reconnect identity verification: an orphaned character is only handed over to someone returning
    with the same UGS PlayerId (`NetworkIdentityRegistry` + `TurnManager.SubmitIdentityServerRpc`).
12. The online client HUD came alive: the turn counter is replicated via NetworkVariable; the skill
    panel binds to ITS OWN character on each machine (the mobile client couldn't select weapons); a
    RequestEndTurn RPC for passing the turn + a programmatic SKIP button in TurnTimerUI (the host can no
    longer skip the opponent's turn with Tab).
13. `PlanetClickExploder` (an unguarded debug cheat tool that wasn't attached anywhere) was deleted.
14. IAP: validation silently staying disabled when the validator can't be constructed is now visible
    via an error log in release builds too.
15. The match-end "{0} Wins!"/"Draw!"/"+{0} Gold" strings were wired to Loc.T (6 languages).

Known remaining gaps (deliberately out of scope for this pass):
- The black hole ZONE visuals/GIF are missing on the client (the zone is built at runtime on the
  server; the pull force already works correctly via the `GravityBody.ApplyForce` routing — only the
  visual is missing).
- The hit/miss (accuracy) statistic is still machine-local online: `FireShotFired` is fired from each
  machine's own local simulation; replicating the shooter's identity to the projectile is required
  (separate work).
- Economy/CloudSave are still client-authoritative (item 22, the Cloud Code plan is unchanged).

## Costumes
Done (2026-07-09) — GARDIROP (Wardrobe) panel added, `CostumeManager` bootstrapped, 150-costume data
generated. Data-complete; still needs real art (see below).

- **`Assets/Scripts/UI/WardrobePanelUI.cs` (new)**: the new WARDROBE button in the main menu (on the left
  rail, above SHOP) → a panel of owned costumes. CHARACTER/WEAPON tabs, grid cards with rarity-colored
  frames (an initial-letter badge fallback when there is no sprite — `previewSprite` is null-safe), and
  tapping a card calls `CostumeManager.Equip()` and updates the EQUIPPED label. **Only owned costumes are
  listed** — nothing locked/unpurchased ever appears (the shop/unlock flow is out of scope, separate work).
  It follows the same programmatic UiKit Canvas pattern as `QuestsPanelUI`.
- **`CostumeManager` is now bootstrapped** (`MainMenuUI.EnsureProgressSingletons()`) — it had previously
  been deliberately excluded (see the old note), and was enabled together with item 1 of the TODO.md
  roadmap. `GrantDefaultCostumes()` was added to `Awake()`: costumes with `CostumeUnlock.Default` (e.g.
  Gray Soldier, Standard Blue) are granted to the player silently (without triggering the reward popup)
  from the start — otherwise the wardrobe would always look empty, because nothing was automatically
  granting the "default" costumes.
- **The 150-costume data was generated**: the `CostumeAssetGenerator.cs`
  (`CosmicRumble/Economy/Generate Costume Assets`) Editor menu command was run —
  `Assets/Resources/Costumes/*.asset` (150 `CostumeDefinition`) and
  `Assets/Resources/Economy/CostumeDatabase.asset` are now permanent in the project. The rarity
  distribution matches the master spec (Common/Uncommon/Rare/Epic/Legendary) and the unlock methods are
  mixed (Default/ByLevel/ByGold/ByGem/ByChest/ByAchievement). **Still missing: no costume has real art**
  (`previewSprite` is null on all of them) — the UI compensates with a rarity-colored circle + initial,
  but the real character/weapon art remains a separate, large art job (see roadmap item 9).
- **Play-tested end-to-end in the Unity Editor via Coplay MCP**: booted with a guest login, opened the
  WARDROBE panel, and verified that the CHARACTER tab showed only the 2 owned costumes ("Gray Soldier",
  "Standard Blue", both Common) and that the "Owned: 2 / 86" counter was correct — none of the 84 locked
  costumes appeared in the list.
- **One real bug was found and fixed in this pass**: `WardrobePanelUI.Show()` initially called
  `Populate()` BEFORE `_panelRoot.SetActive(true)`. On `TextMeshProUGUI` objects created while the panel
  was still inactive, `UiKit.BrawlText()`'s `outlineWidth` setter tries to create a font material
  instance, which requires TMP's `OnEnable()` to have run — and since `OnEnable` is deferred in an
  inactive hierarchy it threw a `NullReferenceException` (Material.CreateWithMaterial, source null), so
  the panel blew up the first time it was opened. The order was changed (`SetActive(true)` first,
  `Populate()` second) — verified to run without errors after the fix.
- **Costume art generation was attempted and suspended (2026-07-11).** The routes tried, in order:
  1. Coplay MCP `generate_or_edit_images` → **401 Unauthorized**. Checked in the Coplay panel (Coplay
     menu → Toggle Window → Model Selection): the account is signed in but the balance is **$0.0000** —
     Coplay's AI generation features (image/audio/3D, all of them) are pay-per-use and there are no
     credits. "Nano Banana Pro" (Google Gemini's image model) is selected as the image generation model —
     i.e. Gemini is already what runs behind Coplay, just through Coplay's own billing.
  2. Free third-party ready-made assets (Kenney.nl, CC0 — the source the project already uses for
     audio/fonts) were investigated: the "Game Icons" pack looked suitable for weapons, and the "Toon
     Characters" pack was downloaded and reviewed for characters (6 archetypes: Female/Male adventurer,
     Female/Male person, Robot, Zombie — the style is close to the game's `player_15.png` but not an
     exact match, and there are only 6 archetypes for 10 themes). By the user's decision this route was
     **abandoned**.
  3. It was proposed that the user generate the art with their own Gemini access (directly, outside
     Coplay) and import it into the project as assets — the user **suspended the work for now** and it
     did not proceed.
  **For the next session**: the costume/avatar art is still entirely missing (`previewSprite` is null). If
  the user loads credits into Coplay, route 1 can be tried directly; if not, route 3 (the user generating
  with their own Gemini and delivering PNGs) is the fastest path — in that case a 10-character + 10-weapon
  theme template is enough (it can be distributed across the 150 costumes by deriving colors from the
  names and automating the tinting; the plan is ready but not implemented).

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

## Training Mode
Done (2026-07-10) — a practice mode available to real players and directly accessible. The old "BOT
MATCH (DEV)" entry stays locked to the Editor (a developer test tool with a selectable bot count); this
NEW entry is separate and works in every build.

- **`LobbyData.IsTraining`** (new flag) + **`TurnManager.isTrainingMode`**: in training the bots are
  NEVER added to the `TurnManager.characters` rotation. `GravityBody.isActive` defaults to
  `false` (`NetworkVariable<bool>(false, ...)`) and only `TurnManager.ActivateCharacter()` sets it to
  `true` — since that never happens for a character that never enters the rotation, the bots stay
  permanently passive by design (and no "prevent firing" code was WRITTEN; the existing input-gate
  pattern is sufficient). `TurnManager.CheckGameOver()` normally ends the match (declaring a winner)
  when `characters.Count <= 1` — since in training the human is the only one registered, the match would
  end on the first frame if this weren't bypassed; the `isTrainingMode` flag skips that check.
- **`MainMenuUI`**: a new "TRAINING" entry in the ☰ drawer (`dw_training`,
  `StartTrainingMatch()`) — it sets `LobbyData.IsTraining=true`, `BotCount=2` and goes straight to the
  Game scene without a lobby screen (one-click practice, unlike `LobbyPanelUI`).
- **Play-tested end-to-end in the Unity Editor**: reached the main menu with a guest login and clicked
  TRAINING; the Game scene opened with the human (`Pulsar630`) + `Bot_1` + `Bot_2`, `GameOverPanel`
  stayed inactive (the match didn't end — the `isTrainingMode` bypass was verified), and after ~15
  seconds `Bot_1`'s position measured exactly the same (x=18.2469711, y=-10.5347147 → unchanged, the bot
  never moved). No errors/warnings.
- Exiting training uses the existing "Return to Main Menu" in the `InGameMenu` ESC menu — no separate
  exit logic was written, the existing path is reused.
- No rewards/XP/Gold/achievements are granted (deliberate): since `TriggerGameOver` is never called, the
  match-completion events don't fire — this is consistent with the "training mode grants no progression"
  rule in other mobile games, and it also required no special restriction code (it came for free as a
  side effect).

## Profile Avatars
Done (2026-07-10) — the same pattern as the costume system (`Assets/Scripts/Economy/Avatars/`), but
simpler: unlike costumes, all avatars are unlocked from the start (no unlock/rarity), and only "which one
is selected" persists. **There is no real icon art** — it was added to the DO LATER list together with
the 150 costume/avatar sprites (below); for now a color + initial placeholder (`AvatarDefinition.icon` is
null-safe and the UI already prioritizes the icon — it switches over automatically once a sprite is
added, no code change needed).

- **`AvatarDefinition`/`AvatarDatabase`/`AvatarManager`** (exactly the same pattern as the costume
  trio): `AvatarManager.Select(id)` saves the selection to `avatar.json` (like CostumeManager's
  `costumes.json`) and raises an `OnAvatarChanged` event.
- **`Assets/Editor/AvatarAssetGenerator.cs`**: 16 space-themed avatars (Nova, Comet, Blaze, Nebula,
  Pulsar, Quasar, Meteor, Orbit, Solstice, Eclipse, Vortex, Cosmos, Photon, Asteroid, Aurora,
  Zenith), each in its own placeholder color — the small-scale equivalent of `CostumeAssetGenerator.cs`.
- **`AvatarPickerUI.cs`**: the same grid pattern as `WardrobePanelUI`/`QuestsPanelUI`, a 4-column grid,
  with the selected avatar marked by a green outline + a "SELECTED" label.
- **Top bar integration**: the avatar circle on the profile plate in `MainMenuUI` now shows the selected
  avatar's color/letter instead of the first letter of the player's name; a small "+" badge with its own
  Button/raycast target was added to the corner of the circle (the rest of the plate still opens the
  Leaderboard, and only that small area calls `AvatarPickerUI.Show()`) — two different actions coexist on
  the same plate without any risk of overlapping click regions.
- **Live updating wired up properly**: initially the top bar was only built once during `BuildUI()` and
  didn't update when the selection changed until the menu was reopened — this was noticed and
  `MainMenuUI.OnAvatarChangedForTopBar` + `ApplyAvatarVisuals()` were added (subscribed to
  `AvatarManager.OnAvatarChanged`), so it now reflects the moment a selection is made (without a scene
  reload).
- **Play-tested end-to-end in the Unity Editor**: reached the menu with a guest login, verified the
  default avatar (Nova, red/pink "N") in the top bar, opened the avatar picker and selected "Meteor",
  verified the SELECTED badge moved from Nova to Meteor in the picker, and verified via
  `get_game_object_info` that the top bar's `Initial.text` and `Avatar.Image.color` matched Meteor's
  defined values exactly (`"M"`, `RGB(0.85, 0.25, 0.55)`) — live updating works.
  No errors/warnings (only the known harmless Coplay/NetworkManager artifacts).

## ui_button_hover wiring
Done (2026-07-10) — `UiKit.Hover(GameObject)` + `UiHoverSound` (new, `IPointerEnterHandler`, `Assets/Scripts/UI/UiKit.cs`)
were added and wired, next to `UiKit.Press()`, into every existing button creation site one by one
(29 GameObjects, 14 files). It stays silent on disabled (`Selectable.IsInteractable() == false`) buttons.

- Every file that creates buttons programmatically (`MainMenuUI`, `InGameMenu`, `WardrobePanelUI`,
  `SocialPanelUI`, `ShopPanelUI`, `QuestsPanelUI`, `OnlineLobbyPanelUI`, `LoginScreenUI`,
  `LoginPanelUI`, `LobbyPanelUI`, `LeaderboardPanelUI`, `InvitePopupUI`, `FriendLobbyPanelUI`,
  `AvatarPickerUI`) was reviewed — `UiKit.Hover(go)` was added to 29 of the 30 places that call
  `AddComponent<Button>()`. **The one deliberately skipped place**: the drawer background dismiss button
  (`dimGO`) in `MainMenuUI` — it's an invisible fullscreen click catcher, not a button where hover would
  be meaningful.
- **Test**: reached the main menu with a guest login in Play Mode, simulated pointer-enter on
  `btn_wardrobe` via `ExecuteEvents.Execute(..., pointerEnterHandler)`, and verified that the `isPlaying`
  state of `AudioManager`'s non-looping (SFX) `AudioSource` was `False` before the hover and `True`
  after — the clip really plays. No console errors.

## Social Achievements (review + missing event wiring)
Done (2026-07-10) — 3 of the 10 achievements in the SOCIAL category (`SOSYAL_KELEBEK`, `HERKESE_MEYDAN`,
`DUELLO_SAMPIYONU`) were made functional; the remaining 6 were deliberately left out of scope as separate,
larger work (each justified individually below).

- **A real bug was found and fixed**: `AchievementEvents.FirePlayerDefeated(string id)` was never called
  from anywhere in the entire codebase — the event and the `AchievementTracker.HandlePlayerDefeated`
  subscription looked "wired", but no code triggered it, so `SOSYAL_KELEBEK` was silently dead. Every time
  `CombatEventReporter.ReportHit()` is called, the target's `TakeDamage()` has already run beforehand (see
  the in-method comment) — exploiting that ordering, the killing blow is detected after the hit via a
  `ch.GetCurrentHealth() <= 0f` check and `FirePlayerDefeated` is triggered from there. Solved without
  going into a far larger refactor like carrying the attacker's identity through the whole projectile
  pipeline.
- **New event: `AchievementEvents.OnDamagedTarget`/`FireDamagedTarget(string id)`** — it publishes the
  target's identity on every hit (lethal or not). `HERKESE_MEYDAN` ("damage 7 different players in the
  same match") is now tracked via `AchievementTracker._matchDamagedTargets` (a `HashSet<string>` reset per
  match), and `UnlockAchievement` is called once it reaches 7. Clearing was added to `ResetMatchState()`.
  Since an invited private match uses exactly the same `CombatEventReporter`/`TurnManager` pipeline as
  Quick Match, there is no separate code path — the event source is independent of the match type.
- **`DUELLO_SAMPIYONU` ("10 wins in 1v1 mode") was wired**: inside
  `AchievementTracker.HandleMatchWon()` a `_matchPlayerCount == 2` check increments the cumulative
  `_totalDuelWins` counter and calls `UpdateProgress` — no separate "duel mode" event was needed, the
  existing `OnPlayerCountInMatch` data was sufficient.
- **Test**: all three scenarios were verified in Play Mode by firing the events directly from a temporary
  script (`Temp_TestSocialAchievements.cs`, deleted afterwards) — after 7 different `FireDamagedTarget`
  calls `HERKESE_MEYDAN` unlocked, after one `FirePlayerDefeated` the `SOSYAL_KELEBEK` progress went from
  0 to 1, and after a 2-player `FireMatchWon` the `DUELLO_SAMPIYONU` progress went from 0 to 1.
  No console errors/warnings.
- **5 of the remaining 6 SOCIAL achievements were wired (2026-07-10)** — only `OGRETMEN` stayed out of
  scope with a justification (below). The previous note (the old version of this line) had mixed up the
  `REKABETCI`/`OGRETMEN` rationales — corrected by checking the real `.asset` descriptions.
  - **`INTIKAM`** ("Defeat whoever killed you in the next match"): there was no need to carry the
    attacker's identity through the projectile pipeline — since the project is always 1v1, the answer to
    "who beat us" is already the match's only opponent. `AchievementEvents.FireMatchLost(string
    winnerName)` (previously parameterless) carries the winner's name from the lost match to
    `AchievementTracker`, where it's written to `PlayerPrefs["cr_intikam_target"]`; in the next match, if
    `HandlePlayerDefeated` matches the same name, `INTIKAM` unlocks and the target is cleared.
  - **`REKABETCI`** (its real description is "Finish top 3 in a ranked match" — nothing to do with the
    tutorial): since the project is strictly 1v1 (`MaxPlayers=2` in every SessionOptions), being one of 2
    players is always within "top 3" — rather than inventing a fake 3+-player ranking system, it unlocks
    directly whenever a ranked (Quick Match) match is completed, win or lose
    (`AchievementEvents.OnRankedMatchCompleted`, triggered from the `ranked` parameter added to
    `TurnManager.FinishMatchLocally`).
  - **`KOZMIK_EKIP`** ("Play 5 matches with the same 3 people" — "the same 3 people" is meaningless in
    1v1, so it was rescaled to "5 matches with the same friend"): `FriendLobbyPanelUI.ShowAsHost/ShowAsClient`
    now writes the friend's `PlayerId` into `LobbyData.FriendOpponentId` (for the client side, the
    `senderId` that `InvitePopupUI.HandleInvite` already receives was added to `ShowAsClient` as a new
    parameter); when the match ends, `TurnManager.FinishMatchLocally` reads it, fires
    `AchievementEvents.FireFriendMatchCompleted(friendId)` and clears the field (it's also cleared in
    `OnCancelClicked`/`CleanupOnBackground` on cancel/backgrounding — otherwise it would leak into the
    next, unrelated match). `AchievementTracker` keeps a separate `PlayerPrefs` counter per friend, and
    the progress is the highest of those counters (playing with a different friend doesn't lower it).
  - **`BIR_NUMARA`/`KOZMIK_AVCI`** (leaderboard rank): the old note said "no synchronous access", but
    the achievement check never needed to be synchronous — after the score is submitted,
    `LeaderboardManager.SubmitScoreAsync` learns the rank asynchronously via `FetchOwnEntryAsync()` (an
    already-existing method) and fires `AchievementEvents.FireLeaderboardRankKnown(rank)`;
    `AchievementTracker` unlocks `BIR_NUMARA` at rank==0 and `KOZMIK_AVCI` at rank<10.
  - **Test**: all 5 events were verified in Play Mode by firing them directly
    (`TestSocialAchievements.cs`, temporary, deleted afterwards) — `INTIKAM`/`REKABETCI`/`BIR_NUMARA`/
    `KOZMIK_AVCI` unlocked, and `KOZMIK_EKIP` progressed to 5/5 and unlocked. No new console errors (only
    the known harmless NGO stop-play cleanup errors).
  - **`OGRETMEN` out of scope (justified)**: its real description is "Guide a new player through the
    tutorial" — the mentor's own device needs to learn that the friend they invited completed their
    TutorialManager. That requires a cross-client notification carrying the local `PlayerPrefs` state from
    the mentee's device to the mentor's (it looks technically possible via FriendsService.MessageAsync,
    but a real two-sided flow requires a separate, genuine second OS process, as in the original
    multiplayer milestone — which couldn't be tested in this pass). Also, `TutorialManager` is
    deliberately only triggered in the offline (hotseat/Training) flow (see the tutorial note above the
    "Training Mode" section) and hasn't been wired into the online friend-invite flow yet — which is also
    separate work. Since it requires a whole subsystem of its own (cross-client notification) and a
    separate test environment (two real processes), it wasn't done in this pass.

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

## Google Play Games SIGN-IN — Console/Dashboard setup (not done, code is ready)

The code side of the 2026-07-08/09 social+auth overhaul (fullscreen login, the Platform + Cosmic ID
model, UGS Friends, the invite lobby) is completely finished and verified in the Editor. ALL of the
remaining steps for Google sign-in to actually work on Android require the user's own Google accounts —
no code changes are needed:

1. **Register the app in Play Console** (a draft is enough) + **set up Play Games Services**
   (Grow → Play Games Services → Configuration → "No, create a new game"; note the 12-digit
   game ID it gives you). This is the same registration as step 1 of the "Achievement platform
   providers" section above — do it once, it serves both.
2. **Android credential** (Credentials → Android): the package name + the SHA-1 of the keystore that
   signs the APK. If the SHA-1s of the Play App Signing key and the local test keystore differ, TWO
   separate Android credentials must be added (the most common stumbling block: on a SHA-1 mismatch,
   sign-in fails silently).
3. **Game server credential** (Credentials → Game server): create a **Web application** type OAuth
   client in the Google Cloud Console (NOT the Android type; the first time you'll also fill in the
   OAuth consent screen — if it's in Test mode, your own Gmail must be added to the test users). Copy
   the resulting **Client ID + Client Secret**.
4. **Unity Dashboard** → Player Authentication → Identity Providers → **Google Play Games** →
   enter the Web Client ID + Secret from step 3 → Enable. (The only step on the Unity side; Unity
   generates no credentials, you just tell it what Google gave you.)
5. **Play Games Services → Testers**: until PGS is published, only people on that list can sign in —
   add your own account.
6. **Once in the Unity Editor**: Window → Google Play Games → Setup → Android Setup — the Play
   Console "Get resources" XML + the WEB Client ID from step 3 (the same wizard as step 3 of the
   achievements section above; both are done in one go).
7. **Device test**: a build signed with the keystore from step 2 → expected flow: silent Google
   sign-in at launch → loading → menu; "GOOGLE — Connected (name)" under Settings → Account.

Ready and waiting on the code side: `GooglePlayAuthProvider` (silent/interactive auth code),
`AuthManager.SignInWithPlatformAsync` (Link/SignIn + AccountAlreadyLinked account switching), and the
"CONTINUE WITH GOOGLE" button in LoginScreenUI (`UNITY_ANDROID && GPGS_INSTALLED`).

Also deferred: **iOS Game Center sign-in** — `AppleGameCenterAuthProvider` remains a stub
(IsAvailable=false); it will be filled in with the Apple.GameKit plugin + `SignInWithAppleGameCenterAsync`
once the iOS track is set up. The **friend/invite end-to-end test** also requires two real identities
(testuser1 in the Editor + a second Cosmic ID on a second device/build) — it was verified one-sided in
the Editor (Friends init + its own code "Pulsar630#51647" came back from UGS); the two-sided flow awaits
device testing.

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
generated by clicking "CREATE HOST" in the Editor, fed to the standalone build, which called the real
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
  `OnlineLobbyPanelUI` gained a working "CANCEL" button on the Host card (visible while waiting for
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
Requested explicitly (quick match is the core of the game and absolutely had to exist). Superseded the originally-planned "matchmaking pools (Steam/mobile split)"
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

## Gravity fixes + Online Leaderboard (trophy system) — Done (2026-07-06)

### Gravity — the bug where projectiles weren't pulled toward the planet (root cause + fix, verified live)
- **Root cause:** `NetworkRigidbody2D.Awake()` on `Projectile.prefab` (the base for the Pistol/RPG/Grenade
  projectiles), `PF_BlackHoleProjectile.prefab` and `TeleportOrbProjectile.prefab` unconditionally forces
  the body to Kinematic, and only `OnNetworkSpawn()` undoes it — since spawning never happens offline, the
  projectiles stayed permanently Kinematic, `GravitySource`'s `AddForce` pull became a silent no-op, and
  the projectiles flew perfectly straight. This is exactly the same regression found for characters in
  Milestone 3 (see `GravityBody.Start()`); the projectile side had been missed at the time. **Fix:**
  `Assets/Scripts/Utilities/NetworkPhysicsGuard.cs` (`EnsureDynamicWhenNotSpawned`) + calls in the
  `Init()`/`Start()` paths of every projectile script (KineticProjectile, Projectile,
  HandGrenadeProjectile, BlackHoleProjectile, TeleportOrbProjectile, ProjectileBase). Since the weapons'
  SpawnAndInit order is Spawn()→Init() everywhere, the guard disables itself online (IsSpawned=true → NGO
  manages authority).
- **A second structural problem:** the `GravitySource` script is on Planet_Interior while the wide gravity
  trigger is on the child `GravityTrigger`, and there is no Rigidbody2D in the hierarchy — Unity does NOT
  forward trigger callbacks to the parent, so that child's `OnTriggerStay2D` never reached GravitySource.
  Until now the pull only worked because Planet_Interior's own collider had been manually made a trigger in
  the scene (and at a radius different from gravityRadius). **Fix:** the force application was moved into
  `GravitySource.FixedUpdate()` — it's applied via `OverlapCircle` to every dynamic Rigidbody2D within
  gravityRadius (once per body, without touching sleeping ones, scaled by `rb.mass`). So the area of effect
  is exactly gravityRadius, the acceleration is a mass-independent `gravityForce`, and it matches the
  `TrajectoryDots`/`IGravityStrategy` prediction exactly.
- `SinglePlanetGravity`/`MultiPlanetGravity` now respect `gravityRadius` (the prediction and the real
  physics boundary are the same).
- **Live verification (Editor Play Mode, SampleScene):** a spawned pistol projectile had bodyType=Dynamic,
  the measured acceleration toward the planet was |a|=19.84 ≈ gravityForce(20), and it hit the surface in
  0.84s and was destroyed correctly. (The ~34 value in the first measurement was a wall-clock/physics
  catch-up artifact; the second, Time.time-based measurement came out correct.)

### Online Leaderboard — a Clash Royale style trophy system (converted from a win counter at the user's request)
- Package: `com.unity.services.leaderboards` 2.3.4 (installed via Coplay MCP; since the tool doesn't work
  while there are compile errors, the leaderboard references were temporarily commented out first and
  restored after installation).
- `Assets/Scripts/Cloud/LeaderboardManager.cs`: the trophy logic — an online match win is **+30**, a loss
  is **−20** (never below 0), and a draw is no change; the current total is submitted to UGS Leaderboards.
  League names by trophy range (`GetLeagueName`: Asteroid/Moon/Planet/Star/Nebula/Galaxy League).
  A registered username is reflected to the leaderboard via `UpdatePlayerNameAsync` (a no-op for guests).
- **Important finding (found via live diagnosis):** the static `LeaderboardsService.Instance` is NEVER set
  in this project — core initializes packages through the instance-based path
  (`IInitializablePackageV2.InitializeInstanceAsync`), and that path skips the static one (visible in the
  package source: `Initialize()` sets the static, `InitializeInstanceAsync()` does not). While the service
  was registered and healthy in the CoreRegistry, static access threw a
  `ServicesInitializationException`. **The correct access:** `UnityServices.Instance.GetLeaderboardsService()`
  (the `LeaderboardManager.Service` property; the static is only a fallback). This gotcha may apply to other
  UGS packages too.
- `TurnManager.TriggerGameOver` → the new `AnnounceMatchResultClientRpc(winnerClientId)`: it announces the
  online match result to ALL clients (since TriggerGameOver only runs on the server, this is the only way
  clients can learn whether they won or lost; a draw = ulong.MaxValue → no trophy change). Offline matches
  award no trophies (the leaderboard is online-only).
- `Assets/Scripts/UI/LeaderboardPanelUI.cs`: a new "LEADERBOARD" button in the main menu (the button card
  was enlarged for 8 buttons) → a panel with rank/name/league/trophy columns, the player's own row
  highlighted, and a REFRESH button (the AchievementsPanelUI pattern). LeaderboardManager + the panel were
  added to the `MainMenuUI.EnsureProgressSingletons` bootstrap.
- **Verification:** the panel opens in Editor Play Mode and the service call reaches UGS; the only expected
  warning is `Leaderboard config could not be found` — because **the leaderboard hasn't been created in the
  dashboard yet**.
- **REMAINING MANUAL STEP (no code):** cloud.unity.com → project → Leaderboards → Add leaderboard:
  ID **`cosmic_trophies`**, Sort order **High to low**, Update type **Latest submission** (NOT "Keep best",
  because trophies can go down). Until it's created the panel shows an empty list + a warning in the editor;
  the game doesn't break.

### General review — known remaining rough edges (not fixed in this session)
- `GravityBody.DominantSource = AllSources[0]` — in a multi-planet scene the jump direction is relative to
  the "first" planet, not the nearest/dominant one; risk of jumping in the wrong direction while on the
  second planet.
- `ProjectileBase.OnBecameInvisible` / `Projectile.destroyOnInvisible` destroys a projectile the instant it
  goes off screen — this can kill long-trajectory shots early (rare in practice, since the camera follows
  the projectile).
- The main menu button icons (▶ ⇄ ★ ◆ ♛ ⚙ ✕) don't exist in LiberationSans SDF and all render as □
  (pre-existing; a fallback font needs to be added).
- At the end of an online match the game-over UI only appears on the host (TriggerGameOver is server-only;
  the trophy RPC relays the result to clients but the UI display is separate and was left out of scope).

## Remaining rough edges resolved + Ranked/Friendly match distinction — Done (2026-07-06, 2nd pass)

- **Leaderboard dashboard VERIFIED (end-to-end):** `cosmic_trophies` was created by the user
  (High to low / Latest). Live test: fetch returned an empty list with no warnings ✓;
  `ReportOnlineMatchResult(true)` → +30 trophies submitted → it appeared in the table at rank #1, score=30
  ✓; the test data was reset to 0 and resubmitted (the table was left clean). Note: since the editor
  session was a guest, the name showed as the anonymous UGS name ("EasyAstonishedOstrich#26782") — for a
  registered user, `SyncPlayerNameAsync` writes the real name.
- **DominantSource fixed** (`GravityBody.FixedUpdate`): the nearest planet (prioritizing those within
  gravityRadius) instead of blindly `AllSources[0]`. The jump direction and `CameraController` rotation are
  now relative to the correct planet in a multi-planet scene.
- **Off-screen projectile death softened:** `ProjectileBase`/`Projectile.OnBecameInvisible` no longer
  destroys instantly — it's cancelled if `OnBecameVisible` arrives within `offscreenGraceTime` (3s); orbital
  shots that wrap behind the planet survive, while ones that don't come back are still cleaned up (the TTL
  is also still in place).
- **Main menu icon glyphs removed** (▶ ⇄ ★ ◆ ♛ ⚙ ✕ → text only): characters missing from LiberationSans SDF
  were rendering as □; clean text-only buttons for mobile. Verified via screenshot.
  (If permanent icons are wanted, a fallback font asset can be added later.)
- **Online match end is now handled on BOTH machines:** `TurnManager.TriggerGameOver` was restructured —
  online, all local match-end work (game-over UI, XP/Gold/chest, achievement events, audio, trophies) runs
  on each machine according to its own local result via `FinishMatchLocally()`, called from the new
  `AnnounceMatchResultClientRpc(winnerClientId, winnerName, matchDuration, totalShots)` (the host doesn't
  get double rewards, and the client now sees the game-over screen and its rewards). Offline hotseat calls
  `FinishMatchLocally` directly, with its old behavior. A draw = winnerClientId=ulong.MaxValue → no trophy
  change, and both sides see the "lost" flow (consistent with the old host behavior).
- **Ranked/Friendly distinction (the Clash Royale rule):** `NetworkBootstrap.IsRankedMatch` — true for
  `QuickMatchAsync`, false for `HostSessionAsync`/`JoinSessionAsync`, and reset by `LeaveSessionAsync`; a
  client reconnect preserves the match's ranked status. Trophies only change in ranked matches. The join
  code is NO LONGER SHOWN while waiting in Quick Match (a friend joining by code would have created a
  ranked/friendly mismatch between the two sides).
- **The online lobby was reframed (the main mobile flow = Quick Match):** a large "QUICK MATCH — RANKED"
  card at the top (with a +30/−20 trophy hint), and below it a "PLAY WITH A FRIEND — friendly match,
  trophies don't change" heading with "CREATE CODE" (send the code to your friend) and "JOIN BY CODE"
  cards; "← BACK". Verified via screenshot.

## Mobile visual refresh (menu + online lobby + leaderboard) — Done (2026-07-06, 3rd pass)

- **`Assets/Scripts/UI/UiKit.cs` (new):** since the project has no UI sprite assets at all, the rounded
  corner sprite is generated at runtime via SDF (antialiased, 9-sliced, cached). `UiKit.Round(img,
  cornerScale)`, `UiKit.Shadow(go)` (a soft bottom shadow) and `UiKit.ButtonColors(normal)` (derives the
  hover/press colors from the normal color) are used on every newly styled surface.
- **The main menu moved to a mobile landscape layout:** instead of a narrow vertical list of 8 buttons,
  2 large primary buttons (PLAY / ONLINE, 388×92) + a 2-column × 3-row secondary grid (376×72) inside a
  wide rounded card — the touch targets got bigger and the horizontal screen space is used. The title
  background and the settings card/tabs/back button were rounded too; the □ (⚙) in the settings header was
  removed; and the left stripe on the old buttons (which clashed with the rounding) was removed.
- **Online lobby:** the cards are rounded+shadowed; PLAY is large and GREEN as the primary action (320×68);
  CREATE CODE/JOIN were enlarged to 290×62; CANCEL/BACK were given a neutral dark color and enlarged; the
  code input was rounded.
- **Leaderboard:** the card was widened to 760 and rounded+shadowed, the rows are rounded (52 height,
  6 spacing), and CLOSE/REFRESH were enlarged.
- All of it was verified with Editor Play Mode screenshots (menu, lobby, leaderboard). The
  Achievements/Quests/Shop panels stayed in the OLD flat style — they can be migrated with the same UiKit
  calls, separate work.

## Brawl Stars style lobby main screen — Done (2026-07-06, 4th pass)

After research (Brawl Stars UI analyses + mobile lobby patterns), the main menu was moved from a "button
list" to a "lobby/hub" layout:
- **Bottom-right: a BIG YELLOW PLAY** (420×124, dark text) → OnlineLobbyPanelUI; a "RANKED • QUICK MATCH"
  info chip above it; a secondary "LOCAL MATCH" (hotseat) to its left. In the right-thumb zone in
  landscape grip.
- **Top bar:** on the left a profile chip (username/Guest + live trophy count + league name; tapping it
  opens the Leaderboard), on the right the Gem + Gold chips (subscribed live to CurrencyManager, tapping
  them opens the Shop; positioned so they don't clash with MainMenuAuthButton's top-right card).
- **Left rail:** SHOP / QUESTS / ACHIEVEMENTS / LEADERBOARD "chunky" buttons; bottom-left: SETTINGS +
  QUIT (neutral).
- **MakeChunkyBtn:** a button with a 3D edge for the Brawl Stars look (a 6px dark bottom edge + a face
  plate).
- **Decor:** a planet on the bottom horizon + a blue atmosphere halo + a small moon, via UiKit.CircleSprite
  (new, an AA circle). The title block was shrunk (52pt) — the center is now airy.
- The old shortcut button in the top-left of AchievementsPanelUI was removed (it clashed with the left rail
  and was redundant).
- Verified via screenshots; the build is clean.

## Next-gen UI pass: panel restyle + silent sign-in + PLAY v2 — Done (2026-07-08)

Scope (user request): (1) migrate the Achievements/Quests/Shop panels to the UiKit style, (2) research
Brawl Stars / modern mobile UI patterns, (3) differentiate the PLAY button, convert the sign-in/register
flow to the mobile pattern (no visible login — one silent sign-in), and make the UI "next-gen". The game is
now treated as **mobile-only** (acting as if Steam doesn't exist — the user said so explicitly; the Steam
sections in the TODO remain as history).

- **UiKit grew (new tools, all runtime-generated sprites/components):** `Stroke` (a thin light outline —
  a glass-edge feel, `RoundedOutlineSprite`), `Gradient` (a vertical vertex gradient,
  `UiVerticalGradient:BaseMeshEffect`), `Press` (`UiPressScale` — shrinks to 94% on touch), `Pop`
  (`UiPopIn` — scale+alpha on panel open, ease-out-back), `Pulse` (`UiPulse` — a breathing animation for
  the primary button), `CloseButton` (the mobile standard: a red X overflowing the card's top-right
  corner), `GlowSprite` (a radially fading blob — MainMenuUI's nebula "Glow"s were plain Images and looked
  rectangular; they're soft now).
  - Gotcha: `UiPulse`/`UiPressScale` both write localScale — don't add both to the same object (PLAY only
    has Pulse).
- **The Achievements/Quests/Shop panels were restyled** (an 820×600 rounded+outlined+pop card, a corner X,
  rounded rows/progress bars): in Achievements a rarity-colored icon circle (instead of the old square
  stripe) and Common/Rare/Epic/Legendary labels; in Quests pill tabs (DAILY/WEEKLY/MONTHLY); **the Shop got
  a completely new layout** — instead of a row list, 5 side-by-side gradient pack cards (a gem circle +
  amount + a green BUY with the price), with a "POPULAR" badge on 1200 and a "BEST VALUE" badge on 6000.
  LeaderboardPanelUI was brought into the same language too (a LEADERBOARD heading, REFRESH, a corner X).
- **Silent one-time sign-in (the Supercell pattern):** after cloud init, `MainMenuUI.BootstrapSequence` now
  silently awaits `LoginAsGuest()` if there is no session → `IsLoggedIn=True IsGuest=True` without showing
  any UI (verified live). The persistent LOG IN card in the top-right (`MainMenuAuthButton.cs`) was
  **deleted** (+ its GameObject in MenuScene was removed via MCP); the currency chips moved to the real
  top-right corner. The "LOG IN AND START" gate in `LobbyPanelUI` was removed. Account linking lives in one
  place: Settings → ACCOUNT tab ("You're playing as a guest... link your account" + LINK ACCOUNT/LOG OUT) →
  `LoginPanelUI` was reframed: a "LINK YOUR ACCOUNT" heading, value-proposition text about carrying
  progress over, LOG IN / CREATE NEW ACCOUNT (with a hint that it inherits guest progress), the PLAY AS
  GUEST button was dropped (guest is already the default), and it moved to the UiKit style.
- **PLAY v2:** a light→dark gold vertical gradient face + a white glass outline + a dark gold 3D edge + a
  continuous gentle breathing (Pulse) + a "QUICK MATCH • RANKED" subline embedded in the button (the
  separate mode chip was removed). An initial-letter avatar circle was added to the profile chip. The Press
  micro-interaction was added to all chunky buttons/chips, and Pop to the panels.
- Slider labels being squashed under the slider was fixed (a pre-existing issue: "Master Volume" was being
  cut off to something like "Master Vo").
- **Verification:** via Editor Play Mode screenshots: the hub (soft nebula + the new PLAY + the avatar
  chip), QUESTS, ACHIEVEMENTS, SHOP, LEADERBOARD (with live UGS data), the LINK YOUR ACCOUNT dialog, and
  the SETTINGS/ACCOUNT tab. The build is clean. Silent guest sign-in was verified with the `AuthState`
  script.
- **Tooling gotcha (new):** Coplay `save_scene(scene_name)` does a **save-as** of the active scene to
  `Assets/<name>.unity` (it doesn't preserve the `Assets/Scenes/...` path) — MenuScene was accidentally
  copied to the root; fixed via `execute_script` with
  `EditorSceneManager.SaveScene(scene, "Assets/Scenes/MenuScene.unity")` + deleting the stray asset. From
  now on, use execute_script instead of save_scene to save scenes.

## Brawl Stars menu revision: Guest removed, sign-in gate, skewed buttons — Done (2026-07-08, 2nd pass)

Following user feedback ("I don't like it; there will be no guest — only for testing; there will be no quit
button in the main menu; study the Brawl Stars menu properly; the sign-in screen only for those without a
linked account, and straight to the main menu once signed in"):

- **"Guest" was removed from the UI entirely.** The new `Assets/Scripts/Utilities/PlayerIdentity.cs`: the
  display name comes from a single source — the username on a linked account, otherwise a cosmic nickname
  generated once on the device and written to PlayerPrefs (e.g. "Pulsar630"; an ASCII prefix list — UGS
  UpdatePlayerNameAsync rejects special characters). Used by: the main menu profile chip, `GameInitializer`
  (the hotseat character name — it used to show "Guest" for guests), `LobbyPanelUI`, and
  `LeaderboardManager.SyncPlayerNameAsync` (which is no longer a no-op for guests — it writes the nickname
  to UGS in an anonymous session too, so names like "EasyAstonishedOstrich" no longer appear on the
  leaderboard).
- **The sign-in gate (a single login screen):** at the end of `MainMenuUI.BootstrapSequence`, if no account
  is linked, `LoginPanelUI.Show(dismissable:false)` — it cannot be dismissed on device (no X/ESC, heading
  "SIGN IN"); after sign-in/registration the panel closes and the player is in the main menu. In the Editor
  it's `dismissable:true` (an unlinked session is for testing only — so hotseat tests don't get stuck at
  sign-in). Settings → LINK ACCOUNT opens the same panel in a dismissable state (heading "LINK YOUR
  ACCOUNT"). The silent anonymous UGS session in the background remains (it's required for Register to
  inherit progress) but it isn't a UI state.
- **The QUIT button was removed from the main menu** (along with `OnQuit` — mobile games don't have a quit
  button).
- **Brawl Stars menu signatures:** `UiKit.Skew` (`UiSkew:BaseMeshEffect`, horizontal shear 0.10 — all
  chunky buttons + a PLAY parallelogram) and `UiKit.BrawlText` (bold italic + a dark SDF outline — all
  button text, with PLAY's text white-outlined). The layout was aligned with Brawl Stars: 3 buttons on the
  left rail (SHOP/ACHIEVEMENTS/LEADERBOARD), **QUESTS bottom-left** (BS's quest slot), **SETTINGS as a
  small neutral chip top-right** (below the currency chips), PLAY bottom-right with LOCAL MATCH to its
  left. The Stroke on PLAY's face was removed (a child stroke wasn't being skewed and looked inconsistent).
- **Bug fix (caught from the logs):** `GetComponent<CanvasGroup>() ?? AddComponent` in `UiPopIn.OnEnable`
  was throwing a `MissingComponentException` because of Unity's fake-null — converted to an explicit
  `== null` check. (The `NetworkManager.OnDestroy` NullRefs when exiting Play Mode are the NGO package's own
  known shutdown noise, not our code.)
- **Verification:** Editor Play Mode — the SIGN IN screen (with no X) appeared automatically at launch;
  behind it the main menu in the new layout ("Pulsar630" profile, skewed buttons, no QUIT); PLAY's right
  margin was measured with `GetWorldCorners` (1884/1920 — the capture tool's cropping had made it look like
  overflow; there is no real overflow); the quest panel opened with the pop animation without errors. The
  build is clean.

## Brawl Stars visual language — full revision against a real reference — Done (2026-07-08, 3rd pass)

The user shared real Brawl Stars screenshots ("the design is still bad", "what matters is the layout", "the
panel that slides in from the right in the 3rd image makes a lot of sense") — the design is no longer
guesswork, it was done against a direct reference:

- **The font (this was the biggest difference):** `Assets/Fonts/TitanOne-Regular.ttf` (Google Fonts, OFL)
  was downloaded; an editor script generated a dynamic TMP font asset (`Assets/Fonts/TitanOne SDF.asset`)
  and **made it the TMP Settings default font** — all programmatic UI switched over automatically.
  `UiKit.BrawlText` is now: a white fill + a thick dark SDF outline, UPRIGHT (italic/fake-bold were turned
  off — Titan One is already a heavy display font).
  - **Glyph gotcha:** Titan One has no capital **'İ'** (U+0130) — it falls back to LiberationSans and looks
    conspicuously thin. Lilita One was tried: worse (no ğşĞŞİ), deleted. Workaround: use lowercase in
    sublabels; the İ characters in headings remain on the fallback (accepted).
  - MakeCycler's ◀▶ arrows were changed to "<" ">" (neither font has those glyphs, they rendered as □).
- **The button language was fixed:** instead of colored-face buttons, **dark charcoal plates** like in BS
  (`MakePlate`/`MakeBrawlBtn`: PlateDark + a dark bottom edge + a slight skew + a colored circle-letter
  icon badge on the left + white-outlined text). Color only for emphasis: SHOP and PLAY are yellow (BS's
  SHOP/PLAY), prices are green.
- **Layout (directly from the BS main screen):** top-left: [avatar+name plate][trophy box: a trophy badge +
  count + league] as two separate plates (both open the Leaderboard); top-right: [gold][gem] plates + a
  **☰ menu button** (3 white bars, drawn with an Image); left column: SHOP (yellow) + ACHIEVEMENTS;
  bottom-left: QUESTS; bottom-center: a **mode plate** ("QUICK MATCH / Ranked • Win +30 trophies" — the
  position of BS's map box, opens the online lobby when tapped); bottom-right: the big yellow PLAY. The
  footer was removed entirely (the version moved to the bottom of Settings).
- **The ☰ drawer (which the user specifically asked for):** a list of dark plates sliding in from the right
  with a 0.14s ease-out — SETTINGS / LEADERBOARD / LOCAL MATCH / ACCOUNT (ACCOUNT → the Account tab of
  Settings); it closes when you tap outside (a transparent dimming button). `SetDrawer(bool)` + a
  `SlideDrawer` coroutine, inside MainMenuUI.
- **Background:** a lively BS lobby instead of pitch-black space: a purple vertical gradient + a giant
  radial glow (blue on the left, pink on the right) + a **faint pattern texture** (`BuildPattern`: alpha
  0.022, rotated rounded tiles — the counterpart of BS's skull pattern) + the existing starfield/planet
  horizon.
- **The Settings screen moved to BS blue** (the 2nd reference image): a fullscreen blue gradient + the
  pattern, a white-outlined SETTINGS heading, blue tab plates, and a dark BACK.
- **Verification:** Play Mode screenshots — the hub (new layout, soft pattern, PLAY's subline correct), the
  drawer open (the same arrangement as BS's 3rd image), the blue SETTINGS, and the SHOP panel with the new
  font. The build is clean, no new console errors.

## Game Modes (1v1 / FFA / Team) + Party Lobby
Done (2026-07-10) — user request: "I should be able to play with my 8 friends, we should gather in a lobby
and pick a mode". The project had previously been entirely locked to 1v1 (a fixed session `MaxPlayers=2`,
`TurnManager`'s "last character wins" logic, and an invite flow specific to a single friend). Now 1v1, FFA
(3-8 players), 2v2, 3v3, 4v4 and 2v2v2v2 are all supported — the lobby capacity was capped at **max 8** by
user decision, so 3v3v3 (which requires 9 players) was left out of scope (there are only 2-team modes + the
2v2v2v2 four-way mode; there is no 3-team mode).

- **`GameModeDefinition.cs`** (new): a `GameModeType` enum + the team count/size for each mode. `LobbyData`
  received `SelectedMode`/`FfaPlayerCount`/`PartyMembers` in place of the dormant `GameMode` string.
- **`GravityBody.teamId`** (a new `NetworkVariable<int>`, the same pattern as `isActive`) — in team-less
  modes (Duel1v1/Ffa) every player is their own singleton team; in team modes it's a round-robin shared id.
  The name tag is tinted with the team color (**the body sprite was DELIBERATELY not tinted** —
  `ShieldSkill.cs` uses the same SpriteRenderer for the shield effect, and if they clashed the color would
  be wiped when the shield ended).
- **`TurnManager.CheckGameOver`**: "the number of distinct surviving teams ≤1" instead of "the last
  character" — identical to the old behavior in team-less modes, and in team modes it doesn't end until an
  entire team is eliminated.
  **A critical regression found and closed in the same pass**: because teamId defaults to 0, this change on
  its own would have ended EVERY match on the first frame — `GameInitializer`/`NetworkPlayerSpawner` now
  perform real team assignment at spawn time.
- **`PartyLobbyPanelUI.cs`** (new, replacing the old `FriendLobbyPanelUI` — which had a fixed 2 slots): the
  host picks a mode (a 3-8 stepper for FFA), CREATE PARTY opens a private UGS session (MaxPlayers according
  to the mode), and from a 3x3 roster screen they can invite as many friends as they like to the same
  session code (via `FriendsManager`, one at a time). The guest side only sees a live "X/N participants"
  counter — **there is no full name-based roster sync** (it would require a clientId↔PlayerId
  mapping/NetworkList, left out of scope).
- **Deliberate scope decision**: Quick Match (public, ranked, +30/−20 trophies) **stayed 1v1** — the new
  modes are only reachable from the private party lobby (always a friendly match). This keeps
  `DUELLO_SAMPIYONU`, `REKABETCI` and the trophy formula (`LeaderboardManager`) correct without any change.
- **Friendly fire filter**: `CombatEventReporter` compares the target's team using `TurnManager.CurrentShooter`
  (the already-existing "character currently firing" tracking) to prevent "N different opponents"
  achievements like `HERKESE_MEYDAN`/`SOSYAL_KELEBEK` from being inflated by teammate/self hits.
- **`INTIKAM` fixed**: it now targets the actual killer via `FireDefeatedBy` rather than the match's final
  winner (in FFA/team modes those can be different people) — the new `AchievementEvents.OnDefeatedBy`.
- **Play-tested (Coplay MCP)**: offline hotseat Duel1v1 (2 characters, doesn't end on the first frame — the
  regression test), Team2v2 (4 characters, round-robin team assignment, doesn't end while 2 teams are
  distinct), and the party lobby host flow (pick mode → FFA stepper → CREATE PARTY → a real UGS session was
  created, no errors). One real bug was found and fixed in this pass:
  `PartyLobbyPanelUI.BuildRosterRoot()` was calling `UiKit.BrawlText()` in an inactive hierarchy (the same
  TMP outline/OnEnable bug seen earlier in WardrobePanelUI) — the ordering was fixed.
- **Remaining/deliberately deferred work**:
  - A real multi-device online FFA/team test (a single-developer environment, never possible — the same
    constraint as the friend invite/presence test, see roadmap item 2).
  - The host cannot reassign teams by drag-and-drop in the party lobby — team assignment is currently
    automatic by join order only (round-robin).
  - `KOZMIK_EKIP` still tracks a single friend id (in the party flow it only progresses for the first
    invited friend) — real group tracking is separate work.
  - `KARA_DELIK_USTASI` (Black Hole multi-pull) is still on its own separate counter path and did not
    receive the friendly fire filter.
  - There is no name-based roster sync of the other participants on the guest side (explained above).

## Terrain destruction performance fix + Black Hole VFX refresh — Done (2026-07-14)

### DestructiblePlanet explosion cost (~60-87ms/explosion → ~7-10ms/explosion)
- Found by real measurement in Play Mode: every `ExplodeWithForce` call (a projectile/RPG/bomb/grenade
  hit) took 60-87ms — 4-5× the 16.6ms frame budget at 60fps, a genuine freeze on every hit. By isolating
  the root cause (measuring the pixel loop / `Apply` / `Sprite.Create` / collider rebuild separately with
  a `Stopwatch`), it was found that `Sprite.Create(..., generateFallbackPhysicsShape: true)` spends a
  constant ~90-140ms INDEPENDENT of the explosion radius — that API re-scans the alpha outline of the
  ENTIRE 1280x1280 texture every time it's called (regardless of the dirty-region size). The
  `GetPixel`/`SetPixel` loop was also separately expensive at large radii (RPG/bomb), up to 149ms.
- **Fix (`Assets/Scripts/Planet/DestructiblePlanet.cs`):**
  1. `GetPixel`/`SetPixel` → array indexing over a `Color32[] pixels` cache fetched once in `Start()`,
     with a single `SetPixels32`+`Apply()` at the end of the loop.
  2. The visual update no longer RECREATES the Sprite at all — `Apply()` already updates the texture the
     same Sprite references, so a new Sprite was never necessary (the old code recreated one needlessly
     on every explosion).
  3. The collider is now generated from a separate, one-off helper texture downscaled by
     `physicsDownsampleFactor` (default 8x, adjustable from the Inspector, range [1,12])
     (`RebuildColliderFromAlpha`, which replaced the old `RebuildCollider`) — the downscaled sprite's
     `pixelsPerUnit` is reduced by the same ratio (`ppu/factor`) so the shape is produced in the correct
     local-unit space without manual scaling. Visual quality is entirely unaffected (runtimeTex/sr.sprite
     are untouched); only the collider's corner precision drops (not noticeable at character scale).
- **Measured (Play Mode, a real planet, before/after with the same method):** r=0.3: 65.8→7.1ms, r=1.0:
  60.7→7.0ms, r=2.0: 79.2→8.8ms, r=3.0: 86.8→10.5ms — an average gain of ~8-9x, comfortably under the
  60fps budget.
- **Verification:** the build is clean, no runtime errors in Play Mode, the collider is functional after
  an explosion (`pathCount=4`, 124 points, `isTrigger=false`), and the visuals were verified by screenshot
  (the circular notches are clean, no distortion). The other systems that trigger explosions
  (BlackHoleZone/BlackHoleProjectile's `dp.ExplodeWithForce`, Bomb/Grenade/RPG/Kinetic) benefit
  automatically since they go through the same path; no separate work was needed.

### Black Hole VFX — the user's own generated sprite sheet instead of a downloaded GIF
- The old `BlackHoleGif` art was 90 separate frame PNGs extracted from a Tenor GIF downloaded off the
  internet (unclear license; the filename `...gTCLX76XXbiwlrts...` is a Tenor GIF ID) — it was replaced
  with an 8-frame (4x2) hand-drawn-style vortex sprite sheet provided by the user.
- The black background was made transparent via alpha-keying (`alpha = max(R,G,B)`, Python/Pillow) — no
  halo/smudging at the edges, verified by screenshot.
- It was imported as `Assets/Art/Sprites/BlackHoleVortex/BlackHoleVortex_Sheet.png` and sliced into 8
  sprites in Unity (a 4x2 grid, ppu=50). The existing `BlackHoleGif.anim` (12fps, 0.67s loop) was updated
  with these new frames — no code changes were needed on the
  `BlackHoleProjectile`/`BlackHoleZone`/prefab/controller side (the same `gifPrefab` reference,
  `Assets/Art/Sprites/Projectiles/BlackHoleGif.prefab`).
- The old 90 frame PNGs (+ meta) were deleted.

### Next steps
- `physicsDownsampleFactor` is currently a fixed Inspector value (8) for all planets — if very small/very
  large planet variants are added, it can be tuned separately according to the radius/resolution ratio.
- The production workflow for the black hole art (alpha-key + grid slicing) is repeatable — in the future
  the user can have other visual effects (explosions, shields, etc.) added the same way from art they
  generate themselves.
- For the other gaps identified in the general project review (same pass) but not yet touched, see the
  "RELEASE ROADMAP" section above (non-code items like the Google Play Console setup, a real device build
  test, the 150 costume sprites, and legal text approval are still the priority).

## Security audit + fixes — Done (2026-07-14, 2nd pass)

The codebase was scanned systematically (economy/IAP client authority, multiplayer RPC authorization,
auth/credential management, hardcoded secrets, save-data integrity). The portion of the findings that
could be fixed at the code level was resolved in this pass; the ones requiring server/backend
infrastructure (Cloud Code etc.) were explicitly marked "couldn't be done, here's how it should be done" —
no fake/half "resolved" impression was given.

### Fixed

1. **Server-side turn validation was missing in the ability-fire [ServerRpc]s.** `Pistol`/`Rpg`/
   `Shotgun`/`HandGrenade`/`Teleport`/`BlackHoleSkill` (`FireServerRpc`/`FirePelletsServerRpc`),
   `BatHammerSkill` (`SwingServerRpc`), `ShieldSkill` (`ActivateShieldServerRpc`) — the "is it my turn"
   check only existed in the client-side `AbilityBase.Update()`; the server-side RPC handler ran directly
   without any validation. A modified client could fire during the opponent's turn, or fire repeatedly
   before the same projectile resolved. **Fix:**
   `protected bool ServerCanAct => gravityBody != null && gravityBody.isActive.Value;` was added to
   `AbilityBase.cs`, and `if (!ServerCanAct) return;` was placed as the first line of all 8 RPC handlers —
   this single check prevents both out-of-turn firing and repeat firing, thanks to
   `TurnManager.NotifyProjectileLaunched` synchronously setting `isActive.Value` to false immediately
   after the first shot.
   - **Followed up and closed (2026-07-15, 3rd pass) — `CharacterAbilities` was made
     network-authoritative.** `MonoBehaviour` → `NetworkBehaviour`; all ammo counters (`superJumps`,
     `rpgAmmo`, `pistolAmmo`, `shotgunAmmo`, `grenades`, `shields`) were collected into a single
     `AmmoState` (`INetworkSerializable`) struct and made a `NetworkVariable<AmmoState>` (Server-write);
     `HasUsedSkillThisTurn` also became a `NetworkVariable<bool>` (Server-write). The new
     `ServerTryConsume(int slotIndex)` rejects ANY slot while `netHasUsedSkill` is true (the one-ability-
     per-turn rule, which also closes the "different weapon back to back" hole without needing a
     weapon-specific server-side cooldown timer) and checks and decrements the relevant slot's ammo — it is
     called from each of the 9 abilities' (Pistol/Shotgun/Rpg/HandGrenade/Teleport/BlackHoleSkill/
     BatHammerSkill/ShieldSkill/SuperJumpSkill) `[ServerRpc]` handler IMMEDIATELY AFTER `ServerCanAct`.
     `SuperJumpSkill` had no `[ServerRpc]` of its own (it only set the client-side
     `gravityBody.nextJumpIsSuper` flag) — a new `ConsumeServerRpc`/`ApplySuperJumpClientRpc` pair was
     added (the same owner-targeted ClientRpc pattern as in `GravityBody.ApplyForce`). The public API
     (getters, events, `HasUsedSkillThisTurn`) did not change at all — zero changes were needed in
     `WeaponUIManager`/`SkillUIManager`. Offline hotseat behavior was deliberately preserved exactly
     (every `Use*()` method performs the same direct mutation as before in its `!IsSpawned` branch) — the
     (already tested) "direct offline, server-only online" pattern from
     `CharacterHealth.Awake()/OnNetworkSpawn()` was imitated exactly; no new architecture was invented.
     **Note — the Unity Editor was closed during this pass, so live/two-process verification could not be
     done**; the change was written with careful static review + strict fidelity to the existing, already
     verified `CharacterHealth` pattern, but testing firing/ammo/turn transitions in both an offline and an
     online (two-client) match in the next Unity session is essential.
   - **A permanent architectural limit (unsolvable in code):** this project uses P2P Relay/NGO where the
     host is one of the players ("server" = one player's own machine); there is no authoritative/neutral
     dedicated server. The fix above protects against a cheating NON-HOST client; a cheating HOST can write
     its own server-write NetworkVariables like `isActive`/`teamId` however it likes — solving that
     requires investing in a dedicated/cloud-hosted authoritative server, which is outside the scope of a
     bug-fix session.
2. **`BatHammerSkill.SwingServerRpc`** used the `aimDir` coming from the client without normalizing it
   (a non-unit vector could have broken the in-cone target detection). **Fix:** the server now uses
   `aimDir.normalized` and rejects near-zero vectors.
3. **13 unguarded `Debug.Log`/`LogWarning`/`LogError` calls in `NetworkBootstrap.cs`** (join codes,
   connection state) were being written to the device log in production builds too — contrary to the
   `#if UNITY_EDITOR` rule used in the rest of the project. **Fix:** all of them were wrapped in
   `#if UNITY_EDITOR`.
4. **`CurrencyManager`'s `currency.json` was plaintext, unsigned/open to tampering** (Gold/Gem/XP could be
   changed directly with a save editor). **Fix:** the file is now in a `{data, hmac}` envelope, signed with
   `HMACSHA256(embedded key + SystemInfo.deviceUniqueIdentifier)`; on an HMAC mismatch, `Load()` detects
   the tampering, falls back to safe defaults, and rewrites the file clean and signed. Existing player
   files in the old (envelope-less) format are read backward-compatibly and rewritten in the new format at
   the first opportunity (progress is not wiped).
   - **An honest limit (also stated in the comments):** since the key is embedded in the client binary,
     this blocks common tools like save editors but does NOT STOP an advanced attacker capable of reverse
     engineering — it is a deterrent/detection layer, not a cryptographic guarantee.
5. **IAP purchases granted Gem without any receipt validation.** **Fix:**
   receipt validation via Unity IAP's `CrossPlatformValidator` was added to `IAPManager.cs` — the same
   "code is ready, waiting on the real key" pattern as `STEAMWORKS_INSTALLED`/`GPGS_INSTALLED`: while the
   `IAP_RECEIPT_VALIDATION` define is not set (which is the current state), `IsReceiptValid()` always
   returns `true` — enabling that define before the Tangle classes (`GooglePlayTangle`/`AppleTangle`) are
   generated would be a compile error, so it was deliberately left off.
   **Remaining manual step (no code):** get the Base64 RSA public key from Play Console → Your app →
   Integrity → Licensing → paste it into Window → Unity IAP → Receipt Validation Obfuscator in the Unity
   Editor → add `IAP_RECEIPT_VALIDATION` to Player Settings → Scripting Define Symbols.

### Unresolved — requires backend/infrastructure investment (not left half-done on purpose, explicitly flagged)

6. **`CloudSaveManager.PushAsync`** writes the raw contents of the local files (currency/progress/unlocks/
   quests/chests/streak/costumes) directly to UGS Cloud Save without any server-side validation — the HMAC
   fix (item 4) only protects the LOCAL file; the data going to the cloud is still whatever the client
   holds in memory at that moment (and therefore changeable with a memory-hacking tool). The real solution:
   move the economy mutations (Add/Spend) into UGS Cloud Code functions (the client only sends a request
   like "I won this match" to the server, and the real balance calculation and Cloud Save write happen
   there) — that is a Cloud Code authoring/deployment job, outside the scope/tooling of this Unity client
   project.
7. **`LeaderboardManager.ReportOnlineMatchResult`** is a public method that calls `AddPlayerScoreAsync`
   directly without any server/Cloud Code check verifying that a match actually happened — nothing prevents
   a cheating client from inflating trophies arbitrarily.
   - **Why it wasn't done in this pass either (the same rationale as item 6, plus more):** the `ugs` CLI
     isn't installed on this machine, the `com.unity.services.cloudcode` package hasn't been added to the
     project, and a real deploy (Dashboard sign-in/CLI login) is an interactive step that can only be done
     with the user's own Unity identity — effectively impossible from this session. MORE IMPORTANTLY: because
     of the host-trust limit in item 1, this is not a simple "let the client call Cloud Code" fix — since a
     cheating HOST is already in the server role, a real fix requires CROSS-VALIDATION (dual attestation)
     between the two sides: both clients INDEPENDENTLY report the match result (matchId + winnerId/loserId,
     with both of their real UGS PlayerIds) to the same Cloud Code function; the function awards no
     trophies if the two reports DISAGREE or if only one arrives, and only updates trophies if both report
     the same result (a single cheating party is no longer sufficient on its own — they'd have to collude
     with their opponent). This also requires the mutual exchange of the opponent's real PlayerId, which is
     currently unknown to the clients (in Quick Match — it already exists in the friend-invite flow); so
     this isn't just a Cloud Code script, it's a new cross-client protocol. Without being able to test it
     live across two processes (the Unity Editor was closed in this session), wiring it into the
     directly-tested, working ranked match flow is risky — it would conflict with the "no problems" goal,
     so it was deliberately not done.
   - **A concrete plan for the next session:** (1) have both clients report their own
     `AuthenticationService.Instance.PlayerId` to the server via a `[ServerRpc]` at match start in
     `NetworkPlayerSpawner`/`TurnManager`, and have the server broadcast both back to both clients via a
     `[ClientRpc]` (a host-side `Dictionary<ulong,string>` clientId→PlayerId mapping); (2) add the
     `com.unity.services.cloudcode` package and write a `SubmitMatchResult(matchId, winnerId, loserId)`
     Cloud Code module (JS, accumulating and comparing both sides' reports under a `match_<matchId>` key
     via the Cloud Save Data API); (3) write `LeaderboardManager` to call it, but to silently fall back to
     the existing direct `AddPlayerScoreAsync` path if it FAILS (not yet deployed/network error) so it
     doesn't break backward compatibility; (4) verify end-to-end with a real two-process ranked match with
     the Unity Editor open (the standard followed in all of this project's multiplayer milestones, see the
     "Multiplayer" section above).

Item 22 (in the RELEASE ROADMAP above) had already noted this as "a priority once revenue starts"; this
audit confirmed the concrete mechanisms (which file/line, exactly how exposed), additionally made
`CharacterAbilities` network-authoritative in this round (the follow-up to item 1), and clarified the
remaining two items (6-7) as backend-dependent with a concrete implementation plan.
