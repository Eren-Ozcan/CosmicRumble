using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using CosmicRumble.Achievements;
using CosmicRumble.Economy;

/// <summary>
/// UGS Authentication sarmalayıcısı. Kayıt/giriş artık gerçek, cihazlar arası taşınabilir
/// hesaplar (Unity Gaming Services username/password kimlik sağlayıcısı) — eski local
/// users.json/SHA256 sistemi kaldırıldı. Var olan eski local hesaplar bu değişiklikle geçersiz
/// kalır: şifrenin düz hali hiçbir yerde saklanmadığı için (sadece hash) otomatik taşıma mümkün
/// değil.
///
/// CloudSaveManager, MenuScene açılışında zaten otomatik bir anonim UGS oturumu kuruyor —
/// Register() bu oturuma kimlik bilgisi EKLER (AddUsernamePasswordAsync), yani misafirken
/// biriken ilerleme kaybolmaz. Login() ise gerçekten FARKLI bir hesaba geçtiği için (farklı
/// Player ID / bulut verisi) mevcut progress manager'ların bellekteki verisi geçersiz kalır —
/// bu yüzden kimlik gerçekten değiştiğinde sahne yeniden yüklenir (bkz. ReloadSessionScene).
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public bool          IsLoggedIn      { get; private set; }
    public bool          IsGuest         { get; private set; }
    public string        CurrentUsername { get; private set; }
    public PlayerProfile CurrentProfile  { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ───────────────────────────────────────────────────────

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
            LoadProfile(username);
            AchievementManager.Instance?.LoadForUser(username);

            // Aynı kimlik korunuyor (sadece kimlik bilgisi eklendi) — progress manager'lar
            // zaten doğru veriyle bellekte, reload gerekmiyor.
            return (true, null);
        }
        catch (AuthenticationException e) { return (false, e.Message); }
        catch (RequestFailedException e)  { return (false, $"Couldn't reach the server: {e.Message}"); }
        catch (Exception e)               { return (false, $"Unexpected error: {e.Message}"); }
    }

    /// <summary>Var olan farklı bir hesaba (farklı Player ID) giriş yapar.</summary>
    public async Task<(bool success, string error)> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username and password are required.");

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
            LoadProfile(username);
            AchievementManager.Instance?.LoadForUser(username);

            ReloadSessionScene(); // farklı kimlik = farklı bulut verisi, manager'lar taze okumalı
            return (true, null);
        }
        catch (AuthenticationException e) { ResetIfSessionLost(hadActiveSession); return (false, e.Message); }
        catch (RequestFailedException e)  { ResetIfSessionLost(hadActiveSession); return (false, $"Couldn't reach the server: {e.Message}"); }
        catch (Exception e)               { ResetIfSessionLost(hadActiveSession); return (false, $"Unexpected error: {e.Message}"); }
    }

    /// <summary>Hesap açmadan misafir olarak oynar. Zaten anonim bir oturum varsa (normal durum
    /// — CloudSaveManager açılışta kuruyor) onu korur; adlı bir hesaptan geliniyorsa temiz bir
    /// anonim oturuma geçer.</summary>
    public async Task LoginAsGuest()
    {
        bool wasNamedAccount = IsLoggedIn && !IsGuest;
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
        CurrentProfile  = null;
        AchievementManager.Instance?.LoadForUser(null);

        if (wasNamedAccount) ReloadSessionScene();
    }

    /// <summary>Oturumu kapatır. Sahne yeniden yüklenince BootstrapSequence temiz bir anonim
    /// oturumla baştan başlar.</summary>
    public void Logout()
    {
        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut(clearCredentials: true);

        IsLoggedIn      = false;
        IsGuest         = false;
        CurrentUsername = null;
        CurrentProfile  = null;

        ReloadSessionScene();
    }

    // ── Internals ────────────────────────────────────────────────────────

    void LoadProfile(string username)
    {
        CurrentProfile = PlayerProfile.Load(username) ?? new PlayerProfile { username = username };
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
        CurrentProfile  = null;
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
            return "Username and password are required.";
        if (username.Length < 3 || username.Length > 20)
            return "Username must be 3-20 characters.";
        if (password.Length < 8 || password.Length > 30)
            return "Password must be 8-30 characters.";
        return null;
    }
}
