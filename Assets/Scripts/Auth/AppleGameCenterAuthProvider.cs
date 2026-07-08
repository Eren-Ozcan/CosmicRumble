using System.Threading.Tasks;

namespace CosmicRumble.Auth
{
    /// <summary>
    /// Apple Game Center kimlik sağlayıcısı — iOS hattı kurulana kadar stub.
    /// Gerçek implementasyon için: Apple.GameKit plugin'i (Unity Plug-ins for Apple) ile
    /// GKLocalPlayer.FetchItems() imza parametreleri alınır ve UGS tarafında
    /// AuthenticationService.SignInWithAppleGameCenterAsync / LinkWithAppleGameCenterAsync
    /// çağrılır (AuthManager.SignInWithProviderCredentialAsync'e "apple-game-center" dalı eklenir).
    /// </summary>
    public class AppleGameCenterAuthProvider : IPlatformAuthProvider
    {
        public static AppleGameCenterAuthProvider Shared { get; } = new AppleGameCenterAuthProvider();

        public string ProviderId  => "apple-game-center";
        public string DisplayName => "Game Center";
        public bool   IsAvailable => false;

        public Task<string> GetCredentialAsync(bool silent) => Task.FromResult<string>(null);
        public string GetDisplayName() => null;
    }
}
