using System.Threading.Tasks;

namespace CosmicRumble.Auth
{
    /// <summary>
    /// Platform kimlik sağlayıcısı soyutlaması (Google Play Games, Game Center...).
    /// AuthManager.SignInWithPlatformAsync bu arayüz üzerinden çalışır — platforma özel
    /// SDK detayları (auth code alma, oturum durumu) sağlayıcının içinde kalır.
    /// </summary>
    public interface IPlatformAuthProvider
    {
        /// <summary>UGS kimlik TypeId'si — AuthManager.ProviderGooglePlayGames vb. ile eşleşir.</summary>
        string ProviderId { get; }

        /// <summary>UI'da gösterilecek sağlayıcı adı ("Google Play Games").</summary>
        string DisplayName { get; }

        /// <summary>Bu platformda/derlemede kullanılabilir mi (Editor'da GPGS çalışmaz vb.).</summary>
        bool IsAvailable { get; }

        /// <summary>UGS'ye verilecek kimlik bilgisini (auth code / token) alır.
        /// silent=true → kullanıcıya UI göstermeden dene, olmuyorsa null dön.
        /// silent=false → gerekiyorsa platformun kendi giriş ekranını aç.</summary>
        Task<string> GetCredentialAsync(bool silent);

        /// <summary>Platform hesabının görünen adı (oturum yoksa null).</summary>
        string GetDisplayName();
    }
}
