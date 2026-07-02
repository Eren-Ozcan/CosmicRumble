# CosmicRumble — Backlog

Deferred work identified during the economy/achievement audit and fix pass. Not started unless noted.

## Costumes
- 150 costumes from the master spec (`.claude/commands/CosmicRumble_MasterPrompt.md`, Section 4) are not
  authored — `Assets/Resources/Costumes/` is empty (no `CostumeDefinition` assets, no `CostumeDatabase.asset`).
- `CostumeManager` (`Assets/Scripts/Economy/Costumes/CostumeManager.cs`) is fully coded but not bootstrapped
  into any scene — intentionally skipped for now.
- No costume sprite/art assets exist anywhere in the project.
- `Assets/Editor/CostumeAssetGenerator.cs` costume `displayName`/unlock-description strings (150 tuples) are
  now translated to English, completing the English-language pass for this generator.

## Quests
- Only 3 of the 14 spec'd quests exist (`Assets/Resources/Economy/Quests/`), all daily. Weekly/monthly pools
  are empty, so those tiers can never populate even though `QuestManager.cs` supports them.
- No `QuestPanel` UI exists.

## Audio
- Only one sound file exists in the project (`bomb_Explosion.mp3`). `AudioManager.cs` has no music/SFX
  library beyond menu click/hover clips.

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
    `.unitypackage`, not a clean UPM package). To activate: download the latest release from
    `github.com/playgameservices/play-games-plugin-for-unity`, import it (Assets → Import Package → Custom
    Package), run Window → Google Play Games → Setup → Android Setup with the resource XML from Play Console →
    Play Games Services Configuration, then add `GPGS_INSTALLED` under Player Settings → Scripting Define
    Symbols (Android).

## Multiplayer
Not started (code-wise). **The transport recommendation below has changed** now that a mobile release sharing
the same online system is a stated goal — see "Online backend" below for the full reasoning.

**Cross-play scope (confirmed):** Steam is its own isolated player pool — Steam players never match against
mobile players. Android (Play Store) and iOS (App Store) *do* cross-play with each other, i.e. two matchmaking
pools total: `steam` and `mobile` (Android+iOS combined), not three separate silos.

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
- Still a large, separate future effort: full client/server refactor of `TurnManager`, ability sync, and
  per-ability RPCs for projectile spawning.

## Controls
Done — Move Left / Move Right / Jump plus the 9 ability hotkeys are considered sufficient as-is. No further
rebinding work planned.

## Save / sync — cross-platform online backend
Currently all persistence (`currency.json`, `progress.json`, `unlocks.json`, `achievements_*.json`,
`users.json`) is local JSON in `Application.persistentDataPath`. No cloud save/sync yet.

**Stated goal:** Steam release first (max 8 players/room), each player's progress saved server-side; a mobile
version of the *same project* follows later, sharing the *same* online system/backend — only the storefront
differs. That constraint rules out any storefront-locked save API (Steam Cloud alone, Google Play Saved Games
alone, Game Center alone) as the primary store — those only make sense as an *additional*, platform-specific
convenience layer, mirroring how the achievement providers already sit on top of `LocalAchievementProvider`.

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

**Recommendation:** start with Unity Gaming Services (Authentication + Cloud Save + Relay + Lobby) for both the
Steam launch and the later mobile build — one backend, one integration, no storefront lock-in — and keep
Steamworks/Play Games/Game Center strictly for their store-specific extras (achievements, overlay, rich
presence) exactly as the achievement provider layer already does. Revisit Nakama only if UGS billing becomes a
real concern post-launch.

## Mobile-specific gaps (Steam ships first; these only matter once mobile work starts)
Audited the codebase against a mobile release. Backend (UGS) and the achievement providers above already cover
mobile — everything below is mobile-only work not yet started, not started by priority, just recorded:

- **Input is 100% mouse/keyboard, no touch layer.** Every ability (`Pistol`, `Shotgun`, `Rpg`, `HandGrenade`,
  `BlackHoleSkill`, etc.) and `PlayerController2D` read `Input.mousePosition`/`Input.GetMouseButton` directly —
  no `EnhancedTouch`, no `Application.platform` branching. Drag-to-aim is conceptually touch-friendly but the
  code is hard-wired to mouse. Since `com.unity.inputsystem` is already installed, the fix is to route aiming
  through the new Input System's unified `Pointer`/`Touch` abstraction instead of legacy `Input.*`, so one code
  path serves both mouse and touch — not a parallel mobile-only input system.
- **Ability selection is 100% keyboard, with no UI fallback wired up.** `AbilityBase.OnFireUpdate` selects via
  `Input.GetKeyDown(ActivationKey)` (keys 1–9, 0). The good news: `SetSelected(bool)` is already a public method
  on every ability (`AbilityBase.cs`), so a UI button just needs to call it — but no button currently does.
  `ToggleSkillPanel.cs` only toggles panel visibility, it doesn't trigger abilities. On mobile there is currently
  no way to select an ability at all without a keyboard.
- **No safe-area handling.** `SafeArea`/`safeArea` doesn't appear anywhere in the project. On notched/punch-hole
  phones, HUD elements (health bar, turn timer, ability buttons) can be clipped by or overlap the system UI.
  Needs a SafeArea component plus a Canvas Scaler pass for phone aspect ratios (current UI is presumably only
  tuned for Steam's desktop 16:9/ultrawide).
- **No IAP.** `CurrencyManager` (`Assets/Scripts/Economy/Core/CurrencyManager.cs`) only earns Gold/Gem/XP —
  no `Unity.Purchasing`/`IStoreListener` anywhere in the project. Gem is currently earn-only. If gem is meant to
  be purchasable with real money on mobile, this needs the Unity IAP package plus matching product definitions
  in Play Console and App Store Connect.
- **Store-side setup (account/config work, not code):** Play Console (min API level, Data Safety form, Play
  Games Services resource XML), App Store Connect (App Privacy nutrition label, ATT prompt if ads/analytics are
  added, TestFlight), age rating, privacy policy URL.
