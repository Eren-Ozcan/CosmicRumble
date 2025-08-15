# Project Setup

## Physics Layers

The project defines several custom layers used for gameplay logic. Ensure they are added under **Edit ▸ Project Settings… ▸ Tags and Layers** in the given order:

| Index | Layer Name | Usage |
|-------|------------|-------|
| 6 | Planet | Gravity sources and planetary bodies |
| 7 | Player | Player characters |
| 8 | Porjectile | Projectiles fired by weapons |
| 9 | Environment | Static world geometry |
| 10 | Bullet | Reserved for additional projectile types |

## Collision Matrix

Open **Project Settings ▸ Physics 2D** and configure the **Layer Collision Matrix**. All layers should collide with each other except `Player` with itself. A convenient subset of the matrix is shown below:

| Layer ↓ \ Layer → | Default | Planet | Player | Porjectile | Environment | Bullet |
|------------------|:------:|:------:|:------:|:-----------:|:-----------:|:------:|
| **Default** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Planet** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Player** | ✓ | ✓ | ✗ | ✓ | ✓ | ✓ |
| **Porjectile** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Environment** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| **Bullet** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

### Configuration Steps

1. Open **Edit ▸ Project Settings… ▸ Tags and Layers** and add the custom layers above.
2. Open **Project Settings ▸ Physics 2D**.
3. In **Layer Collision Matrix**, uncheck the intersection of `Player` with `Player` and leave all other boxes checked.
4. Assign the appropriate layer to each prefab (e.g., players on `Player`, bullets on `Porjectile`).
5. Save the project.

These settings allow projectile owner-ignore logic to work: projectiles initially ignore their owner's colliders while still colliding with other players and world objects.
