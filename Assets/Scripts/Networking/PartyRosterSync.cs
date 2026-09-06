using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CosmicRumble.Networking
{
    /// <summary>
    /// Parti lobisindeki roster'ı (kim katıldı, hangi isimle, hangi slot rengiyle) tüm makinelere
    /// senkronlar. PartyLobbyPanelUI'nin eski davranışı — host "Joined" placeholder'ı gösteriyordu,
    /// misafir yalnızca kendi adını görüyordu (bkz. PartyLobbyPanelUI sınıf yorumu) — çünkü maç
    /// başlamadan önce (Game sahnesi yüklenip GravityBody'ler spawn olmadan) senkron bir nesne hiç
    /// yoktu. Bu obje MenuScene'e sahne-içi (in-scene) NetworkObject olarak yerleştirilir, böylece
    /// prefab kaydına gerek kalmadan host başlar başlamaz otomatik spawn olur ve bağlanan her
    /// client'a senkronlanır.
    ///
    /// Akış: Host bir parti/davet kurunca kendi girdisini ekler (slot 0). Her client, bağlandığında
    /// adını ReportNameServerRpc ile bildirir; server bir sonraki boş slotu atar. Lobi ekranından
    /// çıkan/kopan biri varsa (maç başlamadan) server RemoveEntry ile temizler — maç başladıktan
    /// sonraki oyuncu senkronu zaten NetworkPlayerSpawner/GravityBody'nin işi, bu sınıf yalnızca
    /// LOBİ aşamasını kapsar.
    /// </summary>
    public class PartyRosterSync : NetworkBehaviour
    {
        public static PartyRosterSync Instance { get; private set; }

        public struct RosterEntry : INetworkSerializable, IEquatable<RosterEntry>
        {
            public ulong ClientId;
            public FixedString32Bytes Name;
            public int SlotIndex;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref Name);
                serializer.SerializeValue(ref SlotIndex);
            }

            public bool Equals(RosterEntry other) => ClientId == other.ClientId;
        }

        public readonly NetworkList<RosterEntry> Entries = new NetworkList<RosterEntry>();

        /// <summary>Roster listesi her değiştiğinde (ekleme/çıkarma) tetiklenir — UI bunu dinleyip
        /// slotları yeniden çizer.</summary>
        public event Action OnRosterChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            Entries.OnListChanged += _ => OnRosterChanged?.Invoke();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Host, lobiyi kurduğunda kendi girdisini ekler. Server-only.</summary>
        public void ServerAddSelf(string name)
        {
            if (!IsServer) return;
            ServerUpsert(NetworkManager.Singleton.LocalClientId, name);
        }

        /// <summary>Client bağlandığında kendi adını sunucuya bildirir — sunucu bir sonraki boş
        /// slotu atar. RequireOwnership=false: bu paylaşılan sahne nesnesinin sahibi server'dır,
        /// herhangi bir client çağırabilmeli.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportNameServerRpc(string name, ServerRpcParams rpcParams = default)
        {
            ServerUpsert(rpcParams.Receive.SenderClientId, name);
        }

        void ServerUpsert(ulong clientId, string name)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].ClientId == clientId)
                {
                    var e = Entries[i];
                    e.Name = name;
                    Entries[i] = e;
                    return;
                }
            }
            Entries.Add(new RosterEntry { ClientId = clientId, Name = name, SlotIndex = Entries.Count });
        }

        /// <summary>Lobi aşamasında biri ayrılır/kopa — server-only. Maç başladıktan sonrası bu
        /// sınıfın kapsamı dışında (bkz. NetworkPlayerSpawner).</summary>
        public void ServerRemove(ulong clientId)
        {
            if (!IsServer) return;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].ClientId == clientId)
                {
                    Entries.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>Yeni bir parti/davet kurulurken (host tarafında) önceki oturumdan kalma
        /// girdileri temizler.</summary>
        public void ServerClear()
        {
            if (!IsServer) return;
            Entries.Clear();
        }
    }
}
