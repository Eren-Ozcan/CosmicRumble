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

        // NGO bileşeni yoksa (Bullet.prefab, Bomb.prefab gibi networked olmayan mermiler)
        // prefab'ın kendi bodyType ayarına karışma.
        if (rb.GetComponent<NetworkRigidbody2D>() == null) return;

        var netObj = rb.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned) return; // networked: otorite NGO'da

        rb.bodyType = RigidbodyType2D.Dynamic;
    }
}
