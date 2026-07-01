using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/ChestConfig")]
    public class ChestConfig : ScriptableObject
    {
        [Header("Günlük limit")]
        public int dailyChestLimit = 3;

        [Header("Drop oranları (toplam 100)")]
        public float commonChance = 65f;
        public float rareChance   = 25f;
        public float epicChance   = 10f;

        [Header("Gold aralıkları")]
        public int commonGoldMin = 50;
        public int commonGoldMax = 150;
        public int rareGoldMin   = 200;
        public int rareGoldMax   = 400;
        public int epicGoldMin   = 500;
        public int epicGoldMax   = 800;

        [Header("Gem")]
        public int rareGem  = 5;
        public int epicGem  = 15;

        [Header("Kostüm drop şansı (0-1)")]
        public float rareCosstumeProbability  = 0.05f;
        public float epicCostumeProbability   = 0.15f;
    }
}
