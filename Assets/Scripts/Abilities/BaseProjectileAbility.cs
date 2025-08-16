using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GravityBody))]
public abstract class BaseProjectileAbility : MonoBehaviour
{
    [Header("Projectile Limit")]
    public int maxProjectiles = 3;

    [Header("Trajectory Preview")]
    public GameObject trajectoryDotPrefab;
    [Range(2,100)] public int trajectoryPoints = 60;
    public float timeStep = 0.05f;
    public float startDotScale = 1f;
    public float endDotScale = 0.3f;

    protected GravityBody gravityBody;
    protected GravitySource[] gravitySources;

    private readonly List<Transform> dotTransforms = new();
    private readonly List<SpriteRenderer> dotRenderers = new();

    // Owner & key
    [SerializeField] private string ownerId;
    protected abstract string WeaponKey { get; }

    protected virtual void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
#if UNITY_2023_1_OR_NEWER
        gravitySources = FindObjectsByType<GravitySource>(FindObjectsSortMode.None);
#else
        gravitySources = FindObjectsOfType<GravitySource>();
#endif
        if (string.IsNullOrEmpty(ownerId))
            ownerId = GetInstanceID().ToString();
        InitializeDots();
    }

    private void InitializeDots()
    {
        if (trajectoryDotPrefab == null) return;
        for (int i = 0; i < trajectoryPoints; i++)
        {
            GameObject dot = Instantiate(trajectoryDotPrefab);
            dot.SetActive(false);
            dotTransforms.Add(dot.transform);
            dotRenderers.Add(dot.GetComponent<SpriteRenderer>());
        }
    }

    protected void ShowTrajectory(Vector2 startPos, Vector2 initialVelocity, float power01)
    {
        if (dotTransforms.Count == 0) return;
        Vector2 pos = startPos;
        Vector2 vel = initialVelocity;
        Color color = GetColorByPower(power01);

        for (int i = 0; i < dotTransforms.Count; i++)
        {
            float t = (float)i / (dotTransforms.Count - 1);
            var tr = dotTransforms[i];
            tr.position = pos;
            tr.localScale = Vector3.one * Mathf.Lerp(startDotScale, endDotScale, t);
            if (dotRenderers[i] != null)
                dotRenderers[i].color = color;
            tr.gameObject.SetActive(true);

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
        }
    }

    protected void HideTrajectory()
    {
        foreach (var t in dotTransforms)
            t.gameObject.SetActive(false);
    }

    private Color GetColorByPower(float power)
    {
        power = Mathf.Clamp01(power);
        Color mid = new Color(1f, 0.65f, 0f); // orange
        if (power < 0.5f)
            return Color.Lerp(Color.green, mid, power * 2f);
        return Color.Lerp(mid, Color.red, (power - 0.5f) * 2f);
    }

    protected string BuildRegistryKey()
    {
        return ownerId + ":" + WeaponKey;
    }

    protected bool CanFireProjectile()
    {
        string key = BuildRegistryKey();
        return ProjectileRegistry.CanSpawn(key, maxProjectiles);
    }

    protected GameObject SpawnProjectile(GameObject prefab, Vector2 position, Quaternion rotation)
    {
        string key = BuildRegistryKey();
        if (!ProjectileRegistry.CanSpawn(key, maxProjectiles))
            return null;
        GameObject go = Instantiate(prefab, position, rotation);
        ProjectileRegistry.OnSpawned(key);
        var tracker = go.AddComponent<ProjectileTracker>();
        tracker.registryKey = key;
        return go;
    }
}

