using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Achievements
{
    [CreateAssetMenu(menuName = "CosmicRumble/AchievementDatabase", fileName = "AchievementDatabase")]
    public class AchievementDatabase : ScriptableObject
    {
        public List<AchievementDefinition> allAchievements = new List<AchievementDefinition>();

        public AchievementDefinition GetById(string id)
        {
            foreach (var a in allAchievements)
                if (a != null && a.achievementId == id) return a;
            return null;
        }

        public List<AchievementDefinition> GetByRarity(AchievementRarity r)
        {
            var result = new List<AchievementDefinition>();
            foreach (var a in allAchievements)
                if (a != null && a.rarity == r) result.Add(a);
            return result;
        }
    }
}
