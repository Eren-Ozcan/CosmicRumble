using UnityEngine;

/// <summary>
/// Shotgun için yarı saydam üçgen (koni) şeklinde atış önizlemesi.
/// Apex = firePoint, iki kenar = spread açısıyla range mesafesinde.
/// Güce göre renk: yeşil → siyah.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShotgunConePreview : MonoBehaviour
{
    [Range(0f, 1f)] public float alpha = 0.35f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 199;

    private MeshFilter _mf;
    private MeshRenderer _mr;
    private Mesh _mesh;
    private Material _mat;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();

        // Üç köşeli mesh — köşeler Show() içinde her frame güncellenir
        _mesh = new Mesh { name = "ShotgunConeMesh" };
        _mesh.vertices = new Vector3[3];
        _mesh.triangles = new[] { 0, 1, 2 };
        _mesh.uv = new Vector2[] { Vector2.zero, Vector2.up, Vector2.right };
        _mf.sharedMesh = _mesh;

        // Sprites/Default URP 2D ile de çalışır; şeffaflık Color.a ile ayarlanır
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        _mat = new Material(shader);
        _mat.color = new Color(0f, 1f, 0f, alpha);

        _mr.sharedMaterial = _mat;
        _mr.sortingLayerName = sortingLayerName;
        _mr.sortingOrder = sortingOrder;

        gameObject.SetActive(false);
    }

    /// <param name="firePos">Apex world konumu (firePoint.position)</param>
    /// <param name="fireDir">Normallanmış ateş yönü</param>
    /// <param name="halfAngle">Spread açısının yarısı (derece)</param>
    /// <param name="range">Koni uzunluğu (pelletMaxRange)</param>
    /// <param name="power">0..1 normalize güç — renk için</param>
    public void Show(Vector3 firePos, Vector2 fireDir, float halfAngle, float range, float power)
    {
        // GO'yu firePoint world konumuna taşı; mesh local space olarak hesaplanır
        transform.position = firePos;
        transform.rotation = Quaternion.identity;

        Vector2 leftDir  = Quaternion.AngleAxis( halfAngle, Vector3.forward) * (Vector3)fireDir;
        Vector2 rightDir = Quaternion.AngleAxis(-halfAngle, Vector3.forward) * (Vector3)fireDir;

        var verts = _mesh.vertices;
        verts[0] = Vector3.zero;                        // apex = firePoint
        verts[1] = (Vector3)(leftDir  * range);         // sol kenar
        verts[2] = (Vector3)(rightDir * range);         // sağ kenar
        _mesh.vertices = verts;
        _mesh.RecalculateBounds();

        Color col = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(power));
        col.a = alpha;
        _mat.color = col;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_mesh) Destroy(_mesh);
        if (_mat)  Destroy(_mat);
    }
}
