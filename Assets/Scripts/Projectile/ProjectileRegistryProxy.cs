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
#if UNITY_2021_1_OR_NEWER
        ProjectileRegistry.OnSpawned(key);
#endif
    }

    void OnDestroy()
    {
#if UNITY_2021_1_OR_NEWER
        ProjectileRegistry.OnDespawned(key);
#endif
        onDespawn?.Invoke();
    }
}