# CosmicRumble — Release Test Plan

Version 1.0 — 2026-09-04. Owner: solo developer. Target: first Android release via Google Play
closed testing, then production.

This plan covers everything from an in-editor smoke check to a 12-tester closed-testing round. It is
written to be executed by one person with two phones and one PC, because that is the actual
situation; anything that genuinely needs more than that is called out as such.

---

## 1. Scope

**In scope:** online multiplayer (session/relay, matchmaking, turn sync, ability and damage
authority, reconnect, host migration once built), the offline hotseat and Training paths, the
economy/progression chain that hangs off a match result, social/friends/invites, mobile platform
behaviour, performance, localization, and store-readiness checks.

**Out of scope:** iOS (blocked, no Mac — see `TODO.md`), Steam build, load/scale testing beyond 8
concurrent players (the game's own maximum lobby size), and server-side Cloud Code validation (not
yet built).

**Definition of done for the release:** every P0 case passes on both test devices, no open Sev-1 or
Sev-2 defects, and the closed-testing round completes 14 days with 12 testers and no Sev-1 reports.

---

## 2. Test environment

### 2.1 Tooling to install before the first pass

Neither of these is in `Packages/manifest.json` today; both are free first-party packages and both
are prerequisites for large parts of this plan.

| Package | Why |
|---|---|
| `com.unity.multiplayer.playmode` (MPPM) | Runs up to 4 players (main editor + 3 virtual players) from one editor against the same assets on disk. This is what makes 3-and-4-player cases (host migration, FFA, team modes) testable at all without four machines. Virtual players support **player tags**, so per-role behaviour (host vs. joiner) can be scripted. |
| `com.unity.multiplayer.tools` (Network Simulator) | Injects latency, jitter and packet loss into the transport in-editor, plus lag spikes and forced disconnects. Effects apply to **editor instances and development builds only** — a release build ignores them, which is exactly what we want. |

Also needed:

- **Clumsy** (Windows) for conditioning the *standalone* DevClient process, which the in-editor
  Network Simulator cannot touch.
- The existing standalone dev build at `Builds/DevClient/CosmicRumble.exe` (gitignored) — still the
  only way to prove two genuinely separate processes, which every past multiplayer milestone used as
  its bar for "verified".

### 2.2 Hardware

| Id | Device | Role |
|---|---|---|
| DEV-PC | Windows 11 dev machine | Editor host, MPPM virtual players, standalone DevClient |
| AND-LOW | Low-end Android (target floor: 3 GB RAM, ~2019 mid-range) | Performance floor, real touch input, real network |
| AND-HI | Current-gen Android | Reference device |

Two physical Android devices are the minimum for the friend/invite two-sided flow, which has
**never** been tested two-sided (roadmap item 2).

### 2.3 Network condition profiles

Used throughout as `NET-P0…P4`. Values follow Unity's own guidance (Boss Room used ~100–150 ms for
desktop, ~200–300 ms and 5–10% loss for mobile) and standard mobile QA practice.

| Profile | Latency (RTT) | Jitter | Packet loss | Meaning |
|---|---|---|---|---|
| P0 Clean | 0 | 0 | 0% | Baseline, must pass first |
| P1 Home broadband | 60 ms | 10 ms | 0.5% | The common case |
| P2 Mobile 4G | 150 ms | 40 ms | 2% | The realistic mobile case |
| P3 Poor mobile | 300 ms | 100 ms | 8% | Bad-but-playable floor |
| P4 Hostile | 500 ms | 200 ms | 15% + 3 s dropouts | Must fail *gracefully*, not silently |

A pass at P3 means: the match remains playable and consistent, turn order never diverges, and no
player is desynced. Cosmetic jitter is acceptable at P3, not at P1.

### 2.4 Evidence rules

Every executed case records: build hash / commit, device, profile, result, and the decisive log line
or screenshot. "It looked fine" is not a result. Host-side and client-side logs are captured for
every online case — the past milestones' habit of quoting both sides' log lines is the standard.

---

## 3. Test levels

1. **L1 — Automated.** `com.unity.test-framework` PlayMode tests using NGO's own
   `NetcodeIntegrationTest` harness for pure logic: turn order rotation, damage arithmetic, trophy
   deltas, snapshot serialise/deserialise round-trips. Cheap, runs on every commit.
2. **L2 — MPPM local.** 2–4 players in one editor. Fast iteration for turn/ability/mode coverage.
3. **L3 — Two-process.** Editor host + standalone DevClient. The bar the project has always used for
   "verified"; catches everything MPPM's shared-process shortcuts hide.
4. **L4 — Real devices, real network.** Two Android phones, one on Wi-Fi and one on cellular.
5. **L5 — Closed testing.** 12 testers, 14 days, Play Console internal/closed track.

A case's level is listed with it. Anything marked L3+ cannot be signed off from MPPM alone.

---

## 4. Test suites

Severity: **Sev-1** blocks release; **Sev-2** blocks the closed-testing round; **Sev-3** ship-with.
Priority: **P0** must run every pass; **P1** every release candidate; **P2** once per milestone.

### 4.1 SES — Session, connection, relay

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| SES-01 | P0 | L2 | Host a private session via friend code | Join code returned, `NetworkManager.IsHost == true`, code shown in UI |
| SES-02 | P0 | L3 | Join by code from a second process | Both clients spawn, scene loads on both |
| SES-03 | P0 | L2 | Cancel while hosting, then host again | `IsListening == false` after cancel, second host succeeds with a *new* code |
| SES-04 | P1 | L3 | Join with a wrong/expired code | Localized error, no hang, UI returns to the lobby |
| SES-05 | P1 | L3 | Host leaves during the lobby wait (before match start) | Client sees a clean "session closed" message, UGS session cleaned up |
| SES-06 | P1 | L4 | App backgrounded in the lobby wait | `LeaveSessionAsync()` fires via `OnApplicationPause`, no orphan session left in UGS |
| SES-07 | P1 | L3 | Two sessions created back to back by the same account | No stale-session error, old one released |
| SES-08 | P2 | L4 | Relay region | Session allocates in the nearest region; a EU-EU match does not route via US |

### 4.2 MM — Matchmaking (Quick Match)

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| MM-01 | P0 | L3 | Quick Match with an empty pool | Becomes host of a new public session, waits, `becameHost=True` in log |
| MM-02 | P0 | L3 | Quick Match with one waiting session | Joins it, `becameHost=False`, **same session code** on both sides, no code ever typed |
| MM-03 | P0 | L2 | Double-tap the Quick Match button | Exactly one matchmaking request (guard added in `fix(ui): guard OnQuickMatchClicked`) |
| MM-04 | P1 | L3 | A private friend-code session must never be matched into | A Quick Match from a third process never joins the `IsPrivate = true` session |
| MM-05 | P1 | L3 | Quick Match timeout with nobody joining | Times out cleanly at the configured limit, session released, UI recoverable |
| MM-06 | P1 | L4 | Cancel Quick Match mid-search | Search stops, session torn down, no ghost entry in the public pool |
| MM-07 | P2 | L4 | Two devices Quick Match simultaneously | They find each other rather than each creating a session (race window) |

### 4.3 TURN — Turn synchronisation

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| TURN-01 | P0 | L3 | Turn passes host → client → host | `isActive` and `IsOwner` flip correctly on both sides, same order, no double-activation |
| TURN-02 | P0 | L2 | Turn timer expiry | Turn passes automatically; both sides agree on who is next |
| TURN-03 | P0 | L3 | `RequestEndTurn` from a non-host client | Server honours it; the client cannot end *someone else's* turn |
| TURN-04 | P1 | L3 | Turn ends while a projectile is in flight | Turn does not advance until the projectile resolves (`NotifyProjectileLaunched`) |
| TURN-05 | P1 | L2 | Turn counter replication | Client HUD turn number matches the host's at all times |
| TURN-06 | P1 | L3 @P3 | Turn sync under 300 ms / 8% loss | Order never diverges; timer drift under one second |
| TURN-07 | P1 | L2 | 8-player FFA full rotation | Every player gets exactly one turn per round, in a stable order |
| TURN-08 | P2 | L2 | A player dies on their own turn | Turn advances correctly, dead player skipped in later rounds |

### 4.4 ABL — Abilities and damage authority

Run each of the nine abilities (Pistol, Shotgun, RPG, Grenade, Bomb, BlackHole, Teleport, Shield,
BatHammer, SuperJump) through ABL-01..04.

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| ABL-01 | P0 | L3 | Non-host client fires; host executes | Host log shows the spawn with `IsServer=True owner=<clientId>`; projectile visible on both |
| ABL-02 | P0 | L3 | Damage applied exactly once | Health decrements once, not twice; host and client health bars agree |
| ABL-03 | P0 | L3 | Knockback/force on a remote-owned character | Force actually lands (targeted `ClientRpc` path in `GravityBody.ApplyForce`), not silently overwritten |
| ABL-04 | P1 | L3 | Teleport of a client-owned character | Final position sticks on the owner's machine and matches on the host |
| ABL-05 | P1 | L3 | Shield blocks damage online | `isShielded` `NetworkVariable` respected server-side; damage prevented on both views |
| ABL-06 | P1 | L2 | Terrain destruction converges | Crater identical on host and client after an RPG/Bomb hit |
| ABL-07 | P1 | L3 @P2 | Fire under 150 ms latency | No double-fire, no lost shot, aim line matches the actual trajectory |
| ABL-08 | P2 | L3 | Two abilities fired in the same frame by different players | Impossible by design (turn gate) — verify the gate actually rejects it |
| ABL-09 | P2 | L2 | Offline hotseat regression | Every ability still works offline; `Rigidbody2D` is Dynamic (the `NetworkRigidbody2D` kinematic regression) |

### 4.5 POS — Position and visual sync

This is the project's known *unproven* claim: position sync was only inferred, never watched
side-by-side over time.

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| POS-01 | P0 | L4 | Two devices side by side, 60 s of continuous movement, recorded on video | No rubber-banding, no teleport-back, positions agree within a character width |
| POS-02 | P0 | L4 @P2 | Same, at mobile latency | Remote character motion is smooth; correction hitches are not visible as jumps |
| POS-03 | P1 | L4 @P3 | Same, at poor mobile | Degradation is gradual; no permanent divergence after the burst ends |
| POS-04 | P1 | L3 | Character on a different planet than the camera's owner | Gravity orientation (`transform.up`) matches on both sides |
| POS-05 | P2 | L4 | Name tag and team colour on remote players | Correct name (not `Player_1`), correct team colour, tag stays upright |

### 4.6 REC — Reconnect (existing, Milestone 4)

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| REC-01 | P0 | L3 | Client killed mid-match, relaunched with the same code inside 90 s | Reclaims its **own** character via `ChangeOwnership`, no duplicate spawn |
| REC-02 | P0 | L3 | Client drops and never returns | Character despawns at the 90 s timeout; match ends with a correct winner |
| REC-03 | P1 | L4 | Phone loses Wi-Fi for 20 s and regains it | Auto-rejoin succeeds within the 6 × 5 s retry budget; status banner shown then hidden |
| REC-04 | P1 | L4 | Wi-Fi → cellular handoff mid-match | Reconnects on the new interface; the match continues |
| REC-05 | P1 | L4 | App backgrounded mid-match for 60 s (call, lock screen) | Reconnect on resume, or a clear loss message — never a frozen screen |
| REC-06 | P1 | L3 | The *wrong* player tries to claim an orphaned character | Refused; identity verified by UGS PlayerId |
| REC-07 | P2 | L3 | Reconnect while it is the reconnecting player's own turn | Turn state correct on return; timer not already expired against them |
| REC-08 | P2 | L4 | Airplane mode toggled on and off | Same as REC-03, plus no duplicate session membership left behind |

### 4.7 HM — Host migration (new, see `docs/HOST_MIGRATION_PLAN.md`)

Requires 3+ players, so MPPM (up to 4) is the practical harness; at least one full pass must be L4.

| Id | Pri | Lvl | Phase | Case | Expected |
|---|---|---|---|---|---|
| HM-01 | P0 | L2 | 1 | Host process killed in a 3-player match | Lobby elects a new host, `SessionHostChanged` fires on all survivors |
| HM-02 | P0 | L2 | 1 | New host starts the network | New Relay allocation created, `NetworkManager.IsHost == true` on the new host only |
| HM-03 | P0 | L2 | 1 | Remaining clients rejoin | `MigrateClientNetworkAsync` reconnects them automatically; `SessionMigrated` fires |
| HM-04 | P0 | L2 | 1 | Relay region preserved | The new allocation is in the same region as the old one |
| HM-05 | P0 | L2 | 2 | Match state after migration | Health, positions, team ids and alive/dead flags match the pre-migration snapshot |
| HM-06 | P0 | L2 | 2 | Turn order after migration | Same order, same active player (or the next one if the leaver was active); turn number continuous |
| HM-07 | P1 | L2 | 2 | Ownership after migration | Every surviving player controls their **own** character; no one gains control of another |
| HM-08 | P1 | L2 | 2 | Host leaves on their own turn | Turn skips to the next player; nobody is stuck waiting on a departed player |
| HM-09 | P1 | L2 | 2 | In-flight projectile at migration time | Cancelled cleanly; the interrupted turn restarts; no orphan `NetworkObject` errors |
| HM-10 | P1 | L2 | 3 | Planet destruction after migration | Craters identical to pre-migration on every client |
| HM-11 | P0 | L2 | 4 | **1v1 host leaves** | **No migration** — survivor wins by forfeit, exactly as today |
| HM-12 | P1 | L2 | 4 | Turn timer during migration | Frozen for the migration window plus grace; nobody loses a turn to it. See HM-22 for the detection gap that precedes it |
| HM-13 | P1 | L2 | 4 | Migration fails (simulate: kill the elected host mid-migration) | Match ends gracefully, **no trophy change for anyone**, no frozen clients |
| HM-14 | P1 | L2 | 4 | Two hosts leave in a row | Second migration works from the state the first produced |
| HM-15 | P1 | L2 | 4 | Ranked forfeit on deliberate host quit | The quitter loses trophies; survivors are not punished |
| HM-16 | P1 | L2 | 4 | Quests/achievements across a migration | Events do not double-fire; a kill counted before migration is not recounted |
| HM-17 | P1 | L4 | 4 | Full migration on real devices, real network | End-to-end pass at P2; visible downtime under ~10 s with a clear status banner |
| HM-18 | P2 | L2 | 4 | Snapshot size | Serialised snapshot stays comfortably inside the Lobby value size limit at 8 players |
| HM-19 | P2 | L2 | 4 | Localized banner | "Host changed, reconnecting…" correct in all 7 languages, including CJK |
| HM-20 | P0 | L2 | 1 | **Detection latency measurement** | Kill the host process and timestamp: (a) client `OnClientDisconnectCallback`, (b) `SessionHostChanged`, (c) `SessionMigrated`. The Lobby host-timeout gap (a→b) is unmeasured today and is not instant — the PUN equivalent is ~10 s. Record the real number; total visible downtime is (a→c). |
| HM-21 | P0 | L2 | 1 | **Banner appears during the detection gap** | The status banner shows the moment the *client's own* connection drops (a), not when the Lobby finally elects a new host (b). Otherwise players stare at a frozen match for the whole gap with no explanation. |
| HM-22 | P1 | L2 | 4 | Turn timer frozen across the *whole* gap | The timer stops at (a), not at (b) — freezing only for the migration itself still burns the detection gap off the active player's turn. |

### 4.8 ECO — Economy, trophies, progression after a match

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| ECO-01 | P0 | L3 | Ranked win/loss | Trophies +30 / −20, applied once, reflected on the leaderboard |
| ECO-02 | P0 | L3 | Friendly (code-invite) match | **No** trophy change on either side |
| ECO-03 | P0 | L2 | XP persists without a level-up | `totalXP` saved on every gain (regression for `fix(economy): persist totalXP`) |
| ECO-04 | P1 | L4 | Account switch | Trophy cache is per-account; no leakage between accounts (`fix(leaderboard): scope trophy cache`) |
| ECO-05 | P1 | L4 | Cloud save round trip | Currency/progress/costumes/avatar pull correctly on a second device |
| ECO-06 | P1 | L4 | Quest progress from an online match | Correct quest ticks; weekly reset lands on Monday |
| ECO-07 | P1 | L4 | IAP purchase, redelivered pending order | Gems granted exactly once (`fix(iap): dedupe redelivered pending orders`) |
| ECO-08 | P1 | L4 | Two overlapping purchase taps | Second request rejected (`fix(iap): reject overlapping purchase requests`) |
| ECO-09 | P2 | L4 | Play the match offline-ish (UGS unreachable) | "Playing offline" message; local progress preserved and pushed later |

### 4.9 SOC — Friends, invites, presence

The two-sided flow has never been tested. This is roadmap item 2 and needs two real accounts.

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| SOC-01 | P0 | L4 | Send a friend request from A, accept on B | Both sides show the friendship; no duplicate request |
| SOC-02 | P0 | L4 | Double-tap "Add" | Exactly one request (`fix(ui): guard OnAddClicked`) |
| SOC-03 | P0 | L4 | Presence | B sees A go online/offline within a reasonable delay |
| SOC-04 | P0 | L4 | Invite → private match → match end | Invite popup appears on B, accepting joins A's private session, match runs and ends correctly on both |
| SOC-05 | P1 | L4 | Invite declined / expired | A gets a clear result; no orphan session |
| SOC-06 | P1 | L4 | Invite while B is already in a match | Handled gracefully, not dropped silently |
| SOC-07 | P1 | L4 | Party lobby: 4-player team mode assembled from friends | Correct teams, correct `MaxPlayers`, everyone lands in the same match |
| SOC-08 | P2 | L4 | Remove a friend mid-session | No crash; presence updates |

### 4.10 MOD — Game modes

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| MOD-01 | P0 | L2 | 1v1 | 2 players, correct win condition |
| MOD-02 | P1 | L2 | FFA at 3 and at 8 | `MaxPlayers` matches `ResolveTotalPlayers`; last player standing wins |
| MOD-03 | P1 | L2 | 2v2 | Teams correct, friendly fire behaviour as designed, team win condition |
| MOD-04 | P2 | L2 | 3v3, 4v4, 2v2v2v2 | Same, at the larger sizes; 8-player lobby fills |
| MOD-05 | P1 | L2 | A team is eliminated | Match ends immediately for the right side |

### 4.11 MOB — Mobile platform behaviour

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| MOB-01 | P0 | L4 | First real device build (never done — roadmap item 3) | App installs, signs in, reaches the menu, plays a match |
| MOB-02 | P0 | L4 | Google Play Games sign-in on a real device | Silent sign-in works; explicit sign-in has a timeout (`fix(auth)`), no crash on cancel |
| MOB-03 | P0 | L4 | Touch controls | Move, aim-drag, fire, skill panel all usable on AND-LOW's screen size |
| MOB-04 | P1 | L4 | Interruptions: call, lock, notification shade, app switch | Resume works; audio resumes; see REC-05 for mid-match |
| MOB-05 | P1 | L4 | Rotation / notch / safe area | UI is not clipped on a notched device |
| MOB-06 | P1 | L4 | Battery and thermal over a 15-minute session | No thermal throttling to unplayable framerate on AND-LOW |
| MOB-07 | P1 | L4 | Cold start time | Under an acceptable threshold on AND-LOW; loading screen never appears frozen |
| MOB-08 | P2 | L4 | Storage: cache cleared / app data wiped | Cloud pull restores the account; no crash on an empty local state |
| MOB-09 | P2 | L4 | Local notifications | Fire correctly, deep-link back into the app |

### 4.12 PERF — Performance

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| PERF-01 | P0 | L4 | Sustained FPS in an 8-player FFA on AND-LOW | Stays above the target floor; no sub-20 dips |
| PERF-02 | P0 | L2 | Terrain explosion cost | ~7–10 ms per explosion (the optimised path), not the old ~60–87 ms |
| PERF-03 | P1 | L2 | GC allocation per turn | No per-frame allocation in `FixedUpdate`; profiler shows a flat GC line during idle turns |
| PERF-04 | P1 | L4 | Bandwidth per match | Measured with the Netcode profiler; sane for a mobile data cap |
| PERF-05 | P2 | L2 | Memory over 10 consecutive matches | No steady growth (no leak in spawner/turn manager subscriptions) |

### 4.13 SEC — Security and anti-cheat (known-gap verification, not a fix)

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| SEC-01 | P0 | L3 | Client-side damage attempt | A non-host client cannot apply damage directly; server authority holds |
| SEC-02 | P0 | L3 | Acting out of turn | Rejected by the `TurnManager` gate |
| SEC-03 | P1 | L2 | Tampered local save file | HMAC check rejects it |
| SEC-04 | P1 | — | **Known open:** host-side trophy inflation | Documented, not fixed; must not regress further. Blocks nothing pre-launch but is tracked as the top post-launch security item |

### 4.14 LOC / UI

| Id | Pri | Lvl | Case | Expected |
|---|---|---|---|---|
| LOC-01 | P1 | L4 | All 7 languages | No missing keys, no clipped strings, CJK font renders |
| LOC-02 | P1 | L4 | Language switch mid-session | UI updates without a restart |
| UI-01 | P0 | L2 | Double-tap every navigation button | No duplicate panels or double scene loads (`SceneFader` re-entrancy guard) |
| UI-02 | P1 | L4 | Legal links | Privacy Policy and Terms open the real hosted URLs (blocked until the URLs exist) |

### 4.15 STORE — Release readiness

| Id | Pri | Case | Expected |
|---|---|---|---|
| STORE-01 | P0 | Signed AAB builds and installs from the Play Console internal track | Clean install and upgrade path |
| STORE-02 | P0 | Real IAP SKUs (`gem_pack_100..6000`) purchasable in the closed track | Real purchase flow, real receipt, gems granted once |
| STORE-03 | P0 | Data Safety form matches what the app actually collects | UGS auth, cloud save, analytics all declared |
| STORE-04 | P0 | Content rating questionnaire submitted | Rating issued |
| STORE-05 | P1 | Achievement IDs mapped to Play Games | The 50 achievements resolve to real `CgkI...` ids |
| STORE-06 | P1 | Store listing assets present | Icon, feature graphic, screenshots — from the private assets repo, never this one |

---

## 5. Adverse-condition pass

Once the functional suites are green at P0, re-run this subset at each profile. This is where
multiplayer bugs actually live.

| Profile | Subset to re-run |
|---|---|
| P1 | TURN-01/02/04, ABL-01/02, POS-01, REC-01 |
| P2 | The P1 subset + MM-02, HM-05/06, POS-02, REC-03/04 |
| P3 | The P2 subset + HM-17, POS-03 |
| P4 | Degradation only: no data corruption, no desync that survives recovery, clear user-facing messaging |

Additional deliberate-failure cases at every profile: kill the process, kill the network interface,
suspend the process (simulating a thermal/OS freeze), and drop packets in a burst rather than
uniformly (uniform loss is the easy case; bursts are what real mobile networks do).

---

## 6. Regression smoke suite

Runs before every commit that touches networking, and before every build handed to a tester.
Roughly 20 minutes by hand:

SES-01, SES-02, MM-01, MM-02, TURN-01, TURN-02, ABL-01, ABL-02, POS-01, REC-01, ECO-01, ECO-02,
UI-01, MOB-01, and — once host migration lands — HM-01 through HM-06 and HM-11.

Everything in L1 (automated) runs on every commit regardless.

---

## 7. Defect handling

Report template:

```
Id / Title
Severity (1-3) / Suite id
Build: <commit sha>  Device: <DEV-PC | AND-LOW | AND-HI>  Profile: <P0..P4>
Steps (numbered, from a cold start)
Expected / Actual
Evidence: host log line, client log line, screenshot or video
Frequency: always / intermittent (n of m)
```

Triage rule: a Sev-1 halts the current suite and is fixed before continuing — a networking Sev-1
usually invalidates the results of everything run after it.

---

## 8. Execution order

The order matters, because later phases are expensive and depend on earlier ones being trustworthy.

1. **Wave 0 — tooling.** Install MPPM and Multiplayer Tools, install Clumsy, rebuild the DevClient,
   write the L1 automated tests for turn order and trophy deltas.
2. **Wave 1 — regain baseline.** Run the smoke suite at P0. Nothing below is meaningful until this
   is green; a lot of code has changed since the last full multiplayer pass in July.
3. **Wave 2 — close the two known gaps.** POS-01..03 (the unproven position-sync claim) and
   SOC-01..07 (the never-tested two-sided friend flow). Both need two real devices.
4. **Wave 3 — first Android build.** MOB-01..03 in full; this is roadmap item 3 and will surface
   platform issues that invalidate assumptions everywhere else.
5. **Wave 4 — host migration**, phase by phase, each phase's HM cases green before the next starts.
6. **Wave 5 — adverse conditions** (section 5) across the whole functional set.
7. **Wave 6 — store readiness** (STORE-\*) and the 12-tester / 14-day closed round, with the smoke
   suite re-run on each build pushed to testers.

---

## 9. References

- Unity Docs — Migrate session host: <https://docs.unity.com/en-us/mps-sdk/session-host-migration>
- Unity Docs — Session events: <https://docs.unity.com/en-us/mps-sdk/session-events>
- Multiplayer Services changelog (host migration added in 1.2.0):
  <https://docs.unity3d.com/Packages/com.unity.services.multiplayer@2.2/changelog/CHANGELOG.html>
- NGO — Testing with artificial network conditions:
  <https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.11/manual/tutorials/testing/testing_with_artificial_conditions.html>
- NGO — Testing multiplayer games locally:
  <https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/manual/tutorials/testing/testing_locally.html>
- Multiplayer Tools — Network Simulator:
  <https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@2.2/manual/network-simulator.html>
- Multiplayer Play Mode: <https://mp-docs.dl.it.unity3d.com/mppm/current/about/>
