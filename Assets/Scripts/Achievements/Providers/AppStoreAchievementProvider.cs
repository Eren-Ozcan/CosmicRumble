using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

namespace CosmicRumble.Achievements
{
#if UNITY_IOS
    public class AppStoreAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "AppStore";

        public void Initialize(Action onReady)
        {
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(true);
            Social.localUser.Authenticate(success =>
            {
#if UNITY_EDITOR
                Debug.Log($"[AppStoreAchievementProvider] Authenticate: {success}");
#endif
                onReady?.Invoke();
            });
        }

        public void Tick() { }

        public void UnlockAchievement(string id)
        {
            if (!Social.localUser.authenticated) return;
            Social.ReportProgress(id, 100.0, _ => { });
        }

        public void UpdateProgress(string id, int current, int max)
        {
            if (!Social.localUser.authenticated || max <= 0) return;
            Social.ReportProgress(id, (double)current / max * 100.0, _ => { });
        }

        // Game Center achievement state requires an async Social.LoadAchievements callback;
        // AchievementManager already tracks unlock state locally, so this stays false here.
        public bool IsUnlocked(string id) => false;

        public void Shutdown() { }
    }
#endif
}
