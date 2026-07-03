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
- **Left open, not done:** `achievements_<username>.json` still isn't synced to Cloud Save (see the
  `CloudSaveManager` note above). Now that usernames are real, globally-unique, cross-device UGS identities
  (not just an arbitrary local string), this would be safer to revisit than before — but it's still a
  separate task, not done as part of this pass.

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
