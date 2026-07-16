using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/CostumeDefinition", fileName = "CostumeDefinition")]
    public class CostumeDefinition : ScriptableObject
    {
        public string        costumeId;
        public string        displayName;
        /// <summary>
        /// Hangi karaktere ait (1-5). 2026-07-16 yeniden tasarımı: 150 serbest kostüm yerine
        /// 5 karakter × 3 kostüm — karakter isimleri şimdilik jenerik ("Character N"),
        /// gerçek isim/tema kostüm sanatı tasarlanırken verilecek (yalnız veri değişir).
        /// </summary>
        public int           characterId;
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
