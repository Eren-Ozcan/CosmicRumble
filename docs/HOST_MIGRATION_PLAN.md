# Host Migration — feasibility study and implementation plan

Status: **phases 1-4 implemented, not yet play-tested.** Written 2026-09-04 after reading the installed
`com.unity.services.multiplayer@2.2.4` source in `Library/PackageCache/` and the current Unity
Multiplayer Services documentation.

Until now the project's stated position was "host migration is out of scope — if the host drops, the
whole session ends." That is no longer acceptable: in every mode above 1v1 (FFA up to 8, 2v2, 3v3,
4v4, 2v2v2v2) a single player's connection loss currently destroys the match for everyone else,
including the trophies and quest progress of players who did nothing wrong.

## 1. What the SDK already does for us

Host migration is **not** something we have to build from nothing. `com.unity.services.multiplayer`
2.2.4 already ships the transport half of it. Verified by reading the package source, not inferred
from docs:

| Step | Where | What happens |
|---|---|---|
| 1 | UGS Lobby | The host leaves or times out; the Lobby service assigns a new host id. |
| 2 | `SessionHandler.OnLobbyHostChanged` | Fires `ISession.SessionHostChanged(newHostId)`. |
| 3 | `NetworkModule.OnSessionHostChanged` | `await m_Session.ReconnectAsync()`, then branches on who we are. |
| 4a | `NetworkModule.MigrateHostNetworkAsync` (new host only) | `ResetAsync()` tears down the old NGO/Relay connection, `ApplyMigrationDataAsync()` hands the last snapshot to **our** handler, `StartRelayNetworkAsync(...)` allocates a **new** Relay allocation and starts `NetworkManager` as host, `SavePropertiesAsync()` publishes the new connection metadata, `SessionMigrated` fires. |
| 4b | `NetworkModule.MigrateClientNetworkAsync` (everyone else) | Sees the changed network metadata, `ResetAsync()` + `JoinNetworkAsync(metadata)` — reconnects to the new host's Relay allocation automatically. |
| 5 | `HostMigrationHandler` | While a match runs, the current host periodically calls `IMigrationDataHandler.Generate()` and uploads the bytes to the Lobby (skipped while `PlayerCount < 2`). |

Enabling it is one call on the options object:

```csharp
var options = new SessionOptions { MaxPlayers = totalPlayers, IsPrivate = true }
    .WithRelayNetwork()
    .WithHostMigration(new CosmicRumbleMigrationDataHandler(),
                       dataUploadInterval: TimeSpan.FromSeconds(5),
                       dataHandlingTimeout: TimeSpan.FromSeconds(10));
```

`RelayOptions.preserveRegion` controls whether the new allocation stays in the old region — the
SDK already passes the previous region through, so migration does not silently move an EU match to
a US relay.

## 2. What is not provided, and is therefore our work

`IMigrationDataHandler` has exactly two members:

```csharp
public byte[] Generate();          // called periodically on the current host
public void   Apply(byte[] data);  // called once on the newly elected host
```

A default implementation exists **only for Netcode for Entities** (`EntitiesMigrationDataHandler`,
requires Netcode for Entities 1.7.0+). For Netcode for GameObjects in a client-server topology there
is no default: NGO does not snapshot `NetworkVariable`/`NetworkObject` state for us. Serialising and
restoring the match is entirely on us.

**The single most important ordering fact:** `Apply()` runs *between* `ResetAsync()` and
`StartRelayNetworkAsync()` — that is, while `NetworkManager` is **stopped**. Nothing can be spawned,
no `NetworkVariable` can be written from inside `Apply()`. `Apply()` must only decode the bytes and
stash them; the real reconstruction happens afterwards, once we are host and clients have rejoined.

## 3. Snapshot contents for CosmicRumble

The snapshot must be small (it goes into a Lobby data field, so keep it well under the Lobby value
size limit — binary and compact, not JSON) and must be enough to rebuild a match from a cold start.

Required:

- **Match config:** `GameModeType`, `FfaPlayerCount`, `IsRankedMatch`, map/planet layout id.
- **Turn state:** turn order (list of UGS PlayerIds), index of the active player, turn number,
  remaining turn time.
- **Per player:** UGS PlayerId, display name, team id, health, `isShielded`, position, `transform.up`,
  velocity, remaining super-jump charges, per-ability cooldowns/ammo, alive/orphaned flag.
- **Planet state:** `DestructiblePlanet`'s destruction mask. This is the heavy field and needs a
  compact encoding (RLE over the carved-hole list, or the list of explosion `(center, radius)` events
  replayed on the new host — the event list is almost certainly smaller and is deterministic).

Deliberately **not** carried over:

- In-flight projectiles. Simplest correct behaviour: cancel them and **restart the interrupted turn**
  for whoever owned it (or skip to the next player if the leaving host owned it). Trying to resume a
  projectile mid-flight across a host change is not worth the complexity.
- Camera state, VFX, audio — cosmetic, rebuilt naturally.

## 4. Reconnection and identity

`NetworkPlayerSpawner` already has the machinery this needs, built for Milestone 4's mid-match
reconnect: `NetworkIdentityRegistry` reports each client's UGS `PlayerId` to the server, and orphaned
characters are reclaimed by `NetworkObject.ChangeOwnership`. Host migration reuses the same identity
path — after the new host starts, each rejoining client reports its PlayerId, and the spawner matches
it against the snapshot's player list to decide which character to hand it.

NGO `NetworkObjectId`s are **not** stable across a migration. The snapshot must key everything by
UGS PlayerId, never by `clientId` or `NetworkObjectId`.

## 5. Rules and edge cases to decide up front

1. **1v1 is not a migration case.** With two players, the host leaving means one player remains —
   that is a forfeit win for the survivor, exactly as it works today. Migration must be skipped (the
   SDK already stops uploading snapshots below 2 players). Only 3+ player modes migrate.
2. **Deliberate quit vs. crash** both surface as the same Lobby host change; no separate path, but a
   deliberate quit should still count as a forfeit for the quitter in ranked.
3. **The detection gap comes before the migration, and it is not free.** The Lobby does not know the
   host is gone until its heartbeat times out, so there is a dead window between the host actually
   dropping and `SessionHostChanged` firing. The equivalent window in Photon PUN — the same
   architecture, one client holding authority while the service only relays — is around 10 seconds,
   and Photon's own advice is to detect it yourself rather than wait for the service. Our real number
   is unmeasured; HM-20 in the test plan exists to measure it. Two consequences:
   - **Freeze the turn timer at the moment the local client's own connection drops**
     (`OnClientDisconnectCallback`), not when `SessionHostChanged` finally arrives — otherwise the
     detection gap is billed to the active player's turn.
   - Freeze stays on through migration, plus a grace period after `SessionMigrated`.
4. **UI**: reuse `NetworkBootstrap`'s existing persistent status banner ("Host changed, reconnecting…")
   — it already survives scene loads and is exactly the right surface for this. Show it on the local
   disconnect, i.e. at the start of the detection gap, not at `SessionHostChanged`; otherwise players
   watch a frozen match for the whole gap with no explanation.
5. **Migration failure** (`NetworkModule.MigrationFailed`) must end the match gracefully with **no
   trophy change for anyone**, never leave clients frozen on a dead connection.
6. **Ranked integrity**: a migration hands the server role to a different client, which widens the
   existing host-trust problem (see the security section of `TODO.md`). Migration should be shipped
   together with — or at least before — the Cloud Code dual-attestation trophy fix, otherwise a
   cheater can gain the server role deliberately by outlasting the host.

## 6. Phased plan

Progress as of 2026-09-04: phases 1, 2 and 3 are written and compile clean (headless editor
compile, zero errors); none has been play-tested (HM-01 attempted same day — blocked on the
standalone DevClient build not having a no-account login path, see below). Phase 1 needed one
unrelated fix first — Multiplayer Play Mode 1.5.0 broke editor compilation outright and was
bumped to 1.6.3.

- **Phase 1 — plumbing, no state.** Add `.WithHostMigration(...)` with a stub handler that
  serialises only the match config and turn order. Verify the transport half end-to-end: 3 processes,
  kill the host, confirm a new host is elected, a new Relay allocation is made, and the remaining two
  clients rejoin it. This alone proves the SDK path works before any game state is on the line.
- **Phase 2 — full snapshot.** Player state + turn state; match resumes with correct health,
  positions and turn order. Planet destruction still reset.
- **Phase 3 — planet destruction replay via the explosion-event list.** Implemented: `TurnManager`
  now records every `(planetIndex, pos, radius)` it broadcasts in `_explosionHistory` (host-only,
  force is deliberately dropped on replay — only the migration/reconnect target's own client needs
  to *not* get re-launched by forces that already resolved on the old host). `HostMigrationSnapshot`
  carries this list (bumped to snapshot v3); `NetworkPlayerSpawner.RebuildWhenReady` seeds the new
  host's history from it and broadcasts a `ReplayExplosionHistoryClientRpc` to everyone currently
  connected right after the rebuild. This turned out to be the same gap as **plain mid-match
  reconnect** (Milestone 4, not migration-specific): any client that reloads the Game scene locally
  — crash + relaunch, or becoming the new host — gets a pristine `DestructiblePlanet` from
  `Start()`, so `TryResolveClaim` (the existing reconnect-ownership path) now also calls a
  single-target `TurnManager.ReplayExplosionHistoryTo(clientId)` right after `ChangeOwnership`, for
  a reconnecting client that missed the broadcast entirely. Not yet live-tested (needs HM-09/HM-10).
- **Phase 4 — polish.** Implemented:
  - **Timer freeze/grace**: the countdown was already implicitly frozen for the whole detection +
    migration gap (nothing decrements `turnTimer` while no server is running; `ResumeMatchAfterMigration`
    restores the snapshot's remaining time verbatim). Added an explicit `migrationGraceSeconds` (3s
    default) window on top, starting at `ResumeMatchAfterMigration` — `TurnManager.Update` now gates
    the countdown on `Time.time >= _migrationGraceUntil` in addition to `!ProjectileInFlight` (HM-12/HM-22).
  - **Status banner text (7 languages)**: already fully covered in `LocStrings.cs` for every HM
    banner string used by `NetworkBootstrap`/`NetworkPlayerSpawner` (Turkish, Chinese Simplified,
    Spanish, Japanese, Korean, German + English source) — verified, no gaps found (HM-19).
  - **Failure fallback**: `WaitForHostMigrationAsync` timing out now also clears
    `_migrationInProgress` before falling back to the manual rejoin loop, so no flag is left stuck
    "migration in progress" forever. No trophy RPC exists on this path (trophies only change via
    `AnnounceMatchResultClientRpc` on a real game-over), so migration failure structurally cannot
    change anyone's trophies (HM-13).
  - **Ranked forfeit rules**: `NetworkBootstrap.LeaveSessionAsync` now reports a forfeit loss
    (`LeaderboardManager.ReportOnlineMatchResult(false)`) when the local player deliberately leaves
    a ranked match that is still in progress (`TurnManager.IsMatchInProgress`, new). Note: in the
    current design ranked is *always* 1v1 (`QuickMatchAsync`, `MaxPlayers = 2`) and host migration is
    *never* enabled for ranked (`HostSessionAsync`'s migration path is friend-code only, always
    friendly) — so "ranked forfeit on deliberate host quit" reduces to the 1v1 deliberate-leave case;
    only the leaver is penalized, survivors' trophies are untouched (HM-15).
  - **Quest/achievement double-fire fix**: found and fixed a real bug — `TurnManager.ReplayExplosionHistoryClientRpc`
    (run on the new host and on any reconnecting client whose `DestructiblePlanet` reloaded pristine)
    was replaying past explosions through the same `ApplyExplosionNow` path as a live explosion, so a
    planet that a player had already seen destroyed pre-migration would re-fire
    `AchievementEvents.FirePlanetDestroyed()` and the SFX a second time on that player's machine.
    `DestructiblePlanet.ApplyExplosionNow`/`ExplodeVisual` now take an `isReplay` flag; replay calls
    pass `isReplay: true` and skip the achievement/audio fire while still redrawing the crater (HM-16).

Testing for every phase is defined in `docs/TEST_PLAN.md`, section HM.

## 7. Live results, 2026-09-06 — the SDK path does not carry a crashed host

Three standalone DevClient processes (`-autohost 3` + two `-autojoin`), match started, then the
host process hard-killed with `taskkill /F`. Runs `hm10`–`hm12`, logs kept out of the repo.

**HM-20 measured at last: `SessionHostChanged` fires 108 seconds after the local disconnect.**
Both surviving clients reported the same figure (108.1 s and 108.2 s) and the same elected host id.
That is not the ~10 s Photon-equivalent the plan assumed — it is the Lobby's own host-inactivity
timeout, and it is an order of magnitude too long to sit behind a "reconnecting…" banner.

**`SessionMigrated` never arrived at all**, not even 42 s after the host change, with no error or
exception from the SDK on either client. So for a *crashed* host the SDK gives us host election and
nothing else: the elected host never re-hosts, so `MigrateClientNetworkAsync` has nothing to join.
A deliberate quit (a graceful `LeaveSessionAsync`, which removes the member immediately instead of
waiting out the timeout) is a different path and is not covered by these runs.

Two defects fell out of the same runs and are fixed (see the commits):

- `ReconnectAsync()` succeeds on the lobby membership even when no host process exists, so the
  client logged "Reconnect succeeded" and then sat in a frozen match forever. The transport, not the
  membership, is now the criterion (`WaitForTransportAsync`).
- With the network gone, `TurnManager.Update` fell into the offline hotseat branch and every
  survivor declared *itself* the winner, rewards included (`_wasOnline` freeze).

**Resulting behaviour, verified in `hm12`:** host dies → both survivors freeze the match, banner
shown, migration waited out, three rejoin attempts each proven against the transport, then a clean
return to the menu inside the 60 s downtime budget. No false win, no rewards granted, no exceptions,
no frozen client.

**What this means for the release.** Host migration cannot be advertised as working for a crashed
host on SDK 2.2.4 as wired here. The shipped behaviour is the graceful degradation above. Remaining
options, in the order worth trying: (a) test the deliberate-quit path, which may migrate correctly
and is the common case for a player leaving a friendly match; (b) drive the election ourselves once
we detect the drop locally, rather than waiting on the Lobby timeout, and have the elected client
re-host from the snapshot we already serialise correctly (the snapshot half is done and self-tested);
(c) accept the degradation for the first release and revisit after the SDK's next version.
