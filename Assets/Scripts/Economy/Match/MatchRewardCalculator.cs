using UnityEngine;

namespace CosmicRumble.Economy
{
    public static class MatchRewardCalculator
    {
        // Galibiyet: 50 base + (süre/60)*10, max 150 XP
        // Mağlubiyet: 20 base + (süre/60)*5,  max  50 XP
        public static long CalculateMatchXP(bool isWinner, float matchDurationSeconds)
        {
            int minutes = Mathf.FloorToInt(matchDurationSeconds / 60f);
            if (isWinner)
                return Mathf.Min(50 + minutes * 10, 150);
            else
                return Mathf.Min(20 + minutes * 5, 50);
        }

        // Galibiyet: 30 base + (süre/60)*5, max 80 Gold
        // Mağlubiyet: 10 base + (süre/60)*2, max 30 Gold
        public static long CalculateMatchGold(bool isWinner, float matchDurationSeconds)
        {
            int minutes = Mathf.FloorToInt(matchDurationSeconds / 60f);
            if (isWinner)
                return Mathf.Min(30 + minutes * 5, 80);
            else
                return Mathf.Min(10 + minutes * 2, 30);
        }
    }
}
