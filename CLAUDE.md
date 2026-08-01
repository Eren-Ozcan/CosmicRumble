# CosmicRumble

## Project
2D multi-planet turn-based combat game (Worms + Crazy Planets style).

- Engine: Unity 2D, C#
- Physics: Custom gravity — Unity's default gravity (`Physics2D.gravity`) is DISABLED
- Character: 360° surface movement, vectorial attraction
- Weapons/Abilities: Pistol, RPG, Shotgun, Grenade, BlackHole, Teleport, Shield, BatHammer, Bomb
- Multiplayer: not integrated yet — see `TODO.md` for the recommended approach

## Folder Structure
```
Assets/Scripts/
├── Gravity/      → GravitySource, GravityBody, GravityManager
├── Character/    → PlayerController2D, CharacterHealth, CharacterAbilities
├── Abilities/    → IAbility + all abilities
├── Projectile/   → ProjectileBase hierarchy, TrajectoryPredictor
├── Managers/     → TurnManager, UIManager
├── Planet/       → DestructiblePlanet, BombExplosion
└── UI/           → HealthBarUI, TurnTimerUI, ToggleSkillPanel
```

## Critical Rules (Non-Negotiable)
1. Unity default gravity (`Physics2D.gravity`) is never enabled.
2. Velocity is never set directly — use `AddForce`.
3. Every ability must implement `IAbility`.
4. `FixedUpdate` → physics, `Update` → input/UI.
5. No action may be taken without `TurnManager` approval.
6. Don't say "done" before tests pass.

## Slash Commands
- `/analyze [system|file]` — analyzes the specified system (gravity, turn, trajectory ...) or file in depth, maps the data flow, suggests improvements.
- `/review [file]` — performs a 5-lens code review covering physics, architecture, performance, Unity usage and gameplay.
- `/optimize [file|system]` — reports performance (FPS, GC, object pool) and code quality (SOLID, refactor opportunities) analysis with P0/P1/P2 priorities.
- `/commit` — analyzes staged changes and suggests a semantic commit message.

## Backlog
For deferred work (costumes, quest content, audio content, multiplayer, full key rebinding, cloud save, etc.) see `TODO.md`.

## Store / Marketing Assets

Marketing assets such as store listings, feature graphics, icons and screenshots are
**never committed to this public repo**. They are saved in two places:

1. Local, gitignored copy: `docs/store-assets-originals/`.
2. Private backup repo: `C:\Projects\pictures\CosmicRumble\` (local clone of the private
   `Eren-Ozcan/pictures` repo) — copy them there and commit+push in that repo.

## Studio-wide Information

For studio-wide questions (not specific to this game) such as the Google account, the
Play Console developer account, or the status of yilkgames.com/yilkgames_web,
`C:\Projects\pictures\STUDIO.md` is the single source of truth — it is not repeated here.

## Commit Habits
Committing matters — the user wants their GitHub profile to look active/busy. Accordingly:
- Commit separately after each meaningful step (if a single dev session contains multiple work
  items, commit each item as you finish it — don't pile everything up into one huge commit at the end).
- Even if you notice a small, independent fix/improvement, record it as its own commit;
  don't bury it inside a large piece of work and lose it.
- Even so, every commit must represent a real, working state (clean build, tested/play-tested when
  possible) — committing often does not mean "commit half-finished/broken code".
- Don't forget to push — local-only commits don't show up on the profile.
