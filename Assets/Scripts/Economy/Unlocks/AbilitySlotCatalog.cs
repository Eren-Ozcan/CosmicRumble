namespace CosmicRumble.Economy
{
    /// <summary>
    /// Silah/skill tray slot indeksi ↔ UnlockableItem itemId eşlemesi. Slot düzeni UIManager
    /// başlığındaki sırayla birebir aynıdır:
    /// 0=Pistol, 1=Shotgun, 2=RPG, 3=Grenade, 4=SuperJump, 5=Shield, 6=Teleport,
    /// 7=Bat/Hammer, 8=BlackHole, 9=Bomb.
    /// itemId'ler AchievementTracker/AnnounceFire'ın kullandığı id şemasıyla ve
    /// Resources/Economy/Unlocks altındaki asset'lerle aynıdır.
    /// </summary>
    public static class AbilitySlotCatalog
    {
        private static readonly string[] SlotItemIds =
        {
            "weapon_pistol",   // 0
            "weapon_shotgun",  // 1
            "weapon_rpg",      // 2
            "weapon_grenade",  // 3
            "skill_superjump", // 4
            "skill_shield",    // 5
            "skill_teleport",  // 6
            "skill_bathammer", // 7
            "skill_blackhole", // 8
            "weapon_bomb"      // 9
        };

        public static string GetItemId(int slotIndex) =>
            slotIndex >= 0 && slotIndex < SlotItemIds.Length ? SlotItemIds[slotIndex] : null;

        /// <summary>
        /// UnlockManager yoksa fail-open (her slot açık): Game sahnesi Editor'de doğrudan
        /// açıldığında menü bootstrap'i hiç koşmaz ve ekonomi singleton'ları var olmaz —
        /// bu durumda oynanışı kilitlemek yerine kapı devre dışı kalır. Kapı yalnızca
        /// gerçek oyuncu profili (menüden gelinen normal akış) varken uygulanır.
        /// </summary>
        public static bool IsSlotUnlocked(int slotIndex)
        {
            string id = GetItemId(slotIndex);
            if (id == null) return false;
            var mgr = UnlockManager.Instance;
            return mgr == null || mgr.IsUnlocked(id);
        }

        /// <summary>Kilitli slotun UI'da göstereceği gereken seviye (bilinmiyorsa 0).</summary>
        public static int GetRequiredLevel(int slotIndex)
        {
            string id = GetItemId(slotIndex);
            if (id == null) return 0;
            var item = UnlockManager.Instance?.GetItemById(id);
            return item != null ? item.requiredLevel : 0;
        }
    }
}
