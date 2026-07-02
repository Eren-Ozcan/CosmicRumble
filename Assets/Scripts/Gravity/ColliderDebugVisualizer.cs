using UnityEngine;

public class ColliderDebugVisualizer : MonoBehaviour
{
    [Header("Renkler")]
    public Color triggerColor = Color.cyan;
    public Color solidColor = Color.green;
    public Color inactiveColor = new Color(1f, 0.5f, 0f);  // turuncu
    public float duration = 0f;  // 0 = her frame yenile

    void Update()
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        foreach (var col in colliders)
        {
            Color c = col.isTrigger ? triggerColor : solidColor;
            if (!col.enabled) c = inactiveColor;

            DrawCollider2D(col, c);
        }
    }

    void DrawCollider2D(Collider2D col, Color color)
    {
        if (col is PolygonCollider2D poly)
        {
            for (int p = 0; p < poly.pathCount; p++)
            {
                var pts = poly.GetPath(p);
                if (pts.Length < 2) continue;

                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 a = col.transform.TransformPoint(pts[i]);
                    Vector2 b = col.transform.TransformPoint(pts[(i + 1) % pts.Length]);
                    Debug.DrawLine(a, b, color, duration);
                }
            }
        }
        else if (col is CircleCollider2D circle)
        {
            DrawCircle((Vector2)col.transform.position + circle.offset,
                       circle.radius * col.transform.lossyScale.x,
                       color, 32);
        }
        else if (col is BoxCollider2D box)
        {
            DrawRect(col.transform, box.offset, box.size, color);
        }
        else if (col is CompositeCollider2D comp)
        {
            for (int p = 0; p < comp.pathCount; p++)
            {
                var pts = new Vector2[comp.GetPathPointCount(p)];
                comp.GetPath(p, pts);
                for (int i = 0; i < pts.Length; i++)
                {
                    int next = (i + 1) % pts.Length;
                    Debug.DrawLine(pts[i], pts[next], color, duration);
                }
            }
        }

        // === LOG: hangi collider nerede ===
#if UNITY_EDITOR
        Debug.Log($"[Collider] {col.gameObject.name} | " +
                  $"Type: {col.GetType().Name} | " +
                  $"IsTrigger: {col.isTrigger} | " +
                  $"Bounds center: {col.bounds.center} | " +
                  $"Bounds size: {col.bounds.size}",
                  col.gameObject);
#endif
    }

    void DrawCircle(Vector2 center, float radius, Color color, int segments)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            Vector2 a = center + radius * DirFromAngle(i * step);
            Vector2 b = center + radius * DirFromAngle((i + 1) * step);
            Debug.DrawLine(a, b, color, duration);
        }
    }

    Vector2 DirFromAngle(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    void DrawRect(Transform t, Vector2 offset, Vector2 size, Color color)
    {
        Vector2 c = (Vector2)t.position + offset;
        Vector2 h = size * 0.5f;
        Vector2[] corners = {
            c + new Vector2(-h.x, -h.y), c + new Vector2( h.x, -h.y),
            c + new Vector2( h.x,  h.y), c + new Vector2(-h.x,  h.y)
        };
        for (int i = 0; i < 4; i++)
            Debug.DrawLine(corners[i], corners[(i + 1) % 4], color, duration);
    }
}