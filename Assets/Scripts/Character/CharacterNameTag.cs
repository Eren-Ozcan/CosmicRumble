using UnityEngine;
using TMPro;

/// <summary>
/// Karakterin üzerinde adını WorldSpace canvas ile gösterir.
/// LateUpdate'te her karede dünya koordinatlarında dik tutar.
/// </summary>
public class CharacterNameTag : MonoBehaviour
{
    [Tooltip("Karakterin merkezinden dikey ofseti (yerel birim).")]
    public float verticalOffset = 1.4f;

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
        rt.sizeDelta = new Vector2(3f, 0.6f);

        // TextMeshPro (WorldSpace)
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(canvasGO.transform, false);
        _label = labelGO.AddComponent<TextMeshPro>();
        _label.fontSize        = 3f;
        _label.color           = Color.white;
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
    }
}
