using UnityEngine;
using TMPro;

/// <summary>
/// Karakterin üzerinde adını WorldSpace canvas ile gösterir.
/// LateUpdate'te her karede dünya koordinatlarında dik tutar.
/// </summary>
public class CharacterNameTag : MonoBehaviour
{
    // Artboard 16: isim can barının ÜSTÜNDE ayrı bir satır. HealthBarUI.verticalOffset
    // (1.72) + barın yarım yüksekliği (0.17) üstünde kalacak şekilde seçildi; ikisi
    // birlikte değiştirilir, yoksa etiketler tekrar üst üste biner.
    [Tooltip("Karakterin merkezinden dikey ofseti (yerel birim).")]
    public float verticalOffset = 2.22f;

    // Etiket kutusunun dünya birimi ölçüsü ve yazının punto karşılığı. Eski 3f punto
    // 0.6 birimlik kutuya sığmıyordu ve taşan yazı can barının üzerine düşüyordu.
    const float TagWidth  = 3f;
    const float TagHeight = 0.5f;
    const float TextSize  = 0.42f;

    TextMeshPro _label;
    Transform   _canvasTransform;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        BuildTag();
    }

    /// <summary>Adı ayarlar ve etiketi gösterir.</summary>
    public void SetName(string characterName)
    {
        if (_label != null) _label.text = characterName;
    }

    /// <summary>Takım rengini uygular (GravityBody.teamId) — outline sabit kalır, sadece dolgu rengi değişir.</summary>
    public void SetColor(Color color)
    {
        if (_label != null) _label.color = color;
    }

    // ── Build ─────────────────────────────────────────────────────────────

    void BuildTag()
    {
        // Canvas GO
        var canvasGO = new GameObject("NameTagCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0, verticalOffset, 0);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(TagWidth, TagHeight);

        // TextMeshPro (WorldSpace)
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(canvasGO.transform, false);
        _label = labelGO.AddComponent<TextMeshPro>();
        _label.fontSize        = TextSize;
        _label.color           = UiTheme.TextPrimary;
        _label.alignment       = TextAlignmentOptions.Center;
        _label.fontStyle       = FontStyles.Bold;
        _label.outlineWidth    = 0.2f;
        _label.outlineColor    = new Color32(0, 0, 0, 200);

        var lrt = _label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        _canvasTransform = canvasGO.transform;
    }

    // ── LateUpdate: kamerayı yüzle, yer çekimi yönünden bağımsız dik tut ─

    void LateUpdate()
    {
        if (_canvasTransform != null)
            _canvasTransform.rotation = Quaternion.identity;
    }
}
