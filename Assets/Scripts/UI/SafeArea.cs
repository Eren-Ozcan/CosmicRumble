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
        var safeArea = Intersect(Screen.safeArea, SystemBarArea());
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

    static Rect Intersect(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        if (xMax <= xMin || yMax <= yMin) return a;   // saçma bir kesişim çıkarsa dokunma
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    /// <summary>
    /// Sistem çubuklarının (gezinme çubuğu, durum çubuğu) dışında kalan alan.
    ///
    /// <para><b>Neden gerekli:</b> <c>Screen.safeArea</c> Android'de yalnızca ekran çentiğini
    /// hesaba katıyor. Test cihazında (Huawei POT-LX1, Android 10) yatay tutuşta çentik SOLDA
    /// 81 px, üç tuşlu gezinme çubuğu ise SAĞDA ~90 px yer kaplıyor ve uygulama penceresi tüm
    /// 2340 px'i kapladığı için çubuk arayüzün üstüne biniyor — çekmecenin kapatma butonu
    /// bu yüzden yarım görünüyordu. Gezinme çubuğu safeArea'ya girmediginden pencere
    /// insetlerini ayrıca sormak gerekiyor.</para>
    /// </summary>
    static Rect SystemBarArea()
    {
        var full = new Rect(0, 0, Screen.width, Screen.height);
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity == null) return full;
            using var window = activity.Call<AndroidJavaObject>("getWindow");
            using var decor  = window.Call<AndroidJavaObject>("getDecorView");
            using var insets = decor.Call<AndroidJavaObject>("getRootWindowInsets");
            if (insets == null) return full;

            int left   = insets.Call<int>("getSystemWindowInsetLeft");
            int right  = insets.Call<int>("getSystemWindowInsetRight");
            int top    = insets.Call<int>("getSystemWindowInsetTop");
            int bottom = insets.Call<int>("getSystemWindowInsetBottom");

            // Android insetleri sol-ust kokenli, Unity ekran koordinatlari sol-alt kokenli.
            return Rect.MinMaxRect(left, bottom, Screen.width - right, Screen.height - top);
        }
        catch
        {
            return full;   // eski/farkli bir cihazda API yoksa safeArea'ya guven
        }
#else
        return full;
#endif
    }
}
