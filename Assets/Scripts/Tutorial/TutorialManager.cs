using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Localization;

namespace CosmicRumble.Tutorial
{
    /// <summary>
    /// İlk oturum onboarding: bu cihazda oynanan ilk offline maçta (hotseat veya Antrenman —
    /// GameInitializer'ın spawn ettiği yerel karakter) hareket/atış ipuçlarını gösteren küçük,
    /// engellemeyen bir kart dizisi. Online (Quick Match/özel maç) akışına bilerek bağlanmadı —
    /// oraya ulaşan bir oyuncu zaten en az bir offline maç oynamış olur (Online butonu ana
    /// menüde, Antrenman/hotseat'ten sonra keşfedilir).
    /// Tek seferlik: <see cref="HasSeenTutorial"/> PlayerPrefs'te kalıcı, bir daha gösterilmez.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        const string SeenKey = "cr_tutorial_seen";
        const float  CardSeconds = 4.5f;

        static readonly Color CardBg  = new Color(0.09f, 0.09f, 0.18f, 0.94f);
        static readonly Color AccGold = new Color(1.00f, 0.80f, 0.20f, 1f);
        static readonly Color TextSec = new Color(0.533f, 0.533f, 0.667f, 1f);

        static readonly string[] TipKeys =
        {
            "Move with A/D",
            "Jump with SPACE",
            "Pick a weapon, aim with the mouse, then fire",
        };

        GameObject      _root;
        TextMeshProUGUI _tipText;
        Coroutine       _sequence;

        public static bool HasSeenTutorial => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
            _root.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Daha önce görülmediyse ipucu kartlarını gösterir; görülmüşse no-op.</summary>
        public void ShowIfFirstTime()
        {
            if (HasSeenTutorial) return;
            _root.SetActive(true);
            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = StartCoroutine(RunSequence());
        }

        IEnumerator RunSequence()
        {
            var wait = new WaitForSecondsRealtime(CardSeconds);
            for (int i = 0; i < TipKeys.Length; i++)
            {
                _tipText.text = Loc.T(TipKeys[i]);
                yield return wait;
            }
            Dismiss();
        }

        void Dismiss()
        {
            if (_sequence != null) { StopCoroutine(_sequence); _sequence = null; }
            _root.SetActive(false);
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        void BuildUI()
        {
            var canvasGO = new GameObject("TutorialCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // gameplay HUD üstü, ama pause/game-over menülerinin altı
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Tam ekran değil — üst-orta küçük bir kart. Hareket/ateş kontrollerini kaplamaz,
            // altındaki hiçbir şeyin raycast'ini yemez (kartın kendisi + Skip hariç).
            _root = new GameObject("TipCard");
            _root.transform.SetParent(canvasGO.transform, false);
            var bg = _root.AddComponent<Image>();
            bg.color = CardBg;
            UiKit.Round(bg);
            UiKit.Shadow(_root, 5f, 0.45f);
            UiKit.Stroke(_root, new Color(1f, 1f, 1f, 0.08f));
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.80f);
            rt.sizeDelta = new Vector2(760, 84);
            rt.anchoredPosition = Vector2.zero;

            _tipText = new GameObject("Tip").AddComponent<TextMeshProUGUI>();
            _tipText.transform.SetParent(_root.transform, false);
            _tipText.fontSize  = 22;
            _tipText.color     = AccGold;
            _tipText.alignment = TextAlignmentOptions.Center;
            _tipText.raycastTarget = false;
            var tipRt = _tipText.rectTransform;
            tipRt.anchorMin = new Vector2(0.06f, 0.5f);
            tipRt.anchorMax = new Vector2(0.82f, 0.5f);
            tipRt.sizeDelta = new Vector2(0, 60);
            tipRt.anchoredPosition = Vector2.zero;
            UiKit.BrawlText(_tipText);

            var skipGO = new GameObject("Skip");
            skipGO.transform.SetParent(_root.transform, false);
            var skipImg = skipGO.AddComponent<Image>();
            skipImg.color = new Color(1f, 1f, 1f, 0.06f);
            UiKit.Round(skipImg);
            var skipBtn = skipGO.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            skipBtn.colors = UiKit.ButtonColors(new Color(1f, 1f, 1f, 0.06f));
            skipBtn.onClick.AddListener(Dismiss);
            UiKit.Press(skipGO);
            UiKit.Hover(skipGO);
            var skipRt = skipImg.rectTransform;
            skipRt.anchorMin = skipRt.anchorMax = new Vector2(0.92f, 0.5f);
            skipRt.sizeDelta = new Vector2(56, 56);
            skipRt.anchoredPosition = Vector2.zero;

            var skipLbl = new GameObject("Lbl").AddComponent<TextMeshProUGUI>();
            skipLbl.transform.SetParent(skipGO.transform, false);
            skipLbl.text      = "✕";
            skipLbl.fontSize  = 22;
            skipLbl.color     = TextSec;
            skipLbl.alignment = TextAlignmentOptions.Center;
            skipLbl.raycastTarget = false;
            var skipLblRt = skipLbl.rectTransform;
            skipLblRt.anchorMin = Vector2.zero;
            skipLblRt.anchorMax = Vector2.one;
            skipLblRt.offsetMin = skipLblRt.offsetMax = Vector2.zero;
        }
    }
}
