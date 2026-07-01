using System;

namespace CosmicRumble.Economy
{
    [Serializable]
    public class PlayerProgressData
    {
        public long  totalXP;
        public int   currentLevel;
        public int   prestigeRank;
        public long  xpInCurrentLevel;
        public long  xpNeededForNextLevel;
        public float levelProgress;
    }
}
