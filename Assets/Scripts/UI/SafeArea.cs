using UnityEngine;

/// <summary>
/// Bu RectTransform'u Screen.safeArea'ya göre daraltır (çentik/kamera deliği/status bar/
/// gesture bar payı bırakır). Altına konan her şey kendi anchor/anchoredPosition mantığını
/// değiştirmeden otomatik olarak güvenli alanın içinde kalır — tam ekran (0,0)-(1,1) stretch
/// bir "kök" objeye eklenir, korumak istediğin UI elemanları onun ÇOCUĞU yapılır.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;
    ScreenOrientation lastOrientation;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea
            || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height
            || lastOrientation != Screen.orientation)
            Refresh();
    }

    void Refresh()
    {
        var safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
    }
}
