using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/UnlockDatabase", fileName = "UnlockDatabase")]
    public class UnlockDatabase : ScriptableObject
    {
        public List<UnlockableItem> allItems = new List<UnlockableItem>();

        public UnlockableItem GetById(string id)
        {
            foreach (var item in allItems)
                if (item != null && item.itemId == id) return item;
            return null;
        }

        public List<UnlockableItem> GetByType(UnlockableType type)
        {
            var result = new List<UnlockableItem>();
            foreach (var item in allItems)
                if (item != null && item.type == type) result.Add(item);
            return result;
        }

        public List<UnlockableItem> GetUnlockedAtLevel(int level)
        {
            var result = new List<UnlockableItem>();
            foreach (var item in allItems)
                if (item != null && item.unlockMethod == UnlockMethod.ByLevel && item.requiredLevel == level)
                    result.Add(item);
            return result;
        }
    }
}
