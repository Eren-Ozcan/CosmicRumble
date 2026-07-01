using UnityEngine;

namespace CosmicRumble.Achievements
{
    public enum AchievementRarity      { Common, Rare, Epic, Legendary }
    public enum AchievementTriggerType { SingleUnlock, Cumulative, SpecialAction }

    [CreateAssetMenu(menuName = "CosmicRumble/Achievement", fileName = "AchievementDefinition")]
    public class AchievementDefinition : ScriptableObject
    {
        public string                  achievementId;
        public string                  displayName;
        public string                  description;
        public Sprite                  icon;          // null-safe
        public AchievementRarity       rarity;
        public AchievementTriggerType  triggerType;
        public int                     targetValue;
        public bool                    isSecret;

        public long   rewardXP;
        public long   rewardGold;
        public long   rewardGem;
        public string rewardCostumeId;
    }
}
