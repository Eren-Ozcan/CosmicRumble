# CosmicRumble

**CosmicRumble** is a 2D turn based shooter prototype built with Unity. Multiple characters battle on small planets where gravity comes from local `GravitySource` objects. Players take turns performing actions such as jumping, using weapons and activating special skills.

## Gameplay Overview

- Characters are switched with **Tab** using the `TurnManager`.
- A turn timer (15 seconds by default) limits how long each character can act.
- Jump with **W** or **Space** when near a gravity source. Super jumps are limited and triggered via ability keys.
- Skills/weapons are bound to number keys 1‑6 and usually require pressing **Enter** to confirm before firing or activating:
  - **1** Pistol
  - **2** Shotgun
  - **3** RPG
  - **4** Grenade
  - **5** Super Jump
  - **6** Shield
- Aim by dragging the mouse from the character to set power and direction.

The main playable level is **`Assets/Scenes/SampleScene.unity`** which is the only scene included in the build settings.

## Requirements

- **Unity Editor:** version `6000.1.1f1` as specified in `ProjectSettings/ProjectVersion.txt`.
- **Unity packages** (from `Packages/manifest.json`):
  - `com.unity.render-pipelines.universal` (`17.1.0`)
  - `com.unity.inputsystem` (`1.14.0`)
  - `com.unity.feature.2d` (`2.0.1`)
  - `com.unity.visualscripting` (`1.9.6`)
  - `com.unity.timeline` (`1.8.7`)
  - plus other standard Unity modules.

## Opening the Project

1. Launch Unity Hub and choose **Open**.
2. Select this repository folder. Ensure the editor version matches `6000.1.1f1` or later.
3. After opening, load `Assets/Scenes/SampleScene.unity`.
4. Press **Play** to test in the editor.

## Building

1. Go to **File ▸ Build Settings…**
2. Confirm that `SampleScene` is in the **Scenes In Build** list.
3. Choose your target platform and click **Build**.

