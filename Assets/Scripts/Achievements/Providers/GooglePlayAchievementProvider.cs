using System;
using UnityEngine;

namespace CosmicRumble.Achievements
{
#if UNITY_ANDROID
    public class GooglePlayAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "GooglePlay";

        public void Initialize(Action onReady)
        {
            // TODO: Google Play Games Services initialization
#if UNITY_EDITOR
            Debug.Log("[GooglePlayAchievementProvider] Initialized (placeholder).");
#endif
            onReady?.Invoke();
        }

        public void UnlockAchievement(string id)
        {
            // TODO: Social.ReportProgress(id, 100.0, result => { });
#if UNITY_EDITOR
            Debug.Log($"[GooglePlayAchievementProvider] Unlock: {id}");
#endif
        }

        public void UpdateProgress(string id, int current, int max)
        {
            // TODO: Social.ReportProgress(id, (double)current / max * 100.0, result => { });
#if UNITY_EDITOR
            Debug.Log($"[GooglePlayAchievementProvider] Progress {id}: {current}/{max}");
#endif
        }

        public bool IsUnlocked(string id) => false;
    }
#endif
}
