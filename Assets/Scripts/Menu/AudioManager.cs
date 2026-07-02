using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menü müziği ve ses efektleri yöneticisi. DontDestroyOnLoad ile kalıcıdır.
/// Klipler Inspector'dan elle atanmaz — id ile Resources/Audio/{SFX|Music}/{id} altından yüklenir,
/// böylece yeni bir ses dosyası eklemek Inspector'a dokunmayı gerektirmez. Dosya henüz yoksa
/// ilgili çağrı sessizce no-op olur (hata fırlatmaz).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const string SfxResourcePath   = "Audio/SFX/";
    const string MusicResourcePath = "Audio/Music/";
    const string MenuMusicId       = "menu_music";

    AudioSource _musicSource;
    AudioSource _sfxSource;

    // null sonuçlar da cache'lenir ki eksik dosyalar için Resources.Load her çağrıda tekrarlanmasın.
    readonly Dictionary<string, AudioClip> _sfxCache   = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, AudioClip> _musicCache = new Dictionary<string, AudioClip>();

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
        PlayMusic(MenuMusicId);
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

    public void PlayClick() => PlaySfx("ui_button_click");
    public void PlayHover() => PlaySfx("ui_button_hover");

    /// <summary>Resources/Audio/SFX/{clipId} klibini bir kere çalar (yoksa no-op).</summary>
    public void PlaySfx(string clipId)
    {
        if (string.IsNullOrEmpty(clipId) || _sfxSource == null) return;
        var clip = GetCached(_sfxCache, SfxResourcePath, clipId);
        if (clip != null) _sfxSource.PlayOneShot(clip);
    }

    // ── Müzik ────────────────────────────────────────────────────

    /// <summary>Resources/Audio/Music/{clipId} klibine geçer ve loop çalar (yoksa no-op).</summary>
    public void PlayMusic(string clipId)
    {
        if (string.IsNullOrEmpty(clipId) || _musicSource == null) return;
        var clip = GetCached(_musicCache, MusicResourcePath, clipId);
        if (clip == null || _musicSource.clip == clip) return;

        _musicSource.Stop();
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    // ── Mermi üzerinde çalan sesler (uçuş whoosh'u vb.) ────────────

    /// <summary>
    /// Hedef GameObject'e bir AudioSource ekleyip Resources/Audio/SFX/{clipId} klibini loop çalar
    /// (dosya yoksa no-op, null döner). Hedef yok edildiğinde AudioSource da onunla birlikte
    /// yok olur — ayrıca durdurmaya gerek yok. Aynı objeye iki kez çağrılırsa ikinci AudioSource
    /// eklenmez, var olan kaynağın klibi değiştirilir.
    /// </summary>
    public AudioSource PlayLoopingSfxOnObject(GameObject target, string clipId)
    {
        if (target == null || string.IsNullOrEmpty(clipId)) return null;
        var clip = GetCached(_sfxCache, SfxResourcePath, clipId);
        if (clip == null) return null;

        var src = target.GetComponent<AudioSource>() ?? target.AddComponent<AudioSource>();
        src.clip        = clip;
        src.loop         = true;
        src.playOnAwake  = false;
        src.volume       = CurrentSfxVolume();
        src.Play();
        return src;
    }

    float CurrentSfxVolume()
    {
        var cfg = GameConfig.Instance;
        return cfg != null ? cfg.MasterVolume * cfg.SfxVolume : 1f;
    }

    static AudioClip GetCached(Dictionary<string, AudioClip> cache, string basePath, string clipId)
    {
        if (cache.TryGetValue(clipId, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>(basePath + clipId);
        cache[clipId] = clip;
        return clip;
    }
}
