using System;

namespace CosmicRumble.Achievements
{
    public interface IAchievementProvider
    {
        string ProviderName { get; }
        void Initialize(Action onReady);
        void UnlockAchievement(string id);
        void UpdateProgress(string id, int current, int max);
        bool IsUnlocked(string id);

        /// <summary>Called every frame by AchievementManager. Steam needs this to pump SteamClient.RunCallbacks(); other providers can no-op.</summary>
        void Tick();

        /// <summary>Called on application quit so platform SDKs can release native resources (e.g. SteamClient.Shutdown()).</summary>
        void Shutdown();
    }
}
