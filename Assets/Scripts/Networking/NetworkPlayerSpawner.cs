using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class NetworkPlayerSpawner : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;

        void Start()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) return; // offline hotseat — GameInitializer halleder
            if (!nm.IsServer) return;                  // sadece server spawn eder

            SpawnAllConnectedClients(nm);
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

            var allPlayers = new List<GravityBody>();
            int i = 0;
            foreach (var clientId in clientIds)
            {
                var s = slots[i++];
                var go = Instantiate(playerPrefab, s.position, Quaternion.identity);
                go.name = $"Player_{clientId}";
                go.transform.up = s.upDir;

                var netObj = go.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError("[NetworkPlayerSpawner] playerPrefab'ta NetworkObject yok!");
                    Destroy(go);
                    continue;
                }
                netObj.SpawnAsPlayerObject(clientId);

                var gb = go.GetComponent<GravityBody>();
                if (gb != null) allPlayers.Add(gb);

                Debug.Log($"[NET] Spawned player for clientId={clientId} at {s.position}");
            }

            TurnManager.Instance?.RegisterPlayers(allPlayers);
            TurnManager.Instance?.BeginMatch();
        }
    }
}
