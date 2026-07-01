using System;
using UnityEngine;

namespace CosmicRumble.Achievements
{
#if UNITY_IOS
    public class AppStoreAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "AppStore";

        public void Initialize(Action onReady)
        {
            // TODO: Game Center initialization via UnityEngine.SocialPlatforms
#if UNITY_EDITOR
            Debug.Log("[AppStoreAchievementProvider] Initialized (placeholder).");
#endif
            onReady?.Invoke();
        }

        public void UnlockAchievement(string id)
        {
            // TODO: Social.ReportProgress(id, 100.0, result => { });
#if UNITY_EDITOR
            Debug.Log($"[AppStoreAchievementProvider] Unlock: {id}");
#endif
        }

        public void UpdateProgress(string id, int current, int max)
        {
            // TODO: Social.ReportProgress(id, (double)current / max * 100.0, result => { });
#if UNITY_EDITOR
            Debug.Log($"[AppStoreAchievementProvider] Progress {id}: {current}/{max}");
#endif
        }

        public bool IsUnlocked(string id) => false;
    }
#endif
}
