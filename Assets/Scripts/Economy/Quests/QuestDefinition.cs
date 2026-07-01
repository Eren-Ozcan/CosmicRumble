using UnityEngine;

namespace CosmicRumble.Economy
{
    public enum QuestPeriod { Daily, Weekly, Monthly }

    [CreateAssetMenu(menuName = "CosmicRumble/Economy/QuestDefinition")]
    public class QuestDefinition : ScriptableObject
    {
        public string      questId;
        public string      displayName;
        public string      description;
        public QuestPeriod period;
        public string      trackedEventKey; // AchievementEvents fire metodu adıyla eşleşir
        public int         targetValue;
        public long        rewardXP;
        public long        rewardGold;
        public long        rewardGem;
    }
}
