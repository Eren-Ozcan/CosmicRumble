using UnityEngine;

/// <summary>
/// Menü → Oyun sahnesi arasında ayarları taşıyan kalıcı singleton.
/// DontDestroyOnLoad ile tüm sahnelerde yaşar.
/// </summary>
public class GameConfig : MonoBehaviour
{
    public static GameConfig Instance { get; private set; }

    // ── Maç Ayarları ──────────────────────────────────────────────
    public float TurnDuration  { get; set; } = 15f;
    public int   PlayerCount   { get; set; } = 2;

    // ── Ses Ayarları (PlayerPrefs ile kalıcı) ────────────────────
    public float MasterVolume  { get; set; } = 1f;
    public float MusicVolume   { get; set; } = 0.7f;
    public float SfxVolume     { get; set; } = 1f;

    // ── Grafik ───────────────────────────────────────────────────
    public bool  Fullscreen      { get; set; } = true;
    public int   ResolutionIndex { get; set; } = -1; // -1 = current/auto
    public int   QualityLevel    { get; set; } = -1; // -1 = engine's current setting
    public bool  VSync           { get; set; } = true;

    // ── Controls (only movement + jump are rebindable) ─────────────
    public KeyCode MoveLeftKey  { get; set; } = KeyCode.A;
    public KeyCode MoveRightKey { get; set; } = KeyCode.D;
    public KeyCode JumpKey      { get; set; } = KeyCode.Space;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Load()
    {
        MasterVolume    = PlayerPrefs.GetFloat("MasterVolume", 1f);
        MusicVolume     = PlayerPrefs.GetFloat("MusicVolume",  0.7f);
        SfxVolume       = PlayerPrefs.GetFloat("SfxVolume",    1f);
        TurnDuration    = PlayerPrefs.GetFloat("TurnDuration", 15f);
        PlayerCount     = PlayerPrefs.GetInt  ("PlayerCount",  2);
        Fullscreen      = PlayerPrefs.GetInt  ("Fullscreen",   1) == 1;
        ResolutionIndex = PlayerPrefs.GetInt  ("ResolutionIndex", -1);
        QualityLevel    = PlayerPrefs.GetInt  ("QualityLevel",    QualitySettings.GetQualityLevel());
        VSync           = PlayerPrefs.GetInt  ("VSync", 1) == 1;
        MoveLeftKey     = (KeyCode)PlayerPrefs.GetInt("MoveLeftKey",  (int)KeyCode.A);
        MoveRightKey    = (KeyCode)PlayerPrefs.GetInt("MoveRightKey", (int)KeyCode.D);
        JumpKey         = (KeyCode)PlayerPrefs.GetInt("JumpKey",      (int)KeyCode.Space);

        Screen.fullScreen = Fullscreen;
        ApplyGraphics();
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.SetFloat("MusicVolume",  MusicVolume);
        PlayerPrefs.SetFloat("SfxVolume",    SfxVolume);
        PlayerPrefs.SetFloat("TurnDuration", TurnDuration);
        PlayerPrefs.SetInt  ("PlayerCount",  PlayerCount);
        PlayerPrefs.SetInt  ("Fullscreen",   Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt  ("ResolutionIndex", ResolutionIndex);
        PlayerPrefs.SetInt  ("QualityLevel",    QualityLevel);
        PlayerPrefs.SetInt  ("VSync",           VSync ? 1 : 0);
        PlayerPrefs.SetInt  ("MoveLeftKey",  (int)MoveLeftKey);
        PlayerPrefs.SetInt  ("MoveRightKey", (int)MoveRightKey);
        PlayerPrefs.SetInt  ("JumpKey",      (int)JumpKey);
        PlayerPrefs.Save();
    }

    /// <summary>Applies resolution/quality/VSync to the engine. Also called whenever a setting changes.</summary>
    public void ApplyGraphics()
    {
        if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(QualityLevel, true);

        QualitySettings.vSyncCount = VSync ? 1 : 0;

        var resolutions = Screen.resolutions;
        if (ResolutionIndex >= 0 && ResolutionIndex < resolutions.Length)
        {
            var r = resolutions[ResolutionIndex];
            Screen.SetResolution(r.width, r.height, Fullscreen);
        }
    }
}
