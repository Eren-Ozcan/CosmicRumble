using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/CostumeDatabase", fileName = "CostumeDatabase")]
    public class CostumeDatabase : ScriptableObject
    {
        public List<CostumeDefinition> allCostumes = new List<CostumeDefinition>();

        public CostumeDefinition GetById(string id)
        {
            foreach (var c in allCostumes)
                if (c != null && c.costumeId == id) return c;
            return null;
        }

        public List<CostumeDefinition> GetByRarity(CostumeRarity r)
        {
            var result = new List<CostumeDefinition>();
            foreach (var c in allCostumes)
                if (c != null && c.rarity == r) result.Add(c);
            return result;
        }

        public List<CostumeDefinition> GetByType(CostumeType t)
        {
            var result = new List<CostumeDefinition>();
            foreach (var c in allCostumes)
                if (c != null && c.costumeType == t) result.Add(c);
            return result;
        }

        public List<CostumeDefinition> GetByTheme(CostumeTheme t)
        {
            var result = new List<CostumeDefinition>();
            foreach (var c in allCostumes)
                if (c != null && c.theme == t) result.Add(c);
            return result;
        }
    }
}
