using UnityEngine;

/// <summary>
/// Dünya uzayındaki karakter etiketlerinin (isim, can barı) ekranda dik durması için ortak
/// dönüş kaynağı.
///
/// <para>Kamera gezegen yüzeyine göre döner (<see cref="CameraController"/> her karede
/// z ekseninde açı verir), bu yüzden dünya eksenine sabitlenen bir etiket ekranda dik
/// DURMAZ: oyuncu gezegenin etrafında yürüdükçe etiketin ekrandaki açısı kameranın açısı
/// kadar kayar. Etiketin kameranın kendi dönüşünü alması gerekir.</para>
///
/// <para><see cref="Camera.main"/> her etiket için her karede çağrılmasın diye sonuç kare
/// başına bir kez hesaplanıp paylaşılır.</para>
/// </summary>
public static class WorldTagOrientation
{
    static Transform  s_cam;
    static int        s_frame    = -1;
    static Quaternion s_rotation = Quaternion.identity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { s_cam = null; s_frame = -1; s_rotation = Quaternion.identity; }

    /// <summary>Bu karede etiketlerin alması gereken dünya dönüşü.</summary>
    public static Quaternion Current
    {
        get
        {
            if (s_frame == Time.frameCount) return s_rotation;
            s_frame = Time.frameCount;

            // Serbest kamera moduna geçildiğinde aktif kamera değişir; ölü/kapalı
            // transform'a takılı kalmamak için yeniden çözülür.
            if (s_cam == null || !s_cam.gameObject.activeInHierarchy)
            {
                var cam = Camera.main;
                s_cam = cam != null ? cam.transform : null;
            }

            s_rotation = s_cam != null ? s_cam.rotation : Quaternion.identity;
            return s_rotation;
        }
    }
}
