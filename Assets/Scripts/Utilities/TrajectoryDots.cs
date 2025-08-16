using System.Collections.Generic;
using UnityEngine;

public class TrajectoryDots : MonoBehaviour
{
    [Header("Dot Settings")]
    public GameObject dotPrefab;
    public int dotCount = 30;
    public float startScale = 1f;
    public float endScale = 0.3f;

    [Header("Simulation Settings")]
    public int trajectoryPoints = 60;
    public float timeStep = 0.05f;

    private List<Transform> dots = new List<Transform>();
    private List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    void Awake()
    {
        for (int i = 0; i < dotCount; i++)
        {
            if (dotPrefab == null) break;
            GameObject dot = Instantiate(dotPrefab, transform);
            dot.SetActive(false);
            dots.Add(dot.transform);
            renderers.Add(dot.GetComponent<SpriteRenderer>());
        }
    }

    public void Show(Vector2 initialVelocity, Vector2 startPos, GravitySource[] sources, float power01)
    {
        int activeCount = Mathf.Min(trajectoryPoints, dots.Count);
        Vector2 pos = startPos;
        Vector2 vel = initialVelocity;
        Color c = EvaluateColor(power01);

        for (int i = 0; i < activeCount; i++)
        {
            Vector2 acc = Vector2.zero;
            if (sources != null)
            {
                foreach (var src in sources)
                {
                    Vector2 dir = (Vector2)src.transform.position - pos;
                    float r2 = dir.sqrMagnitude;
                    if (r2 < 0.001f) continue;
                    acc += dir.normalized * (src.gravityForce / r2);
                }
            }

            vel += acc * timeStep;
            pos += vel * timeStep;

            Transform dot = dots[i];
            dot.position = pos;
            float t = activeCount > 1 ? (float)i / (activeCount - 1) : 0f;
            float scale = Mathf.Lerp(startScale, endScale, t);
            dot.localScale = Vector3.one * scale;
            if (renderers[i] != null)
                renderers[i].color = c;
            if (!dot.gameObject.activeSelf)
                dot.gameObject.SetActive(true);
        }

        for (int i = activeCount; i < dots.Count; i++)
        {
            if (dots[i].gameObject.activeSelf)
                dots[i].gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        foreach (var dot in dots)
            if (dot.gameObject.activeSelf)
                dot.gameObject.SetActive(false);
    }

    private Color EvaluateColor(float power01)
    {
        power01 = Mathf.Clamp01(power01);
        if (power01 <= 0.5f)
            return Color.Lerp(Color.green, Color.yellow, power01 / 0.5f);
        else
            return Color.Lerp(Color.yellow, Color.red, (power01 - 0.5f) / 0.5f);
    }
}

