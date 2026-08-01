// Assets/Scripts/Utilities/NetworkVariableWarningSuppressor.cs
using System.Reflection;
using UnityEngine;

/// <summary>
/// Offline hotseat'te NetworkObject hiç spawn edilmiyor (IsSpawned kalıcı false, bkz.
/// NetworkPhysicsGuard'daki aynı gözlem) — bu yüzden CharacterHealth/CharacterAbilities/
/// TurnManager/GravityBody'nin her hasar/ammo/tur yazımı NGO'nun "NetworkVariable is written to,
/// but doesn't know its NetworkBehaviour yet" uyarısını tetikliyor (NetworkVariableBase, bir
/// değişkeni sahibi NetworkBehaviour'a yalnızca Spawn anında bağlıyor; offline'da Spawn hiç
/// olmadığı için bağ hiç kurulmuyor). Veri zaten ağa gitmediği için zararsız, ama oynanış boyunca
/// konsolu doldurup gerçek hataları (ör. eksik AudioSource) gözden kaçırtıyor.
///
/// NGO bu senaryo için internal bir bayrak taşıyor (IgnoreInitializeWarning) ama public API'den
/// erişilemiyor — reflection ile bir kerelik açılıyor. Online modda hiçbir etkisi yok: orada
/// yazımlar zaten yalnızca OnNetworkSpawn'dan SONRA (Spawn zaten m_NetworkBehaviour'ı bağlamış
/// haldeyken) yapılıyor, bu uyarı o yolda zaten hiç tetiklenmiyordu.
/// </summary>
public static class NetworkVariableWarningSuppressor
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Suppress()
    {
        var field = typeof(Unity.Netcode.NetworkVariableBase).GetField(
            "IgnoreInitializeWarning", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, true);
    }
}
