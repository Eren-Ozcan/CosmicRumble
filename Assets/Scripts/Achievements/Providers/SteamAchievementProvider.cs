using System;
using UnityEngine;

namespace CosmicRumble.Achievements
{
#if UNITY_STANDALONE && !UNITY_EDITOR
    public class SteamAchievementProvider : IAchievementProvider
    {
        public string ProviderName => "Steam";

#if STEAMWORKS_INSTALLED
        // TODO: replace with the real App ID once registered in the Steamworks partner portal.
        // 480 is Valve's public "Spacewar" test App ID — achievements won't persist against it.
        private const uint AppId = 480;
        private bool _initialized;
#endif

        public void Initialize(Action onReady)
        {
#if STEAMWORKS_INSTALLED
            try
            {
                Steamworks.SteamClient.Init(AppId, true); // asyncCallbacks: true → no manual RunCallbacks needed
                _initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SteamAchievementProvider] SteamClient.Init failed: {e.Message}");
            }
#else
            Debug.LogWarning("[SteamAchievementProvider] com.facepunch.steamworks is not installed — " +
                "see TODO.md 'Achievement platform providers' for setup steps. Falling back to no-op.");
#endif
            onReady?.Invoke();
        }

        public void Tick() { } // asyncCallbacks handles the Steam callback pump; kept for interface symmetry

        public void UnlockAchievement(string id)
        {
#if STEAMWORKS_INSTALLED
            if (!_initialized || !Steamworks.SteamClient.IsValid) return;

            foreach (var achievement in Steamworks.SteamUserStats.Achievements)
            {
                if (achievement.Identifier == id)
                {
                    achievement.Trigger();
                    Steamworks.SteamUserStats.StoreStats();
                    return;
                }
            }
            Debug.LogWarning($"[SteamAchievementProvider] No Steam achievement named '{id}' — set AchievementDefinition.steamId to the real Steamworks Admin API name (defaults to achievementId if left blank).");
#endif
        }

        public void UpdateProgress(string id, int current, int max)
        {
#if STEAMWORKS_INSTALLED
            if (!_initialized || !Steamworks.SteamClient.IsValid) return;
            Steamworks.SteamUserStats.IndicateAchievementProgress(id, (uint)current, (uint)max);
#endif
        }

        public bool IsUnlocked(string id)
        {
#if STEAMWORKS_INSTALLED
            if (!_initialized || !Steamworks.SteamClient.IsValid) return false;
            foreach (var achievement in Steamworks.SteamUserStats.Achievements)
                if (achievement.Identifier == id) return achievement.State;
#endif
            return false;
        }

        public void Shutdown()
        {
#if STEAMWORKS_INSTALLED
            if (_initialized && Steamworks.SteamClient.IsValid)
                Steamworks.SteamClient.Shutdown();
#endif
        }
    }
#endif
}
