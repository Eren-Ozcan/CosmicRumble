using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Achievements
{
    /// <summary>
    /// Listens to AchievementEvents, maintains counters, and drives AchievementManager.
    /// All per-match state resets on match start (FirePlayerCountInMatch with count > 0 used as proxy,
    /// but explicit ResetMatchState() can be called by TurnManager).
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        public static AchievementTracker Instance { get; private set; }

        // ── Cumulative counters (persist across sessions via AchievementManager progress) ──
        private long _totalMatchesWon;
        private long _totalMatchesPlayed;
        private long _totalDamageDealt;
        private long _totalDamageTaken;
        private long _totalShotsFired;
        private long _totalShotsHit;
        private long _totalHeadshots;
        private long _totalPlanetsDestroyed;
        private long _totalBlackHolePulls;
        private long _totalRpgShots;
        private long _totalGrenadeShots;
        private long _totalTeleportUses;
        private long _shieldBlockedDamage;
        private long _totalDuelWins;

        // ── Per-match state ──
        private int  _matchShotsFired;
        private int  _matchShotsHit;
        private int  _matchDamageTaken;
        private int  _matchTurnCount;
        private int  _matchTeleportUses;
        private int  _matchShieldBlocks;
        private int  _matchPlayerCount;
        private int  _consecutiveShotgunVictims;

        private readonly HashSet<string> _weaponsUsedInMatch   = new HashSet<string>();
        private readonly HashSet<string> _uniqueOpponents      = new HashSet<string>();
        private readonly HashSet<string> _abilitiesUsedInMatch = new HashSet<string>();
        private readonly HashSet<string> _matchDamagedTargets  = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()  => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            AchievementEvents.OnMatchWon            += HandleMatchWon;
            AchievementEvents.OnMatchLost           += HandleMatchLost;
            AchievementEvents.OnRankedMatchCompleted += HandleRankedMatchCompleted;
            AchievementEvents.OnFriendMatchCompleted += HandleFriendMatchCompleted;
            AchievementEvents.OnLeaderboardRankKnown += HandleLeaderboardRankKnown;
            AchievementEvents.OnDamageDealt         += HandleDamageDealt;
            AchievementEvents.OnDamageTaken         += HandleDamageTaken;
            AchievementEvents.OnWeaponUsed          += HandleWeaponUsed;
            AchievementEvents.OnAbilityUsed         += HandleAbilityUsed;
            AchievementEvents.OnHeadshotLanded      += HandleHeadshot;
            AchievementEvents.OnMatchCompleted      += HandleMatchCompleted;
            AchievementEvents.OnPlanetDestroyed     += HandlePlanetDestroyed;
            AchievementEvents.OnShotFired           += HandleShotFired;
            AchievementEvents.OnTurnCompleted       += HandleTurnCompleted;
            AchievementEvents.OnPlayerCountInMatch  += HandlePlayerCount;
            AchievementEvents.OnPlayerDefeated      += HandlePlayerDefeated;
            AchievementEvents.OnDamagedTarget       += HandleDamagedTarget;
            AchievementEvents.OnBlackHolePulled     += HandleBlackHolePulled;
            AchievementEvents.OnRpgMultiHit         += HandleRpgMultiHit;
            AchievementEvents.OnGrenadeMultiHit     += HandleGrenadeMultiHit;
            AchievementEvents.OnShotgunPelletHit    += HandleShotgunPelletHit;
            AchievementEvents.OnBatHammerKnockOff   += HandleBatHammerKnockOff;
            AchievementEvents.OnBatHammerChain      += HandleBatHammerChain;
            AchievementEvents.OnSuperJumpEnemyLand  += HandleSuperJumpEnemyLand;
            AchievementEvents.OnPlanetChangedViaJump+= HandlePlanetChangedViaJump;
            AchievementEvents.OnTeleportKill        += HandleTeleportKill;
            AchievementEvents.OnShieldBlocked       += HandleShieldBlocked;
        }

        private void Unsubscribe()
        {
            AchievementEvents.OnMatchWon            -= HandleMatchWon;
            AchievementEvents.OnMatchLost           -= HandleMatchLost;
            AchievementEvents.OnRankedMatchCompleted -= HandleRankedMatchCompleted;
            AchievementEvents.OnFriendMatchCompleted -= HandleFriendMatchCompleted;
            AchievementEvents.OnLeaderboardRankKnown -= HandleLeaderboardRankKnown;
            AchievementEvents.OnDamageDealt         -= HandleDamageDealt;
            AchievementEvents.OnDamageTaken         -= HandleDamageTaken;
            AchievementEvents.OnWeaponUsed          -= HandleWeaponUsed;
            AchievementEvents.OnAbilityUsed         -= HandleAbilityUsed;
            AchievementEvents.OnHeadshotLanded      -= HandleHeadshot;
            AchievementEvents.OnMatchCompleted      -= HandleMatchCompleted;
            AchievementEvents.OnPlanetDestroyed     -= HandlePlanetDestroyed;
            AchievementEvents.OnShotFired           -= HandleShotFired;
            AchievementEvents.OnTurnCompleted       -= HandleTurnCompleted;
            AchievementEvents.OnPlayerCountInMatch  -= HandlePlayerCount;
            AchievementEvents.OnPlayerDefeated      -= HandlePlayerDefeated;
            AchievementEvents.OnDamagedTarget       -= HandleDamagedTarget;
            AchievementEvents.OnBlackHolePulled     -= HandleBlackHolePulled;
            AchievementEvents.OnRpgMultiHit         -= HandleRpgMultiHit;
            AchievementEvents.OnGrenadeMultiHit     -= HandleGrenadeMultiHit;
            AchievementEvents.OnShotgunPelletHit    -= HandleShotgunPelletHit;
            AchievementEvents.OnBatHammerKnockOff   -= HandleBatHammerKnockOff;
            AchievementEvents.OnBatHammerChain      -= HandleBatHammerChain;
            AchievementEvents.OnSuperJumpEnemyLand  -= HandleSuperJumpEnemyLand;
            AchievementEvents.OnPlanetChangedViaJump-= HandlePlanetChangedViaJump;
            AchievementEvents.OnTeleportKill        -= HandleTeleportKill;
            AchievementEvents.OnShieldBlocked       -= HandleShieldBlocked;
        }

        // ── Public reset called by TurnManager at match start ────────────────
        public void ResetMatchState()
        {
            _matchShotsFired          = 0;
            _matchShotsHit            = 0;
            _matchDamageTaken         = 0;
            _matchTurnCount           = 0;
            _matchTeleportUses        = 0;
            _matchShieldBlocks        = 0;
            _matchPlayerCount         = 0;
            _consecutiveShotgunVictims = 0;
            _weaponsUsedInMatch.Clear();
            _abilitiesUsedInMatch.Clear();
            _matchDamagedTargets.Clear();
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void HandleMatchWon()
        {
            _totalMatchesWon++;
            _totalMatchesPlayed++;
            var am = AchievementManager.Instance;
            if (am == null) return;

            am.UpdateProgress("FIRST_BLOOD",    (int)_totalMatchesWon);
            am.UpdateProgress("VETERAN_10",     (int)_totalMatchesWon);
            am.UpdateProgress("SAVAS_MAKINESI", (int)_totalMatchesWon);
            am.UpdateProgress("EFSANE",         (int)_totalMatchesWon);
            am.UpdateProgress("COSMIC_100",     (int)_totalMatchesWon);

            if (_matchDamageTaken == 0)
                am.UnlockAchievement("FLAWLESS");

            if (_matchTurnCount <= 5)
                am.UnlockAchievement("HIZLI_BITIR");

            if (_matchPlayerCount >= 8)
                am.UnlockAchievement("SAMPIYONLAR");

            if (_matchPlayerCount == 2)
            {
                _totalDuelWins++;
                am.UpdateProgress("DUELLO_SAMPIYONU", (int)_totalDuelWins);
            }

            if (_weaponsUsedInMatch.Count >= 5)
                am.UnlockAchievement("TAM_CEPHANE");

            if (_matchTeleportUses >= 5)
                am.UnlockAchievement("KUANTUM");

            if (_matchShieldBlocks >= 3)
                am.UnlockAchievement("KALKAN_DUVARI");

            if (_matchShotsFired >= 30)
                am.UpdateProgress("TETIKCI", _matchShotsFired);

            // Accuracy: min 10 shots, >= 80% hit
            if (_matchShotsFired >= 10)
            {
                float acc = (float)_matchShotsHit / _matchShotsFired;
                if (acc >= 0.8f) am.UnlockAchievement("ISABETLI");
            }
        }

        private const string RevengeTargetPrefKey = "cr_intikam_target";

        private void HandleMatchLost(string winnerName)
        {
            _totalMatchesPlayed++;

            // INTIKAM: bir sonraki maçta bizi yenen kişiyi yenersek tetiklenecek — 1v1 olduğu için
            // "kim" sorusu her zaman tek bir rakip, saldırgan kimliğini projectile boru hattına
            // taşımaya gerek yok (bkz. HandlePlayerDefeated'daki aynı yaklaşım/SOSYAL_KELEBEK notu).
            if (!string.IsNullOrEmpty(winnerName))
            {
                PlayerPrefs.SetString(RevengeTargetPrefKey, winnerName);
                PlayerPrefs.Save();
            }
        }

        private void HandleDamageDealt(int amount)
        {
            _totalDamageDealt += amount;
            var am = AchievementManager.Instance;
            if (am == null) return;
            am.UpdateProgress("DAMAGE_1K",   (int)Mathf.Min(_totalDamageDealt, int.MaxValue));
            am.UpdateProgress("DAMAGE_50K",  (int)Mathf.Min(_totalDamageDealt, int.MaxValue));
            am.UpdateProgress("DAMAGE_250K", (int)Mathf.Min(_totalDamageDealt, int.MaxValue));
        }

        private void HandleDamageTaken(int amount)
        {
            _matchDamageTaken   += amount;
            _totalDamageTaken   += amount;
            var am = AchievementManager.Instance;
            if (am == null) return;
            am.UpdateProgress("SAGLAMDURUG", (int)Mathf.Min(_totalDamageTaken, int.MaxValue));
        }

        private void HandleWeaponUsed(string weaponId)
        {
            _weaponsUsedInMatch.Add(weaponId);
            var am = AchievementManager.Instance;
            if (am == null) return;

            if (weaponId == "weapon_pistol")
            {
                am.UpdateProgress("TABANCALI", (int)(am.GetProgress("TABANCALI") + 1));
            }
            else if (weaponId == "weapon_rpg")
            {
                _totalRpgShots++;
                am.UpdateProgress("PATLAMA_UZMANI", (int)_totalRpgShots);
            }
            else if (weaponId == "weapon_grenade")
            {
                _totalGrenadeShots++;
                am.UpdateProgress("PIM_CEKICI", (int)_totalGrenadeShots);
            }
        }

        private void HandleAbilityUsed(string abilityId)
        {
            _abilitiesUsedInMatch.Add(abilityId);

            if (abilityId == "skill_teleport")
            {
                _matchTeleportUses++;
                _totalTeleportUses++;
            }
        }

        private void HandleHeadshot()
        {
            _totalHeadshots++;
            AchievementManager.Instance?.UpdateProgress("KESKIN_NISANCI", (int)_totalHeadshots);
        }

        private void HandleMatchCompleted(int shots)
        {
            _matchShotsFired = shots;
            var am = AchievementManager.Instance;
            if (am == null) return;
            am.UpdateProgress("GALAKSI_TAMIRCISI", (int)_totalMatchesPlayed);
        }

        private void HandlePlanetDestroyed()
        {
            _totalPlanetsDestroyed++;
            AchievementManager.Instance?.UpdateProgress("GEZEGEN_KATILI", (int)_totalPlanetsDestroyed);
        }

        private void HandleShotFired(bool isHit)
        {
            _totalShotsFired++;
            _matchShotsFired++;
            if (isHit)
            {
                _totalShotsHit++;
                _matchShotsHit++;
            }
            var am = AchievementManager.Instance;
            if (am == null) return;
            am.UpdateProgress("SHOTS_100", (int)_totalShotsFired);
            am.UpdateProgress("SHOTS_1K",  (int)_totalShotsFired);
        }

        private void HandleTurnCompleted(int turnCount)
        {
            _matchTurnCount = turnCount;
        }

        private void HandlePlayerCount(int count)
        {
            _matchPlayerCount = count;
        }

        private void HandlePlayerDefeated(string playerId)
        {
            _uniqueOpponents.Add(playerId);
            AchievementManager.Instance?.UpdateProgress("SOSYAL_KELEBEK", _uniqueOpponents.Count);

            // INTIKAM: az önce yendiğimiz kişi, önceki maçta bizi yenen kişiyle aynıysa tetiklenir.
            if (playerId == PlayerPrefs.GetString(RevengeTargetPrefKey, ""))
            {
                AchievementManager.Instance?.UnlockAchievement("INTIKAM");
                PlayerPrefs.DeleteKey(RevengeTargetPrefKey);
                PlayerPrefs.Save();
            }
        }

        // ── SOSYAL (kalan 6, 2026-07-10) ────────────────────────────────────

        /// <summary>REKABETCI: dereceli (Quick Match) bir maç tamamlandı — 1v1'de kazan/kaybet fark
        /// etmez, her sonuç zaten "top 3" (2 oyuncudan biri) içindedir.</summary>
        private void HandleRankedMatchCompleted()
        {
            AchievementManager.Instance?.UnlockAchievement("REKABETCI");
        }

        private const string FriendMatchCountPrefPrefix = "cr_friend_matches_";

        /// <summary>KOZMIK_EKIP: "aynı 3 kişiyle 5 maç" — bu proje her zaman 1v1 olduğu için "aynı
        /// arkadaşla 5 maç" olarak ölçeklendirildi. Her arkadaş için ayrı sayaç tutulur, ilerleme o
        /// arkadaşın sayacına göre güncellenir (başka bir arkadaşla oynamak ilerlemeyi düşürmez).</summary>
        private void HandleFriendMatchCompleted(string friendId)
        {
            if (string.IsNullOrEmpty(friendId)) return;
            string key = FriendMatchCountPrefPrefix + friendId;
            int count = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, count);
            PlayerPrefs.Save();

            var am = AchievementManager.Instance;
            if (am == null) return;
            int best = Mathf.Max(am.GetProgress("KOZMIK_EKIP"), count);
            am.UpdateProgress("KOZMIK_EKIP", best);
        }

        /// <summary>BIR_NUMARA/KOZMIK_AVCI: dereceli maç sonrası öğrenilen 0-tabanlı leaderboard
        /// sıralaması (bkz. LeaderboardManager.ReportOnlineMatchResult → FetchOwnEntryAsync).</summary>
        private void HandleLeaderboardRankKnown(int rank)
        {
            var am = AchievementManager.Instance;
            if (am == null) return;
            if (rank == 0)      am.UnlockAchievement("BIR_NUMARA");
            if (rank < 10)      am.UnlockAchievement("KOZMIK_AVCI");
        }

        private void HandleDamagedTarget(string targetId)
        {
            _matchDamagedTargets.Add(targetId);
            if (_matchDamagedTargets.Count >= 7)
                AchievementManager.Instance?.UnlockAchievement("HERKESE_MEYDAN");
        }

        private void HandleBlackHolePulled(int count)
        {
            _totalBlackHolePulls += count;
            var am = AchievementManager.Instance;
            if (am == null) return;
            am.UpdateProgress("OLAY_UFKU", (int)_totalBlackHolePulls);
            if (count >= 3)
                am.UnlockAchievement("KARA_DELIK_USTASI");
        }

        private void HandleRpgMultiHit(int count)
        {
            if (count >= 3)
                AchievementManager.Instance?.UnlockAchievement("ROKETCI");
        }

        private void HandleGrenadeMultiHit(int count)
        {
            if (count >= 2)
                AchievementManager.Instance?.UnlockAchievement("EL_BOMBACI");
        }

        private void HandleShotgunPelletHit(bool allHit)
        {
            if (allHit)
                AchievementManager.Instance?.UnlockAchievement("SACMA_YAGMURU");

            if (allHit)
            {
                _consecutiveShotgunVictims++;
                if (_consecutiveShotgunVictims >= 5)
                    AchievementManager.Instance?.UnlockAchievement("POMPACI");
            }
            else
            {
                _consecutiveShotgunVictims = 0;
            }
        }

        private void HandleBatHammerKnockOff()
        {
            AchievementManager.Instance?.UnlockAchievement("CEKIC_ZAMANI");
        }

        private void HandleBatHammerChain()
        {
            AchievementManager.Instance?.UnlockAchievement("HOME_RUN");
        }

        private void HandleSuperJumpEnemyLand()
        {
            AchievementManager.Instance?.UnlockAchievement("SUPER_KAHRAMAN");
        }

        private void HandlePlanetChangedViaJump()
        {
            AchievementManager.Instance?.UnlockAchievement("YORUNGE");
        }

        private void HandleTeleportKill()
        {
            AchievementManager.Instance?.UnlockAchievement("ISINLANAN");
        }

        private void HandleShieldBlocked(int amount)
        {
            _matchShieldBlocks++;
            _shieldBlockedDamage += amount;
            AchievementManager.Instance?.UpdateProgress("DOKUNULMAZ", (int)_shieldBlockedDamage);
        }
    }
}
