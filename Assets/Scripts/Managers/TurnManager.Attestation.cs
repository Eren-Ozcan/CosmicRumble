using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// TurnManager'ın maç kimliği bölümü: maça tekil bir kimlik verir ve her makinenin kendi
/// UGS PlayerId'sinin yanında RAKİBİNKİNİ de bilmesini sağlar.
///
/// <para><b>Neden gerekiyor:</b> kupa gönderimi bugün tek taraflı — kazandığını iddia eden
/// istemci skoru doğrudan yazıyor (bkz. LeaderboardManager). Çift taraflı doğrulamada her iki
/// taraf da KENDİ sonucunu ayrı ayrı bildirir ve sunucu tarafındaki modül ancak iki bildirim
/// birbirini tutuyorsa kupa verir. Bunun için her istemcinin maç kimliğini ve karşı tarafın
/// gerçek PlayerId'sini bilmesi gerekir; sunucu ikisini de burada dağıtır.</para>
///
/// <para>Kimlikleri sunucu zaten topluyordu (<see cref="CosmicRumble.Utilities.NetworkIdentityRegistry"/>,
/// reconnect doğrulaması için) — eksik olan tek şey onları istemcilere geri yayınlamaktı.</para>
/// </summary>
public partial class TurnManager
{
    /// <summary>Sunucunun maç başında ürettiği tekil kimlik; iki taraf da aynı değeri görür,
    /// böylece iki ayrı bildirim aynı maça ait olduğu anlaşılabilir.</summary>
    readonly NetworkVariable<FixedString64Bytes> netMatchId =
        new NetworkVariable<FixedString64Bytes>(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>clientId → UGS PlayerId. Sunucuda kayıttan, istemcilerde ClientRpc'den dolar.</summary>
    readonly Dictionary<ulong, string> _roster = new Dictionary<ulong, string>();

    public string MatchId => netMatchId.Value.ToString();

    public string PlayerIdOf(ulong clientId) =>
        _roster.TryGetValue(clientId, out var id) ? id : null;

    /// <summary>Bu makinenin UGS PlayerId'si — UGS oturumu yoksa null.</summary>
    public static string LocalPlayerId
    {
        get
        {
            try { return Unity.Services.Authentication.AuthenticationService.Instance.PlayerId; }
            catch { return null; }
        }
    }

    /// <summary>Rakibin PlayerId'si. 1v1 için anlamlıdır (kupa yalnızca orada değişir);
    /// kadroda yerel olmayan ilk kimliği döner, bulunamazsa null.</summary>
    public string OpponentPlayerId()
    {
        if (NetworkManager.Singleton == null) return null;
        ulong local = NetworkManager.Singleton.LocalClientId;

        foreach (var kvp in _roster)
            if (kvp.Key != local && !string.IsNullOrEmpty(kvp.Value))
                return kvp.Value;

        return null;
    }

    // ── Sunucu tarafı ─────────────────────────────────────────────────────

    /// <summary>Maç başlarken sunucuda çağrılır: kimlik üretir ve host'un kendi PlayerId'sini
    /// kayda ekler (kayıt yalnızca uzaktaki istemcilerin bildirimiyle doluyordu, host hiç
    /// kendi kimliğini bildirmiyordu — o yüzden rakip listesi tek taraflı kalıyordu).</summary>
    void BeginAttestation()
    {
        if (!IsSpawned || !IsServer) return;

        netMatchId.Value = new FixedString64Bytes(System.Guid.NewGuid().ToString("N"));

        string hostId = LocalPlayerId;
        if (!string.IsNullOrEmpty(hostId) && NetworkManager.Singleton != null)
            CosmicRumble.Utilities.NetworkIdentityRegistry.Report(
                NetworkManager.Singleton.LocalClientId, hostId);
    }

    /// <summary>Kadroyu istemcilere yayınlar. Maç sonucunu duyurmadan HEMEN ÖNCE çağrılır:
    /// o ana kadar herkes kimliğini bildirmiş olur, dolayısıyla kadro eksiksizdir.</summary>
    void BroadcastRoster()
    {
        if (!IsSpawned || !IsServer || NetworkManager.Singleton == null) return;

        var ids   = new List<ulong>();
        var names = new List<FixedString64Bytes>();

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            string pid = CosmicRumble.Utilities.NetworkIdentityRegistry.Get(clientId);
            if (string.IsNullOrEmpty(pid)) continue;
            ids.Add(clientId);
            names.Add(new FixedString64Bytes(pid));
        }

        if (ids.Count == 0) return;
        SyncMatchRosterClientRpc(ids.ToArray(), names.ToArray());
    }

    [ClientRpc]
    void SyncMatchRosterClientRpc(ulong[] clientIds, FixedString64Bytes[] playerIds)
    {
        _roster.Clear();
        int count = Mathf.Min(clientIds.Length, playerIds.Length);
        for (int i = 0; i < count; i++)
            _roster[clientIds[i]] = playerIds[i].ToString();
    }
}
