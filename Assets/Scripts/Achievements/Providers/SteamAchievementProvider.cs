using System;
using UnityEngine;

namespace CosmicRumble.Achievements
{
#if UNITY_STANDALONE && !UNITY_EDITOR
    public class SteamAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "Steam";

        public void Initialize(Action onReady)
        {
            // TODO: Initialize Facepunch.Steamworks
#if UNITY_EDITOR
            Debug.Log("[SteamAchievementProvider] Steam provider initialized (stub).");
#endif
            onReady?.Invoke();
        }

        public void UnlockAchievement(string id)
        {
            // TODO: Steamworks.SteamUserStats.SetAchievement(id);
            //       Steamworks.SteamUserStats.StoreStats();
#if UNITY_EDITOR
            Debug.Log($"[SteamAchievementProvider] Unlock: {id}");
#endif
        }

        public void UpdateProgress(string id, int current, int max)
        {
            // TODO: Steamworks.SteamUserStats.IndicateAchievementProgress(id, (uint)current, (uint)max);
#if UNITY_EDITOR
            Debug.Log($"[SteamAchievementProvider] Progress {id}: {current}/{max}");
#endif
        }

        public bool IsUnlocked(string id)
        {
            // TODO: Steamworks.SteamUserStats.GetAchievement(id, out bool unlocked);
            return false;
        }
    }
#endif
}
