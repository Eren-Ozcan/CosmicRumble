using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Localization;
using CosmicRumble.Data;

namespace CosmicRumble.Networking
{
    /// <summary>
    /// Online eşleşmenin oyuncu-spawn'ı — GameInitializer'ın yerel/offline path'inin networked
    /// karşılığı. Sadece server'da çalışır (offline hotseat'te NetworkManager hiç dinlemiyorsa
    /// tamamen no-op). Game sahnesi yüklendiğinde (host, 2. client bağlandıktan sonra
    /// NetworkManager.SceneManager.LoadScene çağırır) her bağlı client için bir Player prefab
    /// spawn eder, TurnManager'a kaydeder ve maçı başlatır.
    ///
    /// [DefaultExecutionOrder(150)]: TurnManager.Start() [+100] her zaman önce çalışıp boş
    /// characters listesiyle sessizce çıkar (offline path'i bozmadan) — asıl işi bu script yapar.
    ///
    /// Inspector ataması: Player Prefab (aynı prefab, artık kökünde NetworkObject de var).
    /// SampleScene'e boş bir GameObject ekleyip scripti yapıştır.
    ///
    /// Maç başladıktan sonraki bağlantı kopmaları da burada ele alınır: Player.prefab'ın
    /// NetworkObject'i artık DontDestroyWithOwner=true, yani sahibi kopunca karakter YOK
    /// EDİLMİYOR — "sahipsiz" (orphaned) olarak işaretlenip reconnectTimeout süresi boyunca
    /// bekletiliyor. O süre içinde biri yeniden bağlanırsa (aynı katılım koduyla) karakterin
    /// sahipliği yeni clientId'ye devrediliyor — yeni bir karakter spawn edilmiyor. Süre dolarsa
    /// karakter despawn edilir, TurnManager'ın mevcut characters.Count&lt;2 kontrolü maçı doğal
    /// şekilde bitirir. Host'un kendisinin kopması (host migration) kapsam dışı — o durumda
    /// oturumun tamamı zaten sona erer.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class NetworkPlayerSpawner : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;
        [Tooltip("Bir oyuncu kopunca karakterinin sahipsiz bekletileceği azami süre (saniye) — bu süre içinde aynı katılım koduyla geri dönülürse karakter geri kazanılır.")]
        public float reconnectTimeout = 90f;

        readonly Dictionary<ulong, NetworkObject> _playerObjects = new Dictionary<ulong, NetworkObject>();
        readonly Dictionary<ulong, NetworkObject> _orphaned = new Dictionary<ulong, NetworkObject>();
        readonly Dictionary<ulong, float> _orphanedSince = new Dictionary<ulong, float>();
        bool _matchStarted;

        void Start()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) return; // offline hotseat — GameInitializer halleder
            if (!nm.IsServer) return;                  // sadece server spawn eder

            SpawnAllConnectedClients(nm);
            _matchStarted = true;

            nm.OnClientConnectedCallback += OnClientConnectedAfterMatchStart;
            nm.OnClientDisconnectCallback += OnClientDisconnectedMidMatch;
        }

        void OnDestroy()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnClientConnectedCallback -= OnClientConnectedAfterMatchStart;
                nm.OnClientDisconnectCallback -= OnClientDisconnectedMidMatch;
            }
        }

        void Update()
        {
            if (_orphaned.Count == 0) return;

            List<ulong> expired = null;
            foreach (var kvp in _orphanedSince)
            {
                if (Time.time - kvp.Value > reconnectTimeout)
                    (expired ??= new List<ulong>()).Add(kvp.Key);
            }
            if (expired == null) return;

            foreach (var clientId in expired)
            {
                if (_orphaned.TryGetValue(clientId, out var obj) && obj != null)
                {
                    Debug.Log($"[NET] Reconnect window expired for former clientId={clientId} ({obj.name}) — despawning.");
                    NetworkBootstrap.Instance?.ShowStatus(Loc.T("Opponent didn't return, match is ending..."));
                    obj.Despawn(true);
                }
                _orphaned.Remove(clientId);
                _orphanedSince.Remove(clientId);
            }
        }

        void SpawnAllConnectedClients(NetworkManager nm)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[NetworkPlayerSpawner] playerPrefab atanmamış!");
                return;
            }

            var sortedPlanets = SpawnPositioning.GetSortedPlanets();
            var clientIds = nm.ConnectedClientsIds;
            var slots = SpawnPositioning.CalculateSpawnPositions(clientIds.Count, sortedPlanets);

            while (slots.Count < clientIds.Count)
                slots.Add(new SpawnPositioning.SpawnSlot { position = Vector3.up * 3f, upDir = Vector3.up });

            // Takım modlarında (2v2 vb.) katılım sırasına göre round-robin dağıtılır (i % TeamCount)
            // — host'un lobide seçtiği spesifik eşleştirmeleri (kim kiminle aynı takımda) uygulamak
            // clientId↔PlayerId eşlemesi gerektirir ki bu henüz yok (bkz. PartyLobbyPanelUI notu);
            // takımsız modlarda (Duel1v1/Ffa) zaten her oyuncu kendi tekil takımıdır, davranış değişmez.
            GameModeCatalog.All.TryGetValue(LobbyData.SelectedMode, out var modeDef);
            bool isTeamMode = modeDef.IsTeamMode;

            var allPlayers = new List<GravityBody>();
            int i = 0;
            foreach (var clientId in clientIds)
            {
                var s = slots[i];
                var go = Instantiate(playerPrefab, s.position, Quaternion.identity);
                go.name = $"Player_{clientId}";
                go.transform.up = s.upDir;

                var netObj = go.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError("[NetworkPlayerSpawner] playerPrefab'ta NetworkObject yok!");
                    Destroy(go);
                    i++;
                    continue;
                }
                netObj.SpawnAsPlayerObject(clientId);
                _playerObjects[clientId] = netObj;

                var gb = go.GetComponent<GravityBody>();
                if (gb != null)
                {
                    gb.teamId.Value = isTeamMode ? (i % modeDef.TeamCount) : i;
                    gb.ApplyTeamColor();
                    allPlayers.Add(gb);
                }

                Debug.Log($"[NET] Spawned player for clientId={clientId} at {s.position}");
                i++;
            }

            TurnManager.Instance?.RegisterPlayers(allPlayers);
            TurnManager.Instance?.BeginMatch();
        }

        /// <summary>
        /// Maç başladıktan sonra gelen HER yeni bağlantı bir reconnect adayıdır (ilk 2 oyuncu
        /// zaten SpawnAllConnectedClients ile spawn edildi, bu callback yalnızca ondan SONRA
        /// abone olunuyor).
        /// </summary>
        void OnClientConnectedAfterMatchStart(ulong clientId)
        {
            if (_playerObjects.ContainsKey(clientId)) return; // beklenmedik ama güvenlik

            if (_orphaned.Count == 0)
            {
                Debug.LogWarning($"[NET] clientId={clientId} bağlandı ama geri alınacak sahipsiz karakter yok (2 oyuncu zaten dolu ya da hiç kopma yok).");
                return;
            }

            ulong orphanKey = 0;
            NetworkObject orphanObj = null;
            foreach (var kvp in _orphaned) { orphanKey = kvp.Key; orphanObj = kvp.Value; break; }

            _orphaned.Remove(orphanKey);
            _orphanedSince.Remove(orphanKey);

            if (orphanObj == null) return; // bu arada despawn olmuş olabilir

            orphanObj.ChangeOwnership(clientId);
            _playerObjects[clientId] = orphanObj;

            Debug.Log($"[NET] Reconnect: clientId={clientId} {orphanObj.name} karakterini geri kazandı (eski clientId={orphanKey}).");
            NetworkBootstrap.Instance?.HideStatus();
        }

        void OnClientDisconnectedMidMatch(ulong clientId)
        {
            if (!_playerObjects.TryGetValue(clientId, out var netObj)) return;
            _playerObjects.Remove(clientId);
            if (netObj == null) return; // zaten yok olmuş

            _orphaned[clientId] = netObj;
            _orphanedSince[clientId] = Time.time;

            Debug.Log($"[NET] clientId={clientId} koptu — {netObj.name} sahipsiz bırakıldı, {reconnectTimeout}s içinde geri dönülebilir.");
            NetworkBootstrap.Instance?.ShowStatus(Loc.T("Opponent disconnected, waiting for reconnect..."));

            // NGO'nun disconnect'i sadece transport bağlantısını koparır -- UGS Session/Lobby'nin
            // kendi üyelik kaydı ayrı bir katman ve otomatik silinmiyor. Bunu açıkça temizlemezsek
            // aynı kimlikle bir rejoin denemesi "already a member of the lobby" hatasıyla
            // sürekli başarısız olur (canlı testte doğrulandı).
            _ = NetworkBootstrap.Instance?.RemoveDisconnectedPeerAsync();
        }
    }
}
