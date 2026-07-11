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

        [Header("Platform IDs (doldurulunca aktifleşir — boşsa achievementId kullanılır)")]
        [Tooltip("Steamworks Admin API name")]
        public string steamId;
        [Tooltip("Play Console'un ürettiği opak ID (CgkI...)")]
        public string googlePlayId;
        [Tooltip("App Store Connect Game Center achievement ID")]
        public string gameCenterId;

        public string SteamId      => string.IsNullOrEmpty(steamId)      ? achievementId : steamId;
        public string GooglePlayId => string.IsNullOrEmpty(googlePlayId) ? achievementId : googlePlayId;
        public string GameCenterId => string.IsNullOrEmpty(gameCenterId) ? achievementId : gameCenterId;
    }
}
