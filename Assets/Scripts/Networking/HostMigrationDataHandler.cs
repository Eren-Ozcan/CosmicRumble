// Assets/Scripts/Networking/HostMigrationDataHandler.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Services.Multiplayer;
using UnityEngine;
using CosmicRumble.Data;
using CosmicRumble.Utilities;

namespace CosmicRumble.Networking
{
    /// <summary>
    /// Bir oyuncunun migration'da taşınan durumu. Anahtar HER ZAMAN UGS PlayerId'dir —
    /// NGO clientId ve NetworkObjectId migration sonrası değişir.
    /// </summary>
    public struct HostMigrationPlayerState
    {
        public string  UgsPlayerId;
        public string  DisplayName;
        public int     TeamId;

        public float   Health;
        public bool    IsShielded;

        public Vector2 Position;
        public Vector2 UpDirection;   // karakterin gezegen yüzeyindeki "yukarı"sı (transform.up)
        public Vector2 Velocity;

        public AmmoState Ammo;
        public bool    HasUsedSkillThisTurn;
    }

    /// <summary>
    /// Host migration sırasında taşınan maç durumu. FAZ 2 kapsamı: maç yapılandırması, sıra
    /// düzeni/tur durumu ve oyuncu başına can/kalkan/konum/hız/cephane. Gezegen tahribatı hâlâ
    /// taşınmıyor (Faz 3) ve uçuştaki mermiler bilerek taşınmıyor — kesilen tur baştan başlar.
    /// Ayrıntılı plan: docs/HOST_MIGRATION_PLAN.md
    /// </summary>
    public class HostMigrationSnapshot
    {
        public GameModeType  Mode;
        public int           FfaPlayerCount;
        public bool          IsRanked;

        /// <summary>Sıra düzeni — UGS PlayerId listesi. NGO clientId/NetworkObjectId migration
        /// sonrası DEĞİŞİR, bu yüzden hiçbir şey onlarla anahtarlanmaz.</summary>
        public List<string>  TurnOrder = new List<string>();

        /// <summary>TurnOrder içinde o an sırası olan oyuncunun indeksi.</summary>
        public int           ActiveTurnIndex;

        /// <summary>Aktif turun kalan süresi (saniye) — yeni host'ta tur baştan başlamasın diye.</summary>
        public float         RemainingTurnTime;

        /// <summary>Maç başından beri oynanan tur sayısı.</summary>
        public int           TurnNumber;

        /// <summary>Oyuncu durumları; sıra <see cref="TurnOrder"/> ile birebir aynıdır.</summary>
        public List<HostMigrationPlayerState> Players = new List<HostMigrationPlayerState>();

        public override string ToString() =>
            $"mode={Mode} ffa={FfaPlayerCount} ranked={IsRanked} players={Players.Count} " +
            $"active={ActiveTurnIndex} turn={TurnNumber} remaining={RemainingTurnTime:F1}s";
    }

    /// <summary>
    /// SDK'nın host migration verisini üretip uygulayan köprü. Host, maç sürerken
    /// <see cref="Generate"/>'i periyodik çağırır ve sonucu Lobby'ye yükler; host düşünce yeni
    /// seçilen host'ta <see cref="Apply"/> bir kez çağrılır.
    ///
    /// KRİTİK SIRALAMA: Apply(), NetworkModule içinde ResetAsync() ile StartRelayNetworkAsync()
    /// ARASINDA çalışır — yani NetworkManager KAPALIYKEN. Burada hiçbir şey spawn edilemez ve
    /// hiçbir NetworkVariable yazılamaz. Bu yüzden Apply yalnızca çözümleyip <see cref="Pending"/>
    /// içine bırakır; sahneyi gerçekten kuran taraf, host başladıktan sonra bu snapshot'ı tüketen
    /// NetworkPlayerSpawner.RebuildFromMigrationSnapshot'tır.
    /// </summary>
    public class HostMigrationDataHandler : IMigrationDataHandler
    {
        /// <summary>Serileştirme sürümü — ileride alan eklenirse eski snapshot'ı sessizce
        /// yanlış okumak yerine reddedebilmek için. v1: yalnız maç yapılandırması + sıra düzeni.
        /// v2: tur durumu + oyuncu başına can/kalkan/konum/hız/cephane.</summary>
        const byte k_Version = 2;

        /// <summary>Yeni host'ta Apply() ile bırakılan, henüz uygulanmamış snapshot.
        /// Tüketen taraf işi bitince <see cref="ConsumePending"/> çağırmalı.</summary>
        public static HostMigrationSnapshot Pending { get; private set; }

        public static void ConsumePending() => Pending = null;

        public byte[] Generate()
        {
            try
            {
                var snapshot = CaptureCurrentMatch();
                var bytes = Serialize(snapshot);
#if UNITY_EDITOR
                Debug.Log($"[HM] Generate: {snapshot} ({bytes.Length} bytes)");
#endif
                return bytes;
            }
            catch (Exception e)
            {
                // Snapshot üretememek maçı bozmamalı — sadece o turdaki yükleme atlanır.
                Debug.LogWarning($"[HM] Generate failed: {e.Message}");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Snapshot'ı Lobby'ye yüklenecek kompakt ikili biçime çevirir. Canlı sahneden bağımsız,
        /// saf bir dönüşüm — <see cref="Deserialize"/> ile birlikte testten sürülebilsin diye
        /// Generate/Apply'dan ayrı tutulur.
        /// </summary>
        public static byte[] Serialize(HostMigrationSnapshot snapshot)
        {
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream, Encoding.UTF8);

                writer.Write(k_Version);
                writer.Write((int)snapshot.Mode);
                writer.Write(snapshot.FfaPlayerCount);
                writer.Write(snapshot.IsRanked);
                writer.Write(snapshot.ActiveTurnIndex);
                writer.Write(snapshot.RemainingTurnTime);
                writer.Write(snapshot.TurnNumber);

                writer.Write(snapshot.Players.Count);
                foreach (var p in snapshot.Players)
                {
                    writer.Write(p.UgsPlayerId ?? string.Empty);
                    writer.Write(p.DisplayName ?? string.Empty);
                    writer.Write(p.TeamId);
                    writer.Write(p.Health);
                    writer.Write(p.IsShielded);
                    writer.Write(p.Position.x);    writer.Write(p.Position.y);
                    writer.Write(p.UpDirection.x); writer.Write(p.UpDirection.y);
                    writer.Write(p.Velocity.x);    writer.Write(p.Velocity.y);
                    writer.Write(p.Ammo.superJumps);
                    writer.Write(p.Ammo.rpgAmmo);
                    writer.Write(p.Ammo.pistolAmmo);
                    writer.Write(p.Ammo.shotgunAmmo);
                    writer.Write(p.Ammo.grenades);
                    writer.Write(p.Ammo.shields);
                    writer.Write(p.HasUsedSkillThisTurn);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        public void Apply(byte[] migrationData)
        {
            if (migrationData == null || migrationData.Length == 0)
            {
                Debug.LogWarning("[HM] Apply: empty migration data, nothing to restore.");
                return;
            }

            try
            {
                var snapshot = Deserialize(migrationData);
                if (snapshot == null) return;   // sürüm uyuşmazlığı, Deserialize uyardı

                Pending = snapshot;

                // NetworkManager bu noktada KAPALI; yalnızca sahne dışı, düz statik durum yazılabilir.
                LobbyData.SelectedMode   = snapshot.Mode;
                LobbyData.FfaPlayerCount = snapshot.FfaPlayerCount;

                Debug.Log($"[HM] Apply: restored snapshot {snapshot}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HM] Apply failed: {e.Message}");
            }
        }

        /// <summary>
        /// <see cref="Serialize"/>'ın tersi. Sürüm uyuşmazlığında null döner — eski bir snapshot'ı
        /// yeni alan düzeniyle okumak sessizce yanlış bir maç kurardı.
        /// </summary>
        public static HostMigrationSnapshot Deserialize(byte[] migrationData)
        {
            {
                using var stream = new MemoryStream(migrationData);
                using var reader = new BinaryReader(stream, Encoding.UTF8);

                byte version = reader.ReadByte();
                if (version != k_Version)
                {
                    Debug.LogWarning($"[HM] Deserialize: snapshot version {version} != expected {k_Version}, discarding.");
                    return null;
                }

                var snapshot = new HostMigrationSnapshot
                {
                    Mode              = (GameModeType)reader.ReadInt32(),
                    FfaPlayerCount    = reader.ReadInt32(),
                    IsRanked          = reader.ReadBoolean(),
                    ActiveTurnIndex   = reader.ReadInt32(),
                    RemainingTurnTime = reader.ReadSingle(),
                    TurnNumber        = reader.ReadInt32(),
                };

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var p = new HostMigrationPlayerState
                    {
                        UgsPlayerId = reader.ReadString(),
                        DisplayName = reader.ReadString(),
                        TeamId      = reader.ReadInt32(),
                        Health      = reader.ReadSingle(),
                        IsShielded  = reader.ReadBoolean(),
                    };
                    p.Position    = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    p.UpDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    p.Velocity    = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    p.Ammo = new AmmoState
                    {
                        superJumps  = reader.ReadInt32(),
                        rpgAmmo     = reader.ReadInt32(),
                        pistolAmmo  = reader.ReadInt32(),
                        shotgunAmmo = reader.ReadInt32(),
                        grenades    = reader.ReadInt32(),
                        shields     = reader.ReadInt32(),
                    };
                    p.HasUsedSkillThisTurn = reader.ReadBoolean();

                    snapshot.Players.Add(p);
                    snapshot.TurnOrder.Add(p.UgsPlayerId);
                }

                return snapshot;
            }
        }

        /// <summary>Host tarafında o anki maçın durumunu toplar. Maç henüz başlamadıysa
        /// (menüde/lobide) oyuncu listesi boş kalır — yükleme zaten yalnızca 2+ oyuncuda yapılır.</summary>
        static HostMigrationSnapshot CaptureCurrentMatch()
        {
            var snapshot = new HostMigrationSnapshot
            {
                Mode           = LobbyData.SelectedMode,
                FfaPlayerCount = LobbyData.FfaPlayerCount,
                IsRanked       = NetworkBootstrap.Instance != null && NetworkBootstrap.Instance.IsRankedMatch,
            };

            var turnManager = TurnManager.Instance;
            if (turnManager == null || turnManager.characters == null) return snapshot;

            foreach (var character in turnManager.characters)
            {
                if (character == null) continue;

                // Generate() yalnızca host'ta çalışır, dolayısıyla registry burada doludur.
                string playerId = NetworkIdentityRegistry.Get(character.OwnerClientId);

                var state = new HostMigrationPlayerState
                {
                    UgsPlayerId = playerId ?? string.Empty,
                    DisplayName = character.playerName.Value.ToString(),
                    TeamId      = character.teamId.Value,
                    Position    = character.transform.position,
                    UpDirection = character.transform.up,
                };

                var rb = character.GetComponent<Rigidbody2D>();
                if (rb != null) state.Velocity = rb.linearVelocity;

                var health = character.GetComponent<CharacterHealth>();
                if (health != null)
                {
                    state.Health     = health.GetCurrentHealth();
                    state.IsShielded = health.isShielded;
                }

                var abilities = character.GetComponent<CharacterAbilities>();
                if (abilities != null)
                {
                    state.Ammo                 = abilities.CurrentAmmo;
                    state.HasUsedSkillThisTurn = abilities.HasUsedSkillThisTurn;
                }

                snapshot.Players.Add(state);
                snapshot.TurnOrder.Add(state.UgsPlayerId);
            }

            snapshot.ActiveTurnIndex   = Mathf.Clamp(turnManager.CurrentTurnIndex, 0,
                                                    Mathf.Max(0, snapshot.Players.Count - 1));
            snapshot.RemainingTurnTime = turnManager.RemainingTurnTime;
            snapshot.TurnNumber        = turnManager.TurnNumber;
            return snapshot;
        }
    }
}
