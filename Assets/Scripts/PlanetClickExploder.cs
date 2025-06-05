// PlanetClickExploder
using UnityEngine;
using System.Collections;

public class PlanetClickExploder : MonoBehaviour
{
    [Header("Patlama Ayarları")]
    public float explosionRadius = 1f;    // Patlamanın etki yarıçapı (dünya birimi)
    public float explosionForce = 10f;    // Patlamanın kuvveti (Impulse)

    private enum State { Idle, AwaitEnter, AwaitClick }
    private State currentState = State.Idle;
    private GameObject boundaryObj;  // Patlama sınırını çizmek için

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                // 4 tuşuna basılınca onay bekleme durumuna geç
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    currentState = State.AwaitEnter;
                }
                break;

            case State.AwaitEnter:
                // Enter tuşuna basılınca bir frame beklendikten sonra fare tıklamasını bekle
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    StartCoroutine(EnableAwaitClickNextFrame());
                }
                break;

            case State.AwaitClick:
                // Fare sol tıklaması yapıldıysa patlama gerçekleştir
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3 mouseWorld3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 clickPosition = new Vector2(mouseWorld3D.x, mouseWorld3D.y);
                    PerformExplosion(clickPosition);
                    currentState = State.Idle;
                }
                break;
        }
    }

    private IEnumerator EnableAwaitClickNextFrame()
    {
        yield return null; // bir kare bekle
        currentState = State.AwaitClick;
    }

    void PerformExplosion(Vector2 center)
    {
        // 1) Patlama bölgesindeki tüm Collider2D'leri al
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius);

        // 2) Her bir collider için kontrol et
        foreach (Collider2D hit in hits)
        {
            DestructiblePlanet dp = hit.GetComponent<DestructiblePlanet>();
            if (dp != null)
            {
                dp.ExplodeWithForce(center, explosionRadius, explosionForce);
            }
        }

        // 3) Önceki sınırı kaldır (varsa)
        if (boundaryObj != null)
        {
            Destroy(boundaryObj);
        }

        // 4) Yeni sınır çiz
        DrawBoundaryCircle(center, explosionRadius);
    }

    void DrawBoundaryCircle(Vector2 center, float radius)
    {
        int segments = 60;
        boundaryObj = new GameObject("ExplosionBoundary");
        LineRenderer lr = boundaryObj.AddComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.loop = true;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = Color.red;
        lr.sortingOrder = 1000;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    void OnGUI()
    {
        if (currentState == State.AwaitEnter)
        {
            int w = Screen.width;
            int h = Screen.height;
            Rect rect = new Rect(w / 2 - 100, h / 2 - 25, 200, 50);
            GUI.Box(rect, "Patlama yapılsın mı?\n(Enter ile onayla)");
        }
    }
}
