namespace CosmicRumble.Economy
{
    public struct UnlockCheckResult
    {
        public bool isLevelMet;
        public bool isCurrencyMet;
        public bool isAchievementMet;
        public bool canUnlock;
        public long missingGold;
        public long missingGem;
        public int  missingLevel;
    }
}
