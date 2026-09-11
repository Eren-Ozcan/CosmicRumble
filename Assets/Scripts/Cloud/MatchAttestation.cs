using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

namespace CosmicRumble.Cloud
{
    /// <summary>
    /// Kupa gönderiminin çift taraflı doğrulanan yolu.
    ///
    /// <para><b>Sorun:</b> kupa, kazandığını iddia eden istemcinin doğrudan
    /// <c>AddPlayerScoreAsync</c> çağrısıyla yazılıyordu — değiştirilmiş bir istemci istediği
    /// skoru gönderebilir, üstelik host zaten sunucu rolünde olduğu için ağ tarafında da bunu
    /// durduracak bir merci yok.</para>
    ///
    /// <para><b>Çözüm:</b> her iki oyuncu da AYNI maç için KENDİ sonucunu ayrı ayrı bildirir.
    /// Cloud Code modülü iki bildirimi karşılaştırır ve kupayı yalnızca ikisi birbirini
    /// tutuyorsa (biri kazandım, diğeri kaybettim diyorsa ve ikisi de karşı tarafı doğru
    /// gösteriyorsa) hareket ettirir. Tek başına yalan söyleyen bir istemci artık yetmez;
    /// rakibiyle anlaşması gerekir ki rakibin bunu yapmak için hiçbir nedeni yok.</para>
    ///
    /// <para><b>Geriye dönük uyum:</b> modül henüz yayınlanmadığı sürece (bkz.
    /// <c>docs/cloud-code-setup.md</c>) çağrı başarısız olur ve <c>false</c> döner —
    /// çağıran taraf eski doğrudan gönderime düşer, yani hiçbir şey bozulmaz.</para>
    /// </summary>
    public static class MatchAttestation
    {
        /// <summary>Cloud Code modülünün adı — Dashboard'daki script adıyla birebir aynı olmalı.</summary>
        public const string ModuleName = "submit-match-result";

        /// <summary>Bildirimi gönderir.</summary>
        /// <returns>Modül bildirimi kabul ettiyse (kupayı yazdıysa ya da karşı tarafı beklemeye
        /// aldıysa) true; modül yoksa, kimlikler eksikse ya da çağrı patlarsa false — bu durumda
        /// çağıran eski yola düşmeli.</returns>
        public static async Task<bool> SubmitAsync(string matchId, string opponentPlayerId, bool won)
        {
            // Kimliklerden biri yoksa (UGS oturumu kapalı, hotseat, rakip kimliğini hiç
            // bildirmemiş) doğrulanacak bir şey yok — sessizce eski yola bırak.
            if (string.IsNullOrEmpty(matchId) || string.IsNullOrEmpty(opponentPlayerId))
                return false;

            try
            {
                var args = new Dictionary<string, object>
                {
                    { "matchId",    matchId },
                    { "opponentId", opponentPlayerId },
                    { "won",        won },
                };

                var result = await CloudCodeService.Instance
                    .CallEndpointAsync<AttestationResult>(ModuleName, args);

                // "pending" = ilk bildirim kaydedildi, rakip bekleniyor; skoru modül yazacak.
                // "settled" = iki taraf uyuştu, kupa uygulandı.
                // "rejected" = bildirimler çelişti; kupa BİLEREK verilmedi, eski yola DÜŞÜLMEZ,
                //              yoksa doğrulamayı atlatmanın yolu çelişkili bildirim göndermek olurdu.
                return result != null &&
                       (result.status == "pending" || result.status == "settled" ||
                        result.status == "rejected");
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[MatchAttestation] Cloud Code call failed ({e.Message}) — " +
                                 "falling back to direct score submission.");
#endif
                return false;
            }
        }

        [Serializable]
        private class AttestationResult
        {
            public string status;
            public bool   applied;
            public string reason;
        }
    }
}
