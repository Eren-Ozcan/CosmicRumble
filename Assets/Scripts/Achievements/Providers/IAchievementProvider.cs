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
    }
}
