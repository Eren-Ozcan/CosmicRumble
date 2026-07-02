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

        [Header("AbilityUsed / WeaponUsed için opsiyonel")]
        [Tooltip("Boş bırakılırsa trackedEventKey'deki her olay sayılır. Doluysa sadece bu id (ör. skill_blackhole, weapon_pistol) sayılır.")]
        public string      requiredId;
        [Tooltip("İşaretliyse ilerleme +1 artmaz; bunun yerine görülen farklı id sayısı (distinct count) ilerlemeyi belirler.")]
        public bool        distinctTracking;
    }
}
