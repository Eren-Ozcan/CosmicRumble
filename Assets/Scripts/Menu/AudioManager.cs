using UnityEngine;

/// <summary>
/// Menü müziği ve ses efektleri yöneticisi.
/// DontDestroyOnLoad ile kalıcıdır.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Ses Klipleri")]
    [SerializeField] AudioClip menuMusic;
    [SerializeField] AudioClip buttonClick;
    [SerializeField] AudioClip buttonHover;

    AudioSource _musicSource;
    AudioSource _sfxSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Müzik kaynağı (loop)
        _musicSource = GetComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;

        // SFX kaynağı (ayrı)
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        ApplyVolumes();
    }

    void Start()
    {
        if (menuMusic != null)
        {
            _musicSource.clip = menuMusic;
            _musicSource.Play();
        }
    }

    // ── Volume ───────────────────────────────────────────────────

    public void ApplyVolumes()
    {
        var cfg = GameConfig.Instance;
        if (cfg == null) return;

        _musicSource.volume = cfg.MasterVolume * cfg.MusicVolume;
        _sfxSource.volume   = cfg.MasterVolume * cfg.SfxVolume;
    }

    // ── SFX API ──────────────────────────────────────────────────

    public void PlayClick() => PlaySfx(buttonClick);
    public void PlayHover() => PlaySfx(buttonHover);

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    // ── Müzik geçişi ─────────────────────────────────────────────

    public void SetMusic(AudioClip clip)
    {
        if (_musicSource.clip == clip) return;
        _musicSource.Stop();
        _musicSource.clip = clip;
        if (clip != null) _musicSource.Play();
    }
}
