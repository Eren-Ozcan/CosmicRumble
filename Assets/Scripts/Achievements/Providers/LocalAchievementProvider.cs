using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Achievements
{
    public class LocalAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "Local";

        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public void Initialize(Action onReady)
        {
#if UNITY_EDITOR
            Debug.Log("[LocalAchievementProvider] Initialized.");
#endif
            onReady?.Invoke();
        }

        public void UnlockAchievement(string id)
        {
            _unlocked.Add(id);
#if UNITY_EDITOR
            Debug.Log($"[LocalAchievementProvider] Achievement unlocked: {id}");
#endif
        }

        public void UpdateProgress(string id, int current, int max)
        {
#if UNITY_EDITOR
            Debug.Log($"[LocalAchievementProvider] Progress {id}: {current}/{max}");
#endif
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);
    }
}
