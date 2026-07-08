using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && GPGS_INSTALLED
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace CosmicRumble.Auth
{
    /// <summary>
    /// Google Play Games kimlik sağlayıcısı (GPGS v2 plugin). Tek paylaşılan örnek (Shared) —
    /// GooglePlayAchievementProvider da aynı oturumu kullanır, böylece çifte Activate/Authenticate
    /// yarışı olmaz. Editor'da ve GPGS kurulu olmayan derlemelerde IsAvailable=false, tüm
    /// çağrılar zararsız null döner.
    /// </summary>
    public class GooglePlayAuthProvider : IPlatformAuthProvider
    {
        public static GooglePlayAuthProvider Shared { get; } = new GooglePlayAuthProvider();

        public string ProviderId  => AuthManager.ProviderGooglePlayGames;
        public string DisplayName => "Google Play Games";

#if UNITY_ANDROID && GPGS_INSTALLED && !UNITY_EDITOR
        public bool IsAvailable => true;

        bool _activated;
        Task<bool> _pendingAuth; // eşzamanlı çağrılar tek Authenticate'i bekler

        public bool IsAuthenticated =>
            PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();

        public async Task<string> GetCredentialAsync(bool silent)
        {
            bool ok = await EnsureAuthenticatedAsync(silent);
            if (!ok) return null;

            var tcs = new TaskCompletionSource<string>();
            PlayGamesPlatform.Instance.RequestServerSideAccess(
                forceRefreshToken: false,
                code => tcs.TrySetResult(code));
            return await tcs.Task;
        }

        public string GetDisplayName() =>
            IsAuthenticated ? PlayGamesPlatform.Instance.GetUserDisplayName() : null;

        /// <summary>Başarım sağlayıcısının da kullandığı ortak oturum kurulumu.</summary>
        public Task<bool> EnsureAuthenticatedAsync(bool silent)
        {
            if (IsAuthenticated) return Task.FromResult(true);
            if (_pendingAuth != null && !_pendingAuth.IsCompleted) return _pendingAuth;

            if (!_activated)
            {
                PlayGamesPlatform.Activate();
                _activated = true;
            }

            var tcs = new TaskCompletionSource<bool>();
            Action<SignInStatus> onResult = status =>
            {
                if (status != SignInStatus.Success)
                    Debug.LogWarning($"[GooglePlayAuthProvider] Sign-in: {status}");
                tcs.TrySetResult(status == SignInStatus.Success);
            };

            // v2 plugin: Authenticate açılışta otomatik/sessiz dener; ManuallyAuthenticate
            // kullanıcı etkileşimiyle (buton) Play Games giriş ekranını açar.
            if (silent) PlayGamesPlatform.Instance.Authenticate(new Action<SignInStatus>(onResult));
            else        PlayGamesPlatform.Instance.ManuallyAuthenticate(new Action<SignInStatus>(onResult));

            _pendingAuth = tcs.Task;
            return _pendingAuth;
        }
#else
        public bool IsAvailable => false;
        public bool IsAuthenticated => false;

        public Task<string> GetCredentialAsync(bool silent) => Task.FromResult<string>(null);
        public string GetDisplayName() => null;
        public Task<bool> EnsureAuthenticatedAsync(bool silent) => Task.FromResult(false);
#endif
    }
}
