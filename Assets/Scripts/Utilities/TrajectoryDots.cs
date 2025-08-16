using System.Collections.Generic;
using UnityEngine;

public class TrajectoryDots : MonoBehaviour
{
    public int dotCount = 60;
    public float timeStep = 0.05f;
    public float startScale = 1f;
    public float endScale = 0.3f;
    public Transform firePoint;

    private readonly List<Transform> dots = new List<Transform>();
    private SpriteRenderer[] renderers;
    private GravitySource[] gravitySources;
    private Sprite dotSprite;

    void Awake()
    {
        gravitySources = FindObjectsByType<GravitySource>(FindObjectsSortMode.None);
        dotSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        CreatePool();
        Hide();
    }

    public void Setup(int count, float step, Transform fp)
    {
        dotCount = count;
        timeStep = step;
        firePoint = fp;
        CreatePool();
        Hide();
    }

    void CreatePool()
    {
        if (dots.Count == dotCount)
            return;

        foreach (var t in dots)
            Destroy(t.gameObject);
        dots.Clear();

        for (int i = 0; i < dotCount; i++)
        {
            GameObject go = new GameObject("TrajectoryDot" + i);
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.sortingOrder = 100;
            go.SetActive(false);
            dots.Add(go.transform);
        }
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Show(Vector2 initialVelocity, float power)
    {
        if (firePoint == null)
            return;

        Color col = GetColor(power);
        Vector2 pos = firePoint.position;
        Vector2 vel = initialVelocity;

        for (int i = 0; i < dots.Count; i++)
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

            Transform t = dots[i];
            t.position = pos;
            float scale = Mathf.Lerp(startScale, endScale, (float)i / (dots.Count - 1));
            t.localScale = Vector3.one * scale;
            renderers[i].color = col;
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        foreach (var t in dots)
            t.gameObject.SetActive(false);
    }

    Color GetColor(float power)
    {
        if (power < 0.5f)
            return Color.Lerp(Color.green, Color.yellow, power * 2f);
        return Color.Lerp(Color.yellow, Color.red, (power - 0.5f) * 2f);
    }
}
