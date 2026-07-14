// Assets/Scripts/Utilities/NetworkIdentityRegistry.cs
using System;
using System.Collections.Generic;

namespace CosmicRumble.Utilities
{
    /// <summary>
    /// Server tarafında NGO clientId → UGS PlayerId eşlemesi. Her client, game sahnesindeki
    /// TurnManager spawn olduğunda kendi UGS PlayerId'sini bir ServerRpc ile bildirir
    /// (bkz. TurnManager.SubmitIdentityServerRpc); NetworkPlayerSpawner bu kaydı, kopan
    /// oyuncunun sahipsiz karakterini YALNIZCA aynı kimlikle geri dönen bağlantıya devretmek
    /// için kullanır — önceden katılım kodunu bilen herhangi bir üçüncü bağlantı karakteri
    /// devralabiliyordu.
    /// </summary>
    public static class NetworkIdentityRegistry
    {
        private static readonly Dictionary<ulong, string> _ids = new Dictionary<ulong, string>();

        /// <summary>Yeni bir kimlik bildirildiğinde (clientId, ugsPlayerId) ile tetiklenir.</summary>
        public static event Action<ulong, string> OnIdentityReported;

        public static void Report(ulong clientId, string ugsPlayerId)
        {
            if (string.IsNullOrEmpty(ugsPlayerId)) return;
            _ids[clientId] = ugsPlayerId;
            OnIdentityReported?.Invoke(clientId, ugsPlayerId);
        }

        public static string Get(ulong clientId) =>
            _ids.TryGetValue(clientId, out var id) ? id : null;

        /// <summary>Yeni maç başlarken çağrılır — clientId'ler oturumlar arasında yeniden kullanılır.</summary>
        public static void Clear() => _ids.Clear();
    }
}
