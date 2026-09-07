# UI Design Kit — how the design gets into the game

The visual language of every menu, panel and HUD element comes from the
**CosmicRumble UI Kit** design file (19 artboards: entry flow, main menu, settings,
shop, wardrobe, quests, achievements, leaderboard, social, party/matchmaking,
gameplay HUD, pause, match end, toasts, screen flow, component library).

## Where the design file lives

Not in this public repo — same rule as the store assets. Both artboard files plus
their sprites sit in the private backup repo:

```
C:\Projects\pictures\CosmicRumble\ui-design-kit\
    CosmicRumble UI Kit.dc.html            # portrait artboards (480x960 = 1080x1920)
    CosmicRumble UI Kit Landscape.dc.html  # landscape artboards (960x540 = 1920x1080)
    assets/                                # weapon, planet, costume sprites, TitanOne font
```

**The landscape file is the one that matches the shipping game.** The player build is
landscape-only (`allowedAutorotateToPortrait: 0`) and every canvas uses a 1920x1080
reference resolution, so the portrait artboards are a style reference, not a layout spec.

## How the design is applied in code

The UI is built procedurally (there are no UI prefabs), so the design lives in code:

| Design concept | Code |
| --- | --- |
| Palette, surface colours, type scale, geometry | `Assets/Scripts/UI/UiTheme.cs` |
| Shared visuals (rounded sprite, plate, stroke, gradient, press/pop, close button) | `Assets/Scripts/UI/UiKit.cs` |
| Main menu, drawer, settings | `Assets/Scripts/Menu/MainMenuUI.cs` |
| Panels (shop, wardrobe, quests, …) | `Assets/Scripts/UI/*PanelUI.cs` |
| In-match extras (turn banner, aim readout, pause button, slot numbers) | `Assets/Scripts/UI/MatchHudUI.cs` |
| Weapon tray slot states | `Assets/Scripts/Managers/UIManager.cs` |

**Rule: no screen defines its own colour.** Every panel reads `UiTheme`, so a design
change is a one-file change. `UIManager` re-applies the tray state colours at runtime
so values serialised into `SampleScene` cannot drift away from the design.

### Tap sizes

`UiTheme.MinTouchSize` is 72 design units on a canvas whose short reference side is
1080 — the smallest tappable element the kit uses (34–40px on a 960x540 artboard).
Every interactive element must meet it on its short side; the audit below enforces it.

## Verifying that every button works (PC and mobile)

```
Tools > UI > Run Button Audit (Play Mode)
```

It walks each menu screen, then the match scene and the pause menu, and for every
visible button checks:

1. `onClick` has at least one listener (no dead buttons),
2. the button is interactable and has a raycast-target graphic,
3. its canvas has a `GraphicRaycaster`,
4. a raycast at its centre actually reaches it (nothing invisible on top),
5. it meets `UiTheme.MinTouchSize`.

Reachability goes through `EventSystem.RaycastAll` — the same path a mouse click and a
finger tap take — so a single run covers desktop and touch. Buttons are never actually
clicked: purchases, sign-out and matchmaking must not fire during an audit. The only
exception is the editor-only guest sign-in button, which the audit presses to get past
the login gate.

The report is written to `ui-audit-report.txt` (gitignored). Headless / CI:

```
Unity.exe -batchmode -projectPath <project> -screen-width 1920 -screen-height 1080 \
  -executeMethod CosmicRumble.EditorTools.UiInteractionSelfTest.RunBatch
```

It exits 1 when there are findings. Run it at more than one phone aspect
(1920x1080, 2400x1080, 1600x720) — an element that fits 16:9 can still fall outside a
20:9 screen, and only the raycast check at that resolution will catch it.

Last full run: 181 buttons across the menu, the match HUD and the pause menu — no findings.

## Known gaps against the design

- **Wind indicator** (`WIND ▸ 12` in artboard 16) is not implemented: the game has no
  wind mechanic, and a HUD element showing a value nothing affects would be a lie.
- The portrait artboards' vertical layout is unused; see the orientation note above.
