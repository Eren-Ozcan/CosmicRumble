using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/LevelConfig", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public const int MaxLevelBeforePrestige = 100;

        public int GetXPForLevel(int level)
        {
            if (level <= 10)  return 100;
            if (level <= 50)  return 500;
            if (level <= 100) return 1000;
            return 2000;
        }

        public long GetTotalXPForLevel(int level)
        {
            if (level <= 1) return 0;

            long total = 0;
            for (int lv = 1; lv < level; lv++)
                total += GetXPForLevel(lv);
            return total;
        }

        public int GetLevelFromTotalXP(long totalXP)
        {
            int level = 1;
            long accumulated = 0;

            while (true)
            {
                long needed = GetXPForLevel(level);
                if (accumulated + needed > totalXP)
                    break;
                accumulated += needed;
                level++;
            }
            return level;
        }
    }
}
