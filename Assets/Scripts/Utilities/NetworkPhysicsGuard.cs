// Assets/Scripts/Utilities/NetworkPhysicsGuard.cs
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// NetworkRigidbody2D.Awake() (AutoUpdateKinematicState=true) Rigidbody2D'yi koşulsuz Kinematic'e
/// zorlar ve bunu yalnızca OnNetworkSpawn() düzeltir — offline'da NetworkObject hiç spawn
/// edilmediği için düzeltme hiç çalışmaz, body kalıcı Kinematic kalır ve rb.AddForce (GravitySource
/// yerçekimi dahil) sessizce no-op olur. GravityBody.Start() aynı düzeltmeyi karakterler için
/// yapıyor; bu yardımcı, mermi scriptlerinin Init() yollarına aynı kuralı tek yerden uygular.
/// Silahların SpawnAndInit() sırası her yerde Spawn() → Init() olduğundan, Init anında
/// IsSpawned=true ise networked moddayız demektir ve NGO otoriteyi kendi yönetir — dokunulmaz.
/// </summary>
public static class NetworkPhysicsGuard
{
    public static void EnsureDynamicWhenNotSpawned(Rigidbody2D rb)
    {
        if (rb == null) return;

        // NGO bileşeni yoksa (Bullet.prefab gibi networked olmayan mermiler)
        // prefab'ın kendi bodyType ayarına karışma.
        if (rb.GetComponent<NetworkRigidbody2D>() == null) return;

        var netObj = rb.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned) return; // networked: otorite NGO'da

        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>
    /// Mermi imha yollarının ortak çıkışı. Spawned bir NetworkObject'i CLIENT'ta Destroy etmek
    /// NGO hatası üretir ve desync yaratır (server'ın gözünde mermi uçmaya devam eder) — mermiler
    /// her makinede yerel fizikle de simüle edildiği için client'ın kendi çarpışma tespiti bu
    /// yola sık düşer. Kural:
    ///   - offline / network'süz obje  → normal Destroy (eski davranış),
    ///   - server                      → Despawn(true) (tüm makinelerde yok eder),
    ///   - client                      → yerelde "emekliye ayır" (görsel/collider/ses kapatılır),
    ///                                    gerçek yok etme server'ın despawn'ıyla gelir.
    /// </summary>
    public static void DespawnOrDestroy(GameObject go, MonoBehaviour callerScript = null)
    {
        if (go == null) return;

        var netObj = go.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                netObj.Despawn(true);
                return;
            }

            foreach (var r in go.GetComponentsInChildren<Renderer>())  r.enabled = false;
            foreach (var c in go.GetComponentsInChildren<Collider2D>()) c.enabled = false;
            foreach (var a in go.GetComponentsInChildren<AudioSource>()) a.Stop();
            if (callerScript != null) callerScript.enabled = false; // TTL/menzil döngüsü tekrar tetiklenmesin
            return;
        }

        Object.Destroy(go);
    }
}
