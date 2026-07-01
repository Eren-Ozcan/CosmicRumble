using UnityEngine;

namespace CosmicRumble.Economy
{
    public enum UnlockableType { Weapon, Skill, Cosmetic }
    public enum UnlockMethod   { Default, ByLevel, ByGold, ByGem, ByAchievement }

    [CreateAssetMenu(menuName = "CosmicRumble/Economy/UnlockableItem", fileName = "UnlockableItem")]
    public class UnlockableItem : ScriptableObject
    {
        public string        itemId;
        public string        displayName;
        public Sprite        icon;
        public UnlockableType  type;
        public UnlockMethod    unlockMethod;
        public int           requiredLevel;
        public long          goldCost;
        public long          gemCost;
        public string        requiredAchievementId;
        public bool          isDefault;
    }
}
