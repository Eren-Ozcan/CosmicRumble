using UnityEngine;

/// <summary>
/// Oyuncunun görünen adı — tek kaynak. "Misafir/Guest" kavramı UI'da YOK (Brawl Stars kalıbı):
/// hesap bağlıysa kullanıcı adı, değilse cihazda bir kez üretilip saklanan kozmik takma ad
/// (ör. "Nova731") kullanılır. Bağlı olmayan oturum yalnızca test içindir — final akışta açılışta
/// giriş ekranı zorunludur (bkz. MainMenuUI.BootstrapSequence).
/// </summary>
public static class PlayerIdentity
{
#if CR_DEV_CLIENT
    // Host migration testi aynı makinede aynı .exe'nin birden çok kopyasını çalıştırıyor.
    // PlayerPrefs (Windows'ta registry, Company+Product'a göre anahtarlanır) tüm kopyalar
    // için AYNI konumu paylaşır — düzeltilmezse hepsi aynı üretilmiş takma adı ("Kozmo509"
    // gibi) okur/yazar ve iki farklı pencere aynı karakter gibi görünür. AuthManager'daki
    // EnsureDevClientProfile ile aynı mantıkla (-logFile argümanı, yoksa PID) her process'e
    // ayrı bir PlayerPrefs anahtarı veriliyor.
    static readonly string PrefKey = "cr_display_name_" + DevClientSuffix();

    static string DevClientSuffix()
    {
        string raw = null;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-logFile") { raw = System.IO.Path.GetFileNameWithoutExtension(args[i + 1]); break; }
        }
        if (string.IsNullOrEmpty(raw))
            raw = "pid" + System.Diagnostics.Process.GetCurrentProcess().Id;

        var sb = new System.Text.StringBuilder(30);
        foreach (char c in raw)
        {
            if (sb.Length >= 30) break;
            sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        }
        return sb.Length > 0 ? sb.ToString() : "devclient";
    }
#else
    const string PrefKey = "cr_display_name";
#endif

    // ASCII-only: UGS UpdatePlayerNameAsync bazı özel karakterleri reddediyor.
    static readonly string[] Prefixes =
        { "Astro", "Nova", "Kozmo", "Roket", "Meteor", "Pulsar", "Komet", "Galaksi" };

    /// <summary>Görünen ad: bağlı hesap adı, yoksa üretilmiş kalıcı takma ad.</summary>
    public static string Get()
    {
        var auth = AuthManager.Instance;
        if (auth != null && auth.IsLoggedIn && !auth.IsGuest &&
            !string.IsNullOrWhiteSpace(auth.CurrentUsername))
            return auth.CurrentUsername;

        string name = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(name))
        {
            name = Prefixes[Random.Range(0, Prefixes.Length)] + Random.Range(100, 1000);
            PlayerPrefs.SetString(PrefKey, name);
            PlayerPrefs.Save();
        }
        return name;
    }
}
