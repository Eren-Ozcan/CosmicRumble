using System;
using UnityEngine;

/// <summary>
/// Helper component that notifies ProjectileRegistry about spawned and
/// despawned projectiles. Added at runtime by BaseProjectileAbility.
/// </summary>
public class ProjectileRegistryProxy : MonoBehaviour
{
    private string key;
    private Action onDespawn;

    public void Init(string key, Action onDespawnCallback)
    {
        this.key = key;
        onDespawn = onDespawnCallback;
        ProjectileRegistry.OnSpawned(key);
    }

    void OnDestroy()
    {
        ProjectileRegistry.OnDespawned(key);
        onDespawn?.Invoke();
    }
}
