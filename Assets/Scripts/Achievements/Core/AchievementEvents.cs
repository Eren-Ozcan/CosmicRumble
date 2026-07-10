using System;

namespace CosmicRumble.Achievements
{
    public static class AchievementEvents
    {
        public static event Action         OnMatchWon;
        public static event Action<string> OnMatchLost; // kaybedilen maçta kazananın kimliği (INTIKAM için)
        public static event Action<int>    OnDamageDealt;
        public static event Action<int>    OnDamageTaken;
        public static event Action<string> OnWeaponUsed;
        public static event Action<string> OnAbilityUsed;
        public static event Action         OnHeadshotLanded;
        public static event Action<int>    OnMatchCompleted;
        public static event Action         OnPlanetDestroyed;
        public static event Action<bool>   OnShotFired;
        public static event Action<int>    OnTurnCompleted;
        public static event Action<int>    OnPlayerCountInMatch;
        public static event Action<string> OnPlayerDefeated;
        public static event Action<string> OnDamagedTarget; // her isabette hedef kimliği — HERKESE_MEYDAN gibi "N farklı oyuncu" başarımları için
        public static event Action<string, string> OnDefeatedBy; // (kurbanın adı, öldüreni adı) — INTIKAM'ın "beni kim öldürdü" (maç kazananı DEĞİL) takibi için

        // Additional events for complex achievements
        public static event Action<int>    OnBlackHolePulled;     // enemies pulled count
        public static event Action<int>    OnRpgMultiHit;         // enemies hit in one RPG shot
        public static event Action<int>    OnGrenadeMultiHit;     // enemies hit by one grenade
        public static event Action<bool>   OnShotgunPelletHit;    // all pellets hit = true
        public static event Action         OnBatHammerKnockOff;   // enemy knocked off planet
        public static event Action         OnBatHammerChain;      // victim hit another player
        public static event Action         OnSuperJumpEnemyLand;  // landed on enemy
        public static event Action         OnPlanetChangedViaJump;// jumped to different planet
        public static event Action         OnTeleportKill;        // teleport + attack combo
        public static event Action<int>    OnShieldBlocked;       // damage amount blocked

        // ── SOSYAL (kalan 6, 2026-07-10) ────────────────────────────────────
        public static event Action         OnRankedMatchCompleted; // REKABETCI — dereceli maç bitti (kazan/kaybet fark etmez, 1v1'de her sonuç "top 3" içinde)
        public static event Action<string> OnFriendMatchCompleted; // KOZMIK_EKIP — arkadaş daveti ile kurulan maç bitti, arkadaşın PlayerId'si
        public static event Action<int>    OnLeaderboardRankKnown; // BIR_NUMARA/KOZMIK_AVCI — dereceli maç sonrası öğrenilen 0-tabanlı sıralama

        // Fire methods (null-safe)
        public static void FireMatchWon()                        => OnMatchWon?.Invoke();
        public static void FireMatchLost(string winnerName)      => OnMatchLost?.Invoke(winnerName);
        public static void FireDamageDealt(int amount)           => OnDamageDealt?.Invoke(amount);
        public static void FireDamageTaken(int amount)           => OnDamageTaken?.Invoke(amount);
        public static void FireWeaponUsed(string weaponId)       => OnWeaponUsed?.Invoke(weaponId);
        public static void FireAbilityUsed(string abilityId)     => OnAbilityUsed?.Invoke(abilityId);
        public static void FireHeadshotLanded()                  => OnHeadshotLanded?.Invoke();
        public static void FireMatchCompleted(int shots)         => OnMatchCompleted?.Invoke(shots);
        public static void FirePlanetDestroyed()                 => OnPlanetDestroyed?.Invoke();
        public static void FireShotFired(bool isHit)             => OnShotFired?.Invoke(isHit);
        public static void FireTurnCompleted(int turnCount)      => OnTurnCompleted?.Invoke(turnCount);
        public static void FirePlayerCountInMatch(int count)     => OnPlayerCountInMatch?.Invoke(count);
        public static void FirePlayerDefeated(string id)         => OnPlayerDefeated?.Invoke(id);
        public static void FireDamagedTarget(string id)          => OnDamagedTarget?.Invoke(id);
        public static void FireDefeatedBy(string victimName, string attackerName) => OnDefeatedBy?.Invoke(victimName, attackerName);
        public static void FireBlackHolePulled(int count)        => OnBlackHolePulled?.Invoke(count);
        public static void FireRpgMultiHit(int count)            => OnRpgMultiHit?.Invoke(count);
        public static void FireGrenadeMultiHit(int count)        => OnGrenadeMultiHit?.Invoke(count);
        public static void FireShotgunPelletHit(bool allHit)     => OnShotgunPelletHit?.Invoke(allHit);
        public static void FireBatHammerKnockOff()               => OnBatHammerKnockOff?.Invoke();
        public static void FireBatHammerChain()                  => OnBatHammerChain?.Invoke();
        public static void FireSuperJumpEnemyLand()              => OnSuperJumpEnemyLand?.Invoke();
        public static void FirePlanetChangedViaJump()            => OnPlanetChangedViaJump?.Invoke();
        public static void FireTeleportKill()                    => OnTeleportKill?.Invoke();
        public static void FireShieldBlocked(int amount)         => OnShieldBlocked?.Invoke(amount);
        public static void FireRankedMatchCompleted()             => OnRankedMatchCompleted?.Invoke();
        public static void FireFriendMatchCompleted(string friendId) => OnFriendMatchCompleted?.Invoke(friendId);
        public static void FireLeaderboardRankKnown(int rank)     => OnLeaderboardRankKnown?.Invoke(rank);
    }
}
