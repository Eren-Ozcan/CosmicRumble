using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

#if GPGS_INSTALLED
using GooglePlayGames;
#endif

namespace CosmicRumble.Achievements
{
#if UNITY_ANDROID
    public class GooglePlayAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "GooglePlay";

        public void Initialize(Action onReady)
        {
#if GPGS_INSTALLED
            // Oturum kurulumu GooglePlayAuthProvider.Shared'da tekilleştirildi — AuthManager'ın
            // açılıştaki sessiz girişiyle yarışmasın diye burada Activate/Authenticate çağrılmaz.
            _ = AuthenticateViaSharedProviderAsync(onReady);
#else
            Debug.LogWarning("[GooglePlayAchievementProvider] Google Play Games Plugin is not installed — " +
                "see TODO.md 'Achievement platform providers' for setup steps. Falling back to no-op.");
            onReady?.Invoke();
#endif
        }

#if GPGS_INSTALLED
        static async System.Threading.Tasks.Task AuthenticateViaSharedProviderAsync(Action onReady)
        {
            bool success = await CosmicRumble.Auth.GooglePlayAuthProvider.Shared
                .EnsureAuthenticatedAsync(silent: true);
#if UNITY_EDITOR
            Debug.Log($"[GooglePlayAchievementProvider] Authenticate: {success}");
#endif
            onReady?.Invoke();
        }
#endif

        public void Tick() { }

        public void UnlockAchievement(string id)
        {
#if GPGS_INSTALLED
            if (!Social.localUser.authenticated) return;
            Social.ReportProgress(id, 100.0, _ => { });
#endif
        }

        public void UpdateProgress(string id, int current, int max)
        {
#if GPGS_INSTALLED
            if (!Social.localUser.authenticated || max <= 0) return;
            Social.ReportProgress(id, (double)current / max * 100.0, _ => { });
#endif
        }

        // Play Games achievement state requires an async Social.LoadAchievements callback;
        // AchievementManager already tracks unlock state locally, so this stays false here.
        public bool IsUnlocked(string id) => false;

        public void Shutdown() { }
    }
#endif
}
