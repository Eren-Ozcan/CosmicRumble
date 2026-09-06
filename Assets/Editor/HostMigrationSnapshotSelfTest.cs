// Assets/Editor/HostMigrationSnapshotSelfTest.cs
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Data;
using CosmicRumble.Networking;

namespace CosmicRumble.EditorTools
{
    /// <summary>
    /// Host migration snapshot'ının serileştirmesini canlı maç kurmadan doğrular. Projede
    /// asmdef tabanlı bir test derlemesi yok (tüm oyun kodu Assembly-CSharp içinde ve asmdef'ler
    /// onu referans alamaz), bu yüzden NUnit yerine editörden ve batchmode'dan çalışan bir
    /// öz-denetim: Serialize/Deserialize saf veri dönüşümü olduğu için sahne gerektirmez.
    ///
    /// Menüden: Tools > Host Migration > Run Snapshot Self-Test
    /// Headless:
    ///   Unity.exe -batchmode -quit -projectPath &lt;proje&gt; \
    ///     -executeMethod CosmicRumble.EditorTools.HostMigrationSnapshotSelfTest.RunBatch
    /// Başarısızlıkta batchmode 1 ile çıkar, böylece bir CI adımı olarak kullanılabilir.
    ///
    /// Kapsam: gidiş-dönüş sadakati, sürüm reddi ve 8 oyuncuda snapshot boyutu (TEST_PLAN HM-18).
    /// Kapsam DIŞI: gerçek migration akışı — o üç süreçli canlı test ister (HM-01..HM-07).
    /// </summary>
    public static class HostMigrationSnapshotSelfTest
    {
        /// <summary>Lobby'nin bir veri alanına sığması gereken üst sınır. UGS Lobby metin
        /// değerlerinde binlerce karakter kabul eder; snapshot base64'e genişleyeceği için
        /// burada ham bayt üzerinden rahat bir tavan tutulur.</summary>
        const int k_MaxSnapshotBytes = 4096;

        [MenuItem("Tools/Host Migration/Run Snapshot Self-Test")]
        public static void RunFromMenu()
        {
            var failures = RunAll(out string report);
            if (failures == 0) Debug.Log(report);
            else               Debug.LogError(report);
        }

        public static void RunBatch()
        {
            var failures = RunAll(out string report);
            Debug.Log(report);
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static int RunAll(out string report)
        {
            var log = new StringBuilder("[HM self-test]\n");
            int failures = 0;

            failures += Check(log, "round-trip preserves every field", RoundTripPreservesEveryField);
            failures += Check(log, "wrong version is rejected",        WrongVersionIsRejected);
            failures += Check(log, "8-player snapshot fits the lobby", EightPlayerSnapshotFits);

            log.Append(failures == 0 ? "ALL PASSED" : $"{failures} FAILED");
            report = log.ToString();
            return failures;
        }

        static int Check(StringBuilder log, string name, Func<string> test)
        {
            string error;
            try { error = test(); }
            catch (Exception e) { error = $"threw {e.GetType().Name}: {e.Message}"; }

            log.AppendLine(error == null ? $"  PASS  {name}" : $"  FAIL  {name} — {error}");
            return error == null ? 0 : 1;
        }

        // ── Testler ───────────────────────────────────────────────────────────────────

        static string RoundTripPreservesEveryField()
        {
            var original = BuildSnapshot(3);
            var restored = HostMigrationDataHandler.Deserialize(HostMigrationDataHandler.Serialize(original));

            if (restored == null) return "Deserialize returned null";

            if (restored.Mode              != original.Mode)              return "Mode differs";
            if (restored.FfaPlayerCount    != original.FfaPlayerCount)    return "FfaPlayerCount differs";
            if (restored.IsRanked          != original.IsRanked)          return "IsRanked differs";
            if (restored.ActiveTurnIndex   != original.ActiveTurnIndex)   return "ActiveTurnIndex differs";
            if (restored.TurnNumber        != original.TurnNumber)        return "TurnNumber differs";
            if (!Mathf.Approximately(restored.RemainingTurnTime, original.RemainingTurnTime))
                return $"RemainingTurnTime {restored.RemainingTurnTime} != {original.RemainingTurnTime}";
            if (restored.Players.Count     != original.Players.Count)     return "player count differs";

            for (int i = 0; i < original.Players.Count; i++)
            {
                var a = original.Players[i];
                var b = restored.Players[i];

                if (b.UgsPlayerId != a.UgsPlayerId) return $"[{i}] UgsPlayerId differs";
                if (b.DisplayName != a.DisplayName) return $"[{i}] DisplayName differs";
                if (b.TeamId      != a.TeamId)      return $"[{i}] TeamId differs";
                if (!Mathf.Approximately(b.Health, a.Health)) return $"[{i}] Health differs";
                if (b.IsShielded  != a.IsShielded)  return $"[{i}] IsShielded differs";
                if (b.Position    != a.Position)    return $"[{i}] Position differs";
                if (b.UpDirection != a.UpDirection) return $"[{i}] UpDirection differs";
                if (b.Velocity    != a.Velocity)    return $"[{i}] Velocity differs";
                if (!b.Ammo.Equals(a.Ammo))         return $"[{i}] Ammo differs";
                if (b.HasUsedSkillThisTurn != a.HasUsedSkillThisTurn) return $"[{i}] skill lock differs";

                // Sıra düzeni oyuncu listesiyle aynı sırada olmalı — migration sonrası sırayı
                // bununla kuruyoruz, kayması sırayı yanlış oyuncuya verirdi.
                if (restored.TurnOrder[i] != a.UgsPlayerId) return $"[{i}] TurnOrder out of step";
            }

            if (restored.Explosions.Count != original.Explosions.Count) return "explosion count differs";
            for (int i = 0; i < original.Explosions.Count; i++)
            {
                var a = original.Explosions[i];
                var b = restored.Explosions[i];
                if (b.PlanetIndex != a.PlanetIndex) return $"[explosion {i}] PlanetIndex differs";
                if (b.Pos         != a.Pos)         return $"[explosion {i}] Pos differs";
                if (!Mathf.Approximately(b.Radius, a.Radius)) return $"[explosion {i}] Radius differs";
            }

            return null;
        }

        static string WrongVersionIsRejected()
        {
            var bytes = HostMigrationDataHandler.Serialize(BuildSnapshot(2));
            bytes[0] = 99;   // sürüm baytı

            // Eski/ileri sürümlü bir snapshot'ı okumak sessizce yanlış bir maç kurardı.
            LogAssert_ExpectWarning();
            return HostMigrationDataHandler.Deserialize(bytes) == null
                ? null
                : "a snapshot with an unknown version byte was accepted";
        }

        static string EightPlayerSnapshotFits()
        {
            int size = HostMigrationDataHandler.Serialize(BuildSnapshot(8)).Length;
            return size <= k_MaxSnapshotBytes
                ? null
                : $"8-player snapshot is {size} bytes, over the {k_MaxSnapshotBytes} byte budget";
        }

        // ── Yardımcılar ───────────────────────────────────────────────────────────────

        /// <summary>Her alanı birbirinden ayırt edilebilir kılan sentetik snapshot — alanların
        /// yer değiştirmesi (ör. Position ile Velocity) ancak böyle yakalanır.</summary>
        static HostMigrationSnapshot BuildSnapshot(int playerCount)
        {
            var snapshot = new HostMigrationSnapshot
            {
                Mode              = GameModeType.Ffa,
                FfaPlayerCount    = playerCount,
                IsRanked          = true,
                ActiveTurnIndex   = Mathf.Max(0, playerCount - 2),
                RemainingTurnTime = 7.25f,
                TurnNumber        = 42,
                Players           = new List<HostMigrationPlayerState>(),
            };

            for (int i = 0; i < playerCount; i++)
            {
                var p = new HostMigrationPlayerState
                {
                    // Gerçekçi uzunlukta: UGS PlayerId 28 karakterlik bir kimliktir.
                    UgsPlayerId = $"ugs-player-id-{i:D2}-0123456789",
                    DisplayName = $"Oyuncu {i} ✦",     // ASCII dışı: UTF-8 yolu da sınansın
                    TeamId      = i % 4,
                    Health      = 100f - i * 7.5f,
                    IsShielded  = (i % 2) == 0,
                    Position    = new Vector2(i * 1.5f, -i * 2.25f),
                    UpDirection = new Vector2(0f, 1f),
                    Velocity    = new Vector2(-i * 0.5f, i * 0.75f),
                    Ammo = new AmmoState
                    {
                        superJumps  = 3 - (i % 4),
                        rpgAmmo     = 4 - (i % 5),
                        pistolAmmo  = -1,          // sınırsız
                        shotgunAmmo = 5 - (i % 6),
                        grenades    = 2 - (i % 3),
                        shields     = 1 - (i % 2),
                    },
                    HasUsedSkillThisTurn = (i % 3) == 0,
                };

                snapshot.Players.Add(p);
                snapshot.TurnOrder.Add(p.UgsPlayerId);
            }

            // Maç başına makul bir patlama sayısı simüle edilir (oyuncu başına ~3 tur) —
            // 8-player bütçe testi bunu da hesaba katsın diye gerçekçi bırakılır.
            for (int i = 0; i < playerCount * 3; i++)
            {
                snapshot.Explosions.Add(new PlanetExplosionRecord(
                    planetIndex: i % 2,
                    pos: new Vector2(i * 0.3f, -i * 0.2f),
                    radius: 1.5f + (i % 4) * 0.25f));
            }

            return snapshot;
        }

        /// <summary>Sürüm reddi testi bilerek bir uyarı bastırır; konsolda hata gibi görünmesin
        /// diye beklendiği not düşülür (NUnit'in LogAssert'i burada yok).</summary>
        static void LogAssert_ExpectWarning() =>
            Debug.Log("[HM self-test] the next '[HM] Deserialize: snapshot version ...' warning is expected");
    }
}
