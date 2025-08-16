using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Serialization;
#endif

/// <summary>
/// Common projectile ability behaviour: ammo limit via ProjectileRegistry and
/// reusable dot based trajectory preview. Weapons like Pistol, RPG and
/// HandGrenade should derive from this.
/// </summary>
public abstract class BaseProjectileAbility : MonoBehaviour
{
    [Header("Fire Settings")]
#if UNITY_EDITOR
    [FormerlySerializedAs("firePoint")]
#endif
    public Transform firePoint;
#if UNITY_EDITOR
    [FormerlySerializedAs("projectilePrefab")]
#endif
    public GameObject projectilePrefab;
#if UNITY_EDITOR
    [FormerlySerializedAs("maxDragDistance")]
#endif
    public float maxDragDistance = 3f;
#if UNITY_EDITOR
    [FormerlySerializedAs("powerMultiplier")]
#endif
    public float powerMultiplier = 5f;
#if UNITY_EDITOR
    [FormerlySerializedAs("ignoreOwnerDuration")]
#endif
    public float ignoreOwnerDuration = 1f;

    [Header("Ammo")]
#if UNITY_EDITOR
    [FormerlySerializedAs("maxProjectiles")]
#endif
    public int maxProjectiles = 10;

    [Header("Trajectory Preview")]
#if UNITY_EDITOR
    [FormerlySerializedAs("trajectoryDotPrefab")]
#endif
    public GameObject trajectoryDotPrefab;
#if UNITY_EDITOR
    [FormerlySerializedAs("trajectoryPoints")]
#endif
    public int trajectoryPoints = 30;
#if UNITY_EDITOR
    [FormerlySerializedAs("timeStep")]
#endif
    public float timeStep = 0.05f;

    [Header("Registry Key")]
    [Tooltip("Unique identifier of the owning character.")]
    public string ownerId = "owner";

    protected readonly List<Transform> trajectoryDots = new();
    protected readonly List<SpriteRenderer> trajectoryDotSprites = new();
    protected GravitySource[] gravitySources;
    protected GravityBody gravityBody;
    protected CharacterAbilities charAbilities;
    protected bool isDragging = false;
    protected Vector2 dragStart;
    protected bool wasActive = false;

    protected int activeProjectiles = 0;

    protected abstract string WeaponKey { get; }

    protected virtual void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();
#if UNITY_2023_1_OR_NEWER
        gravitySources = FindObjectsByType<GravitySource>(FindObjectsSortMode.None);
#else
        gravitySources = FindObjectsOfType<GravitySource>();
#endif

        for (int i = 0; i < trajectoryPoints; i++)
        {
            var dot = Instantiate(trajectoryDotPrefab, transform);
            dot.SetActive(false);
            trajectoryDots.Add(dot.transform);
            trajectoryDotSprites.Add(dot.GetComponent<SpriteRenderer>());
        }
    }

    protected virtual void UpdateAmmoUI() { }

    protected string GetRegistryKey() => ownerId + ":" + WeaponKey;
    // ProjectileRegistry is part of the full Unity project but may not be
    // available in isolated compilation environments. Guard the calls so the
    // code still compiles when the registry class is missing.
#if UNITY_2021_1_OR_NEWER
    protected bool CanSpawn() => ProjectileRegistry.CanSpawn(GetRegistryKey(), maxProjectiles);

    protected void RegisterProjectile(GameObject proj)
    {
        activeProjectiles++;
        var proxy = proj.AddComponent<ProjectileRegistryProxy>();
        proxy.Init(GetRegistryKey(), OnProjectileDespawned);
        UpdateAmmoUI();
    }
#else
    // Fallback stubs for non-Unity builds (e.g., CI compilation).
    protected bool CanSpawn() => true;

    protected void RegisterProjectile(GameObject proj)
    {
        activeProjectiles++;
        UpdateAmmoUI();
    }
#endif

    protected virtual void OnProjectileDespawned()
    {
        activeProjectiles = Mathf.Max(0, activeProjectiles - 1);
        UpdateAmmoUI();
    }

    protected void DrawTrajectory(Vector2 initialVelocity, float power01)
    {
        Vector2 pos = firePoint.position;
        Vector2 vel = initialVelocity;
        Color c = EvaluateColor(power01);

        for (int i = 0; i < trajectoryPoints; i++)
        {
            Vector2 acc = Vector2.zero;
            foreach (var src in gravitySources)
            {
                Vector2 dir = (Vector2)src.transform.position - pos;
                float r2 = dir.sqrMagnitude;
                if (r2 < 0.001f) continue;
                acc += dir.normalized * (src.gravityForce / r2);
            }

            vel += acc * timeStep;
            pos += vel * timeStep;

            if (i < trajectoryDots.Count)
            {
                var dot = trajectoryDots[i];
                dot.position = pos;
                float scale = Mathf.Lerp(1f, 0.3f, (float)i / (trajectoryPoints - 1));
                dot.localScale = Vector3.one * scale;
                dot.gameObject.SetActive(true);
                var sr = trajectoryDotSprites[i];
                if (sr != null) sr.color = c;
            }
        }
    }

    protected void CancelDrag()
    {
        isDragging = false;
        foreach (var dot in trajectoryDots)
            dot.gameObject.SetActive(false);
    }

    protected Color EvaluateColor(float t)
    {
        if (t <= 0.33f)
            return Color.Lerp(Color.green, Color.yellow, t / 0.33f);
        if (t <= 0.66f)
            return Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (t - 0.33f) / 0.33f);
        return Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, (t - 0.66f) / 0.34f);
    }

    protected void Fire(Vector2 initial)
    {
        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        RegisterProjectile(bulletGO);

        if (bulletGO.TryGetComponent<Projectile>(out var proj))
            proj.Init(initial, gameObject, ignoreOwnerDuration);
        else if (bulletGO.TryGetComponent<HandGrenadeProjectile>(out var grenade))
            grenade.Init(initial, gameObject, ignoreOwnerDuration);
        else if (bulletGO.TryGetComponent<Rigidbody2D>(out var rb))
            rb.AddForce(initial, ForceMode2D.Impulse);
    }
}
