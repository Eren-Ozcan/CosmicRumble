using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using CosmicRumble.Achievements;
using CosmicRumble.Auth;
using CosmicRumble.Economy;
using CosmicRumble.Localization;

/// <summary>
/// UGS Authentication sarmalayıcısı — "Platform + Cosmic ID" modeli (Supercell ID benzeri):
/// - Platform girişi: Android'de Google Play Games (sessiz, açılışta) — GPGS auth code'u
///   UGS oturumuna bağlanır (Link) veya o hesaba geçilir (SignIn).
/// - Cosmic ID: mevcut username/password sistemi — cihazlar arası taşıma + Editor testi.
///
/// CloudSaveManager, MenuScene açılışında zaten otomatik bir anonim UGS oturumu kuruyor —
/// Register()/platform Link bu oturuma kimlik bilgisi EKLER, yani misafirken biriken ilerleme
/// kaybolmaz. Login()/platform hesap değişimi ise gerçekten FARKLI bir hesaba geçtiği için
/// (farklı Player ID / bulut verisi) sahne yeniden yüklenir (bkz. ReloadSessionScene).
///
/// Soğuk açılışta kimlik tespiti: UGS session token'ı bağlı hesabı sessizce geri yükler;
/// TrySilentSignInAsync + RefreshFromSessionAsync, PlayerInfo.Identities üzerinden
/// IsGuest/CurrentUsername'i doğru doldurur (eskiden her açılışta giriş kapısı çıkıyordu).
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    // UGS kimlik sağlayıcı TypeId'leri (PlayerInfo.Identities)
    public const string ProviderUsernamePassword = "username-password";
    public const string ProviderGooglePlayGames  = "google-play-games";

    const string LastUsernamePrefsKey = "cr_last_username";

    public bool          IsLoggedIn      { get; private set; }
    public bool          IsGuest         { get; private set; }
    public string        CurrentUsername { get; private set; }
    public PlayerProfile CurrentProfile  { get; private set; }

    /// <summary>Adlı (misafir olmayan) bir hesap aktif mi — giriş kapısının tek sorusu.</summary>
    public bool HasNamedAccount => IsLoggedIn && !IsGuest;

    /// <summary>Yerel dosya adları için güvenli kullanıcı anahtarı (profiles/, achievements_*.json).
    /// Cosmic ID → kullanıcı adı (mevcut dosyalarla geriye uyumlu); platform → "gpg_" + PlayerId
    /// öneki (Google görünen adı boşluk/emoji içerebilir); misafir → null.</summary>
    public string UserFileKey { get; private set; }

    /// <summary>Aktif oturuma bağlı kimlik sağlayıcıları (RefreshFromSessionAsync doldurur).</summary>
    public IReadOnlyList<string> LinkedProviders => _linkedProviders;
    readonly List<string> _linkedProviders = new List<string>();

    /// <summary>Belirtilen kimlik sağlayıcısı bu oturuma bağlı mı.</summary>
    public bool HasProvider(string providerId) => _linkedProviders.Contains(providerId);

    /// <summary>Bir giriş/kayıt/platform bağlama başarıyla tamamlandığında tetiklenir.
    /// (Login() sahneyi yenilediği için o yol event yerine reload ile sonuçlanır.)</summary>
    public event Action OnSignedIn;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Açılışta sessiz oturum kurtarma: UGS init → session token varsa oturumu geri yükle →
    /// (Android) sessiz GPGS bağlama denemesi → kimlikleri PlayerInfo'dan doldur.
    /// Hiçbir UI göstermez; başarısızlık sessizce misafir durumunda bırakır.
    /// </summary>
    public async Task TrySilentSignInAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn &&
                AuthenticationService.Instance.SessionTokenExists)
            {
                // Cached token adlı hesabı da geri yükler (anonim çağrı sadece token'ı kullanır)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AuthManager] Silent sign-in failed: {e.Message}");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Google'a henüz bağlı değilse sessizce bağlamayı dene (ilk kurulumda tek dokunuşsuz giriş)
        try
        {
            await RefreshFromSessionAsync();
            var google = GooglePlayAuthProvider.Shared;
            if (google.IsAvailable && !_linkedProviders.Contains(ProviderGooglePlayGames))
                await SignInWithPlatformAsync(google, silent: true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AuthManager] Silent GPGS attempt failed: {e.Message}");
        }
#endif

        await RefreshFromSessionAsync();
    }

    /// <summary>Bellekteki kimlik durumunu (IsGuest/CurrentUsername/LinkedProviders) aktif UGS
    /// oturumundan yeniden türetir. Sessiz restore sonrası ve her kimlik değişiminde çağrılır.</summary>
    public async Task RefreshFromSessionAsync()
    {
        _linkedProviders.Clear();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            IsLoggedIn = false;
            IsGuest    = false;
            CurrentUsername = null;
            UserFileKey     = null;
            CurrentProfile  = null;
            return;
        }

        IsLoggedIn = true;

        try
        {
            var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
            if (info?.Identities != null)
                foreach (var identity in info.Identities)
                    _linkedProviders.Add(identity.TypeId);

            if (_linkedProviders.Contains(ProviderUsernamePassword))
            {
                IsGuest = false;
                string username = info.Username;
                if (string.IsNullOrEmpty(username))
                    username = PlayerPrefs.GetString(LastUsernamePrefsKey, null);
                CurrentUsername = string.IsNullOrEmpty(username) ? "Oyuncu" : username;
                UserFileKey     = SanitizeFileKey(CurrentUsername);
            }
            else if (_linkedProviders.Contains(ProviderGooglePlayGames))
            {
                IsGuest = false;
                CurrentUsername = GooglePlayAuthProvider.Shared.GetDisplayName() ?? "Oyuncu";
                UserFileKey     = PlatformFileKey();
            }
            else
            {
                // Kimlik bilgisi eklenmemiş anonim oturum = misafir
                IsGuest = true;
                CurrentUsername = "Guest";
                UserFileKey     = null;
            }
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AuthManager] GetPlayerInfo failed: {e.Message}");
#endif
            // Kimlikler okunamadı — misafir varsay, giriş kapısı tekrar sorar
            IsGuest = true;
            CurrentUsername = "Guest";
            UserFileKey     = null;
        }

        if (HasNamedAccount)
        {
            LoadProfile();
            AchievementManager.Instance?.LoadForUser(UserFileKey);
        }
        else
        {
            CurrentProfile = null;
            AchievementManager.Instance?.LoadForUser(null);
        }
    }

    /// <summary>Platform (Google Play Games vb.) girişi. Anonim oturuma bağlar (ilerleme korunur);
    /// hesap zaten başka bir oyuncuya bağlıysa o hesaba geçer ve sahneyi yeniler.</summary>
    public async Task<(bool success, string error)> SignInWithPlatformAsync(IPlatformAuthProvider provider, bool silent)
    {
        if (provider == null || !provider.IsAvailable)
            return (false, silent ? null : Loc.T("Not available on this platform."));

        string credential;
        try
        {
            var credTask = provider.GetCredentialAsync(silent);
            if (silent)
            {
                // Sessiz deneme açılışı bloklamasın — 5 sn'de cevap yoksa vazgeç
                var done = await Task.WhenAny(credTask, Task.Delay(5000));
                if (done != credTask) return (false, null);
            }
            credential = await credTask;
        }
        catch (Exception e) { return (false, silent ? null : e.Message); }

        if (string.IsNullOrEmpty(credential))
            return (false, silent ? null : Loc.T("Platform sign-in couldn't be completed."));

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignInWithProviderCredentialAsync(provider.ProviderId, credential);
            }
            else if (!_linkedProviders.Contains(provider.ProviderId))
            {
                await LinkWithProviderCredentialAsync(provider.ProviderId, credential);
            }
            // Zaten bağlıysa yapılacak bir şey yok — durumu tazelemek yeterli

            await RefreshFromSessionAsync();
            OnSignedIn?.Invoke();
            return (true, null);
        }
        catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked ||
                                                e.ErrorCode == AuthenticationErrorCodes.AccountLinkLimitExceeded)
        {
            // Bu platform hesabı BAŞKA bir oyuncuya bağlı — o hesaba geç (kimlik değişimi).
            // Sign-out geri alınamaz; başarısızlıkta local state "çıkış yapıldı" olur (Login ile aynı).
            try
            {
                AuthenticationService.Instance.SignOut(clearCredentials: true);
                await SignInWithProviderCredentialAsync(provider.ProviderId, credential);
                await RefreshFromSessionAsync();
                OnSignedIn?.Invoke();
                ReloadSessionScene(); // farklı kimlik = farklı bulut verisi
                return (true, null);
            }
            catch (Exception e2)
            {
                await RefreshFromSessionAsync();
                return (false, silent ? null : string.Format(Loc.T("Couldn't switch accounts: {0}"), e2.Message));
            }
        }
        catch (RequestFailedException e) { return (false, silent ? null : string.Format(Loc.T("Couldn't reach the server: {0}"), e.Message)); }
        catch (Exception e)              { return (false, silent ? null : string.Format(Loc.T("Unexpected error: {0}"), e.Message)); }
    }

    /// <summary>Mevcut (genelde anonim) oturuma username/password kimlik bilgisi ekler —
    /// aynı Player ID/bulut verisi korunur, misafir ilerlemesi kaybolmaz.</summary>
    public async Task<(bool success, string error)> Register(string username, string password)
    {
        string validationError = ValidateCredentials(username, password);
        if (validationError != null) return (false, validationError);

        try
        {
            await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);

            IsLoggedIn      = true;
            IsGuest         = false;
            CurrentUsername = username;
            UserFileKey     = SanitizeFileKey(username);
            if (!_linkedProviders.Contains(ProviderUsernamePassword))
                _linkedProviders.Add(ProviderUsernamePassword);
            PlayerPrefs.SetString(LastUsernamePrefsKey, username);
            LoadProfile();
            AchievementManager.Instance?.LoadForUser(UserFileKey);

            // Aynı kimlik korunuyor (sadece kimlik bilgisi eklendi) — progress manager'lar
            // zaten doğru veriyle bellekte, reload gerekmiyor.
            OnSignedIn?.Invoke();
            return (true, null);
        }
        catch (AuthenticationException e) { return (false, e.Message); }
        catch (RequestFailedException e)  { return (false, string.Format(Loc.T("Couldn't reach the server: {0}"), e.Message)); }
        catch (Exception e)               { return (false, string.Format(Loc.T("Unexpected error: {0}"), e.Message)); }
    }

    /// <summary>Var olan farklı bir hesaba (farklı Player ID) giriş yapar.</summary>
    public async Task<(bool success, string error)> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, Loc.T("Username and password required."));

        // Yeni hesaba geçmeden önce eski oturumdan çıkılıyor — bu SignInWithUsernamePasswordAsync
        // başarısız olsa bile geri alınamıyor (credentials zaten temizlendi). Bu yüzden hata
        // durumunda local state'i de "çıkış yapıldı" olarak güncellemek gerekiyor, aksi halde
        // IsLoggedIn artık geçersiz olan eski hesabı göstermeye devam eder.
        bool hadActiveSession = AuthenticationService.Instance.IsSignedIn;
        if (hadActiveSession)
            AuthenticationService.Instance.SignOut(clearCredentials: true);

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            IsLoggedIn      = true;
            IsGuest         = false;
            CurrentUsername = username;
            UserFileKey     = SanitizeFileKey(username);
            _linkedProviders.Clear();
            _linkedProviders.Add(ProviderUsernamePassword);
            PlayerPrefs.SetString(LastUsernamePrefsKey, username);
            LoadProfile();
            AchievementManager.Instance?.LoadForUser(UserFileKey);

            OnSignedIn?.Invoke();
            ReloadSessionScene(); // farklı kimlik = farklı bulut verisi, manager'lar taze okumalı
            return (true, null);
        }
        catch (AuthenticationException e) { ResetIfSessionLost(hadActiveSession); return (false, e.Message); }
        catch (RequestFailedException e)  { ResetIfSessionLost(hadActiveSession); return (false, string.Format(Loc.T("Couldn't reach the server: {0}"), e.Message)); }
        catch (Exception e)               { ResetIfSessionLost(hadActiveSession); return (false, string.Format(Loc.T("Unexpected error: {0}"), e.Message)); }
    }

    /// <summary>Hesap açmadan misafir olarak oynar. Zaten anonim bir oturum varsa (normal durum
    /// — CloudSaveManager açılışta kuruyor) onu korur; adlı bir hesaptan geliniyorsa temiz bir
    /// anonim oturuma geçer.</summary>
    public async Task LoginAsGuest()
    {
        bool wasNamedAccount = HasNamedAccount;
        try
        {
            if (wasNamedAccount)
            {
                AuthenticationService.Instance.SignOut(clearCredentials: true);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            else if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AuthManager] Guest sign-in failed: {e.Message}");
#endif
        }

        IsLoggedIn      = true;
        IsGuest         = true;
        CurrentUsername = "Guest";
        UserFileKey     = null;
        CurrentProfile  = null;
        _linkedProviders.Clear();
        AchievementManager.Instance?.LoadForUser(null);

        if (wasNamedAccount) ReloadSessionScene();
    }

    /// <summary>Oturumu kapatır. Sahne yeniden yüklenince BootstrapSequence temiz bir anonim
    /// oturumla baştan başlar (giriş ekranı yeniden görünür).</summary>
    public void Logout()
    {
        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut(clearCredentials: true);

        IsLoggedIn      = false;
        IsGuest         = false;
        CurrentUsername = null;
        UserFileKey     = null;
        CurrentProfile  = null;
        _linkedProviders.Clear();

        ReloadSessionScene();
    }

    // ── Internals ────────────────────────────────────────────────────────

    async Task SignInWithProviderCredentialAsync(string providerId, string credential)
    {
        switch (providerId)
        {
            case ProviderGooglePlayGames:
                await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(credential);
                break;
            default:
                throw new NotSupportedException($"Unknown platform provider: {providerId}");
        }
    }

    async Task LinkWithProviderCredentialAsync(string providerId, string credential)
    {
        switch (providerId)
        {
            case ProviderGooglePlayGames:
                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(credential);
                break;
            default:
                throw new NotSupportedException($"Unknown platform provider: {providerId}");
        }
    }

    void LoadProfile()
    {
        string key = UserFileKey ?? "guest";
        CurrentProfile = PlayerProfile.Load(key) ?? new PlayerProfile { username = key };
        CurrentProfile.lastLogin = DateTime.UtcNow.ToString("o");
        CurrentProfile.Save();
    }

    /// <summary>Login() başarısız olduğunda, eğer önceden aktif bir oturum sign-out edilmişse
    /// (artık geri alınamaz), local state'i buna göre "çıkış yapıldı" olarak düzeltir.</summary>
    void ResetIfSessionLost(bool hadActiveSession)
    {
        if (!hadActiveSession) return;
        IsLoggedIn      = false;
        IsGuest         = false;
        CurrentUsername = null;
        UserFileKey     = null;
        CurrentProfile  = null;
        _linkedProviders.Clear();
    }

    string PlatformFileKey()
    {
        string playerId = AuthenticationService.Instance.PlayerId ?? "unknown";
        return "gpg_" + playerId.Substring(0, Mathf.Min(8, playerId.Length));
    }

    /// <summary>Dosya adında güvensiz karakterleri temizler (Google görünen adı vb. için).</summary>
    static string SanitizeFileKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (char c in raw)
            sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
        return sb.ToString();
    }

    /// <summary>
    /// Kimlik gerçekten değişti — mevcut progress manager'ları (eski kimliğin verisini bellekte
    /// tutuyorlar) yok edip sahneyi yeniden yükler. MainMenuUI'nin BootstrapSequence'i,
    /// CloudSaveManager'ın zaten aktif olan yeni kimlikle otomatik pull yapmasını ve
    /// manager'ların taze veriyle yeniden oluşturulmasını sağlar.
    /// </summary>
    void ReloadSessionScene()
    {
        DestroyIfExists(CurrencyManager.Instance?.gameObject);
        DestroyIfExists(PlayerLevelManager.Instance?.gameObject);
        DestroyIfExists(UnlockManager.Instance?.gameObject);
        DestroyIfExists(QuestManager.Instance?.gameObject);
        DestroyIfExists(ChestManager.Instance?.gameObject);
        DestroyIfExists(LoginStreakManager.Instance?.gameObject);
        DestroyIfExists(AchievementManager.Instance?.gameObject);
        DestroyIfExists(AchievementTracker.Instance?.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static void DestroyIfExists(GameObject go)
    {
        if (go != null) Destroy(go);
    }

    static string ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Loc.T("Username and password required.");
        if (username.Length < 3 || username.Length > 20)
            return Loc.T("Username must be 3-20 characters.");
        if (password.Length < 8 || password.Length > 30)
            return Loc.T("Password must be 8-30 characters.");
        return null;
    }
}
