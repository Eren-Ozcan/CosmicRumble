// Assets/Scripts/Utilities/SaveIntegrity.cs
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CosmicRumble.Utilities
{
    /// <summary>
    /// Yerel save dosyaları/pref'leri için ortak HMAC imzası (CurrencyManager,
    /// LeaderboardManager). İki mod:
    ///
    /// - <see cref="Sign"/>: cihazdan BAĞIMSIZ — Cloud Save ile cihazlar arası taşınan veriler
    ///   için. (Cihaz kimliğini anahtara karmak, buluttan yeni cihaza inen meşru dosyayı
    ///   "kurcalanmış" sayıp SIFIRLIYORDU — bkz. CurrencyManager.Load'daki geçiş notu.)
    /// - <see cref="SignDeviceBound"/>: cihaza BAĞLI — yalnızca bu cihazda yaşayan, buluta hiç
    ///   gitmeyen veriler için (ör. kupa önbelleği); dosya/pref kopyalamayı da geçersiz kılar.
    ///
    /// DÜRÜST SINIR: anahtar client binary'sinde gömülü — amaç save-editor/regedit tarzı kolay
    /// kurcalamayı engellemek, kriptografik bir garanti değil. Tam koruma sunucu taraflı
    /// doğrulama (Cloud Code) ister — bkz. TODO.md madde 22.
    /// </summary>
    public static class SaveIntegrity
    {
        private static readonly byte[] Key =
        {
            0x4b, 0x2e, 0x91, 0x7a, 0xd3, 0x5c, 0x08, 0xf1,
            0x63, 0xa9, 0x2d, 0x74, 0xbe, 0x11, 0x9c, 0x40
        };

        /// <summary>Cihazdan bağımsız imza — Cloud Save ile gezen veriler için.</summary>
        public static string Sign(string data)
        {
            using var hmac = new HMACSHA256(Key);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        /// <summary>Cihaza bağlı imza — buluta gitmeyen, yalnızca yerel veriler için.</summary>
        public static string SignDeviceBound(string data)
        {
            return Sign(data + "|" + SystemInfo.deviceUniqueIdentifier);
        }
    }
}
