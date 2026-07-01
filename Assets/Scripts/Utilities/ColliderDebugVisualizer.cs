#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteAlways]
public class PlanetDebugVisualizer : MonoBehaviour
{
    void OnDrawGizmos()
    {
        // 1. CircleCollider2D'leri çiz
        var circles = GetComponentsInChildren<CircleCollider2D>(true);
        foreach (var c in circles)
        {
            float worldRadius = c.radius * Mathf.Max(
                Mathf.Abs(c.transform.lossyScale.x),
                Mathf.Abs(c.transform.lossyScale.y));

            Gizmos.color = c.isTrigger
                ? new Color(0f, 1f, 0f, 0.8f)   // trigger → yeşil
                : new Color(1f, 0f, 0f, 0.8f);   // solid → kırmızı

            Gizmos.DrawWireSphere(c.transform.position, worldRadius);

#if UNITY_EDITOR
            Handles.Label(
                c.transform.position + Vector3.up * worldRadius,
                $"{c.gameObject.name} | r={worldRadius:F2} | trigger={c.isTrigger}");
#endif
        }

        // 2. PolygonCollider2D'leri çiz
        var polys = GetComponentsInChildren<PolygonCollider2D>(true);
        foreach (var p in polys)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f); // sarı
            for (int i = 0; i < p.pathCount; i++)
            {
                Vector2[] pts = p.GetPath(i);
                for (int j = 0; j < pts.Length; j++)
                {
                    Vector2 a = p.transform.TransformPoint(pts[j] + p.offset);
                    Vector2 b = p.transform.TransformPoint(pts[(j + 1) % pts.Length] + p.offset);
                    Gizmos.DrawLine(a, b);
                }
            }
#if UNITY_EDITOR
            Handles.Label(
                p.transform.position + Vector3.down * 2f,
                $"{p.gameObject.name} | Polygon | trigger={p.isTrigger}");
#endif
        }

        // 3. GravitySource radius'unu çiz
        var sources = GetComponentsInChildren<GravitySource>(true);
        foreach (var g in sources)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f); // mavi
            Gizmos.DrawWireSphere(g.transform.position, g.gravityRadius);
#if UNITY_EDITOR
            Handles.Label(
                g.transform.position + Vector3.right * g.gravityRadius,
                $"GravitySource | gR={g.gravityRadius:F2} | force={g.gravityForce}");
#endif
        }
    }
}
