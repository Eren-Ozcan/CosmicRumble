using UnityEngine;
using UnityEngine.SceneManagement;

namespace CosmicRumble.Localization
{
    /// <summary>
    /// Sıra, <see cref="Loc"/>'un çeviri tablosundaki dizi indeksleriyle eşleşir (English hariç —
    /// İngilizce her zaman anahtarın kendisi, hiç dizi indeksi tutmaz). Yeni bir dil eklerken
    /// hem burada hem LocStrings.cs'teki her satırın dizisine aynı sırada bir eleman eklenmeli.
    /// </summary>
    public enum Language { English, Turkish, ChineseSimplified, Spanish, Japanese, Korean, German }

    /// <summary>
    /// Global dil durumu. Varsayılan İngilizce. PlayerPrefs'te kalıcı, DontDestroyOnLoad — GameScene'e
    /// de hayatta kalır ki in-game HUD da aynı dili kullansın.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        const string PrefKey = "language";

        public Language CurrentLanguage { get; private set; } = Language.English;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentLanguage = (Language)PlayerPrefs.GetInt(PrefKey, (int)Language.English);
        }

        /// <summary>
        /// Dili değiştirir ve aktif sahneyi yeniden yükler — programatik olarak kurulmuş tüm UI
        /// metinleri (build-time'da Loc.T ile seçilmiş) yeniden inşa edilirken doğru dilde kurulur.
        /// Hesap değişimindeki sahne reload'uyla aynı desen (bkz. LoginScreenUI).
        /// </summary>
        public void SetLanguage(Language lang)
        {
            if (lang == CurrentLanguage) return;
            CurrentLanguage = lang;
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static string DisplayName(Language lang) => lang switch
        {
            Language.Turkish           => "Türkçe",
            Language.ChineseSimplified => "简体中文",
            Language.Spanish           => "Español",
            Language.Japanese          => "日本語",
            Language.Korean            => "한국어",
            Language.German            => "Deutsch",
            _                          => "English",
        };
    }

    /// <summary>
    /// Anahtar tabanlı yerelleştirme yardımcısı. Çağrı yeri her zaman İngilizce metni anahtar olarak
    /// kullanır — <see cref="LocStrings"/>'teki tablo eksikse veya dil İngilizce ise metnin kendisi
    /// döner (zarif düşüş: eksik çeviri, boş/kırık metin yerine İngilizceyi gösterir).
    /// </summary>
    public static class Loc
    {
        public static string T(string english)
        {
            var mgr = LocalizationManager.Instance;
            var lang = mgr != null ? mgr.CurrentLanguage : Language.English;
            if (lang == Language.English) return english;

            if (LocStrings.Table.TryGetValue(english, out var translations))
            {
                int idx = (int)lang - 1; // Turkish=0, ChineseSimplified=1, Spanish=2, Japanese=3, Korean=4, German=5
                if (idx >= 0 && idx < translations.Length && !string.IsNullOrEmpty(translations[idx]))
                    return translations[idx];
            }
            return english;
        }

        public static bool IsEnglish => LocalizationManager.Instance == null
            || LocalizationManager.Instance.CurrentLanguage == Language.English;
    }
}
