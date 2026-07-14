using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kills any character that strays beyond deathRadius from the world origin.
/// Attach to a single empty GameObject called "GameBoundary".
/// TurnManager picks up the death automatically via its null-check cleanup.
/// </summary>
public class DeathBoundary : MonoBehaviour
{
    public static DeathBoundary Instance { get; private set; }

    [Tooltip("Distance from world origin beyond which characters are instantly killed")]
    public float deathRadius = 20f;

    // Tracks characters already condemned this death event so we don't spam TakeDamage
    // during the destroyDelay window between Die() and Destroy().
    private readonly HashSet<GravityBody> _condemned = new HashSet<GravityBody>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void FixedUpdate()
    {
        // Purge entries whose GameObjects have since been destroyed
        _condemned.RemoveWhere(gb => gb == null);

        var bodies = FindObjectsByType<GravityBody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var gb in bodies)
        {
            if (_condemned.Contains(gb)) continue;
            if (((Vector2)gb.transform.position).magnitude > deathRadius)
            {
                _condemned.Add(gb);
                var health = gb.GetComponent<CharacterHealth>();
                if (health != null)
                    health.TakeDamage(9999f);
            }
        }

        foreach (var p in FindObjectsByType<ProjectileBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            CheckProjectile(p);

        foreach (var p in FindObjectsByType<HandGrenadeProjectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            CheckProjectile(p);

        foreach (var p in FindObjectsByType<TeleportOrbProjectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            CheckProjectile(p);
    }

    private void CheckProjectile(MonoBehaviour proj)
    {
        if (proj == null) return;
        if (((Vector2)proj.transform.position).sqrMagnitude > deathRadius * deathRadius)
        {
            CameraController.OnProjectileDestroyed();
            // Networked mermilerde client yereli Destroy edemez (NGO hatası + desync) —
            // yardımcı, client'ta görsel kapatıp server'ın despawn'ını bekler.
            NetworkPhysicsGuard.DespawnOrDestroy(proj.gameObject, proj);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Vector3.zero, deathRadius);
    }
}
