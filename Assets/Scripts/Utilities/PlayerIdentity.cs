using UnityEngine;

/// <summary>
/// Oyuncunun görünen adı — tek kaynak. "Misafir/Guest" kavramı UI'da YOK (Brawl Stars kalıbı):
/// hesap bağlıysa kullanıcı adı, değilse cihazda bir kez üretilip saklanan kozmik takma ad
/// (ör. "Nova731") kullanılır. Bağlı olmayan oturum yalnızca test içindir — final akışta açılışta
/// giriş ekranı zorunludur (bkz. MainMenuUI.BootstrapSequence).
/// </summary>
public static class PlayerIdentity
{
    const string PrefKey = "cr_display_name";

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
