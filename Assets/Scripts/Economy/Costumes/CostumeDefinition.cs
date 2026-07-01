using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/CostumeDefinition", fileName = "CostumeDefinition")]
    public class CostumeDefinition : ScriptableObject
    {
        public string        costumeId;
        public string        displayName;
        public Sprite        previewSprite;       // null-safe: UI shows placeholder
        public CostumeType   costumeType;
        public CostumeRarity rarity;
        public CostumeTheme  theme;
        public CostumeUnlock unlockMethod;

        public int    requiredLevel;
        public long   goldCost;
        public long   gemCost;
        public string requiredAchievementId;
        public string unlockDescription;
    }
}
