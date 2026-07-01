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
    public bool  Fullscreen    { get; set; } = true;

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
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        MusicVolume  = PlayerPrefs.GetFloat("MusicVolume",  0.7f);
        SfxVolume    = PlayerPrefs.GetFloat("SfxVolume",    1f);
        TurnDuration = PlayerPrefs.GetFloat("TurnDuration", 15f);
        PlayerCount  = PlayerPrefs.GetInt  ("PlayerCount",  2);
        Fullscreen   = PlayerPrefs.GetInt  ("Fullscreen",   1) == 1;
        Screen.fullScreen = Fullscreen;
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.SetFloat("MusicVolume",  MusicVolume);
        PlayerPrefs.SetFloat("SfxVolume",    SfxVolume);
        PlayerPrefs.SetFloat("TurnDuration", TurnDuration);
        PlayerPrefs.SetInt  ("PlayerCount",  PlayerCount);
        PlayerPrefs.SetInt  ("Fullscreen",   Fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
