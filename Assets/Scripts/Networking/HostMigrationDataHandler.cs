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
    /// Host migration sırasında taşınan maç durumu. FAZ 1 kapsamı: yalnızca maç yapılandırması
    /// ve sıra düzeni — oyuncu canları/pozisyonları (Faz 2) ve gezegen tahribatı (Faz 3) henüz
    /// taşınmıyor. Ayrıntılı plan: docs/HOST_MIGRATION_PLAN.md
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

        public override string ToString() =>
            $"mode={Mode} ffa={FfaPlayerCount} ranked={IsRanked} players={TurnOrder.Count} active={ActiveTurnIndex}";
    }

    /// <summary>
    /// SDK'nın host migration verisini üretip uygulayan köprü. Host, maç sürerken
    /// <see cref="Generate"/>'i periyodik çağırır ve sonucu Lobby'ye yükler; host düşünce yeni
    /// seçilen host'ta <see cref="Apply"/> bir kez çağrılır.
    ///
    /// KRİTİK SIRALAMA: Apply(), NetworkModule içinde ResetAsync() ile StartRelayNetworkAsync()
    /// ARASINDA çalışır — yani NetworkManager KAPALIYKEN. Burada hiçbir şey spawn edilemez ve
    /// hiçbir NetworkVariable yazılamaz. Bu yüzden Apply yalnızca çözümleyip <see cref="Pending"/>
    /// içine bırakır; sahneyi gerçekten kuran taraf, host başladıktan ve client'lar geri
    /// bağlandıktan sonra bu snapshot'ı tüketen NetworkPlayerSpawner/TurnManager tarafıdır.
    /// </summary>
    public class HostMigrationDataHandler : IMigrationDataHandler
    {
        /// <summary>Serileştirme sürümü — ileride alan eklenirse eski snapshot'ı sessizce
        /// yanlış okumak yerine reddedebilmek için.</summary>
        const byte k_Version = 1;

        /// <summary>Yeni host'ta Apply() ile bırakılan, henüz uygulanmamış snapshot.
        /// Tüketen taraf işi bitince <see cref="ConsumePending"/> çağırmalı.</summary>
        public static HostMigrationSnapshot Pending { get; private set; }

        public static void ConsumePending() => Pending = null;

        public byte[] Generate()
        {
            try
            {
                var snapshot = CaptureCurrentMatch();
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream, Encoding.UTF8);

                writer.Write(k_Version);
                writer.Write((int)snapshot.Mode);
                writer.Write(snapshot.FfaPlayerCount);
                writer.Write(snapshot.IsRanked);
                writer.Write(snapshot.ActiveTurnIndex);
                writer.Write(snapshot.TurnOrder.Count);
                foreach (var id in snapshot.TurnOrder)
                    writer.Write(id ?? string.Empty);

                writer.Flush();
                var bytes = stream.ToArray();
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

        public void Apply(byte[] migrationData)
        {
            if (migrationData == null || migrationData.Length == 0)
            {
                Debug.LogWarning("[HM] Apply: empty migration data, nothing to restore.");
                return;
            }

            try
            {
                using var stream = new MemoryStream(migrationData);
                using var reader = new BinaryReader(stream, Encoding.UTF8);

                byte version = reader.ReadByte();
                if (version != k_Version)
                {
                    Debug.LogWarning($"[HM] Apply: snapshot version {version} != expected {k_Version}, discarding.");
                    return;
                }

                var snapshot = new HostMigrationSnapshot
                {
                    Mode            = (GameModeType)reader.ReadInt32(),
                    FfaPlayerCount  = reader.ReadInt32(),
                    IsRanked        = reader.ReadBoolean(),
                    ActiveTurnIndex = reader.ReadInt32(),
                };

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    snapshot.TurnOrder.Add(reader.ReadString());

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

        /// <summary>Host tarafında o anki maçın Faz 1 durumunu toplar. Maç henüz başlamadıysa
        /// (menüde/lobide) sıra düzeni boş kalır — yükleme zaten yalnızca 2+ oyuncuda yapılır.</summary>
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
                snapshot.TurnOrder.Add(playerId ?? string.Empty);
            }

            snapshot.ActiveTurnIndex = Mathf.Clamp(turnManager.CurrentTurnIndex, 0,
                                                  Mathf.Max(0, snapshot.TurnOrder.Count - 1));
            return snapshot;
        }
    }
}
