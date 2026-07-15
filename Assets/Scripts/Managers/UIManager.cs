using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Slot’lar (0=Pistol,1=Shotgun,2=RPG,3=Grenade,4=SuperJump,5=Shield,6=Teleport,7=Bat/Hammer,8=Black Hole)")]
    public Image[] filterImages;         // 10 elemanlı
    public TextMeshProUGUI[] countTexts; // 10 elemanlı

    [Header("End Game — Winner Announcement")]
    [Tooltip("Panel shown when the game ends (starts inactive)")]
    public GameObject gameOverPanel;
    [Tooltip("Displays 'Player X Wins!' or 'Draw!'")]
    public TextMeshProUGUI winnerText;
    [Tooltip("Displays earned XP — assign a TMP Text in the gameOverPanel")]
    public TextMeshProUGUI xpEarnedText;
    [Tooltip("Displays earned Gold — assign a TMP Text in the gameOverPanel")]
    public TextMeshProUGUI goldEarnedText;
    [Tooltip("Button that dismisses the winner panel and opens the end-game menu")]
    public Button okButton;

    [Header("End Game — End Game Menu")]
    [Tooltip("Panel with Return to Main Menu / Free Camera buttons (starts inactive)")]
    public GameObject endGameMenuPanel;
    public Button returnToMenuButton;
    public Button freeCameraButton;

    [Header("Confirm Prompt")]
    [Tooltip("Opsiyonel: kalkan/onay beklenirken gösterilecek metin (atanmazsa sessiz kalır)")]
    public TextMeshProUGUI confirmPromptText;

    [Header("Free Camera")]
    [Tooltip("The FreeCameraController component to enable/disable")]
    public FreeCameraController freeCamera;
    [Tooltip("All gameplay UI roots to hide during free camera mode")]
    public GameObject[] gameplayUIRoots;

    [Header("Renk Ayarları")]
    public Color selectionColor = new Color(1, 1, 0, 0.5f);   // sarı
    public Color confirmColor = new Color(0, 1, 0, 0.5f);   // yeşil
    public Color emptyColor = new Color(1, 0, 0, 0.5f);   // kırmızı
    public Color noneColor = new Color(0, 0, 0, 0f);     // şeffaf
    public Color lockedColor = new Color(0.12f, 0.12f, 0.16f, 0.85f); // level kilidi (koyu)

    private CharacterAbilities currentAb;
    private int selectedIndex = -1;

    // --- Event handler referansları (aynı delegate ile unsubscribe edebilmek için) ---
    private Action<int> _onSkillChangedHandler;
    private Action _onSuperJumpChangedHandler;
    private Action _onRpgAmmoChangedHandler;
    private Action _onPistolAmmoChangedHandler;
    private Action _onShotgunAmmoChangedHandler;
    private Action _onGrenadeChangedHandler;
    private Action _onShieldChangedHandler;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Skill event handlers
        _onSkillChangedHandler    = OnSkillChanged;
        _onSuperJumpChangedHandler = () => UpdateSlot(4);
        _onRpgAmmoChangedHandler   = () => UpdateSlot(2);
        _onPistolAmmoChangedHandler = () => UpdateSlot(0);
        _onShotgunAmmoChangedHandler = () => UpdateSlot(1);
        _onGrenadeChangedHandler   = () => UpdateSlot(3);
        _onShieldChangedHandler    = () => UpdateSlot(5);

        // End-game buttons are wired via Inspector OnClick — no AddListener needed here.
    }

    public void SetCharacter(CharacterAbilities ab)
    {
        // önceki karakterden ayrıl
        if (currentAb != null)
        {
            currentAb.SkillChanged -= _onSkillChangedHandler;
            currentAb.SuperJumpChanged -= _onSuperJumpChangedHandler;
            currentAb.RpgAmmoChanged -= _onRpgAmmoChangedHandler;
            currentAb.PistolAmmoChanged -= _onPistolAmmoChangedHandler;
            currentAb.ShotgunAmmoChanged -= _onShotgunAmmoChangedHandler;
            currentAb.GrenadeChanged -= _onGrenadeChangedHandler;
            currentAb.ShieldChanged -= _onShieldChangedHandler;
        }

        currentAb = ab;

        if (currentAb != null)
        {
            currentAb.SkillChanged += _onSkillChangedHandler;
            currentAb.SuperJumpChanged += _onSuperJumpChangedHandler;
            currentAb.RpgAmmoChanged += _onRpgAmmoChangedHandler;
            currentAb.PistolAmmoChanged += _onPistolAmmoChangedHandler;
            currentAb.ShotgunAmmoChanged += _onShotgunAmmoChangedHandler;
            currentAb.GrenadeChanged += _onGrenadeChangedHandler;
            currentAb.ShieldChanged += _onShieldChangedHandler;
        }

        // ilk durum
        for (int i = 0; i < filterImages.Length; i++)
            UpdateSlot(i);

        ClearAllSkillSelections();
    }

    private void OnSkillChanged(int slotIndex) => UpdateSlot(slotIndex);

    // ── Level kilidi yardımcıları ────────────────────────────────────────
    private static bool IsSlotLocked(int idx) =>
        !CosmicRumble.Economy.AbilitySlotCatalog.IsSlotUnlocked(idx);

    /// <summary>Seçili olmayan bir slotun "dinlenme" rengi: kilitli > stok bitti > normal.</summary>
    private Color StockColor(int idx)
    {
        if (IsSlotLocked(idx)) return lockedColor;
        int remaining = currentAb?.GetSkillRemaining(idx) ?? 0;
        return remaining == 0 ? emptyColor : noneColor;
    }

    public void ClearSkillColor(int slotIndex, bool isEmpty)
    {
        if (!IsValidSlot(slotIndex)) return;
        filterImages[slotIndex].color = isEmpty ? emptyColor : noneColor;
    }

    private void UpdateSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || currentAb == null) return;

        filterImages[slotIndex].gameObject.SetActive(true);
        countTexts[slotIndex].gameObject.SetActive(true);

        // Level kilidi: sayaç yerine gereken seviyeyi göster, seçim highlight'ı hiç uygulanmaz
        // (kilitli slot CharacterAbilities.SelectSkill kapısından zaten geçemez).
        if (IsSlotLocked(slotIndex))
        {
            int req = CosmicRumble.Economy.AbilitySlotCatalog.GetRequiredLevel(slotIndex);
            countTexts[slotIndex].text = req > 0 ? $"Lv{req}" : "—";
            filterImages[slotIndex].color = lockedColor;
            return;
        }

        int left = currentAb.GetSkillRemaining(slotIndex);
        countTexts[slotIndex].text = left < 0 ? "∞" : left.ToString(); // -1 = sınırsız (Pistol)

        // Seçili slotu asla bozma; değilse stoğa göre boya
        if (selectedIndex == slotIndex)
        {
            // seçili ise sarı/yeşil korunur (ConfirmSkill sarıya basmayacağı için burada sarıyı koruyoruz)
            if (filterImages[slotIndex].color != confirmColor)
                filterImages[slotIndex].color = selectionColor;
        }
        else
        {
            filterImages[slotIndex].color = (left == 0 ? emptyColor : noneColor);
        }
    }

    /// <summary>Turda skill kullanıldığında tüm slotları gri yapar.</summary>
    public void LockAllSkillsUI()
    {
        for (int i = 0; i < filterImages.Length; i++)
            if (filterImages[i] != null)
                filterImages[i].color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    }

    /// <summary>Yeni tur başında filtreleri stoğa göre sıfırlar.</summary>
    public void ClearAllSkillFilters()
    {
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (!IsValidSlot(i)) continue;
            filterImages[i].color = StockColor(i);
        }
    }

    /// <summary>Yalnızca verilen slotu sarı yapar; diğerlerini stoğa göre çeker.</summary>
    public void HighlightSelected(int slot)
    {
        // önce hepsini stok rengine çek
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (!IsValidSlot(i) || i == slot) continue;
            filterImages[i].color = StockColor(i);
        }

        // sonra hedefi sarı yap
        if (IsValidSlot(slot))
        {
            filterImages[slot].color = selectionColor;
            selectedIndex = slot;
        }
        else
        {
            selectedIndex = -1;
        }
    }

    // geriye uyumluluk
    public void HighlightSkill(int slot) => HighlightSelected(slot);

    /// <summary>Seçili slotu yeşil onay rengine alır; diğerlerini stoğa göre çeker.</summary>
    public void ConfirmSkill(int slot)
    {
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (!IsValidSlot(i) || i == slot) continue;
            filterImages[i].color = StockColor(i);
        }

        if (IsValidSlot(slot))
        {
            filterImages[slot].color = confirmColor;
            selectedIndex = slot;
        }
        else
        {
            selectedIndex = -1;
        }
    }

    /// <summary>
    /// Dokunmatik/mouse ortak giriş noktası: skill ikonuna Button.onClick üzerinden bağlanır.
    /// İlk tık seçer (klavyede sayı tuşuna basmakla aynı), o slot zaten seçiliyken ikinci tık
    /// onaylar (klavyede Enter ile aynı) — ayrı bir "onay" butonuna gerek kalmadan tek ikonla
    /// select+confirm akışı sağlar. Farklı bir slota tıklamak klavyedeki gibi doğrudan ona geçer.
    /// </summary>
    public void OnSkillIconTapped(int idx)
    {
        if (currentAb == null) return;
        if (IsSlotLocked(idx)) return; // kilitli slot dokunmatikten de seçilemez

        if (selectedIndex == idx)
            currentAb.ConfirmSkill(idx);
        else
            currentAb.SelectSkill(idx);
    }

    /// <summary>Aktif seçimi iptal eder — tray kapatıldığında (silahı elden bırakma) çağrılır.</summary>
    public void CancelSelection()
    {
        currentAb?.DeselectAll();
    }

    /// <summary>Tüm highlight’ları kaldırır; stoğa göre renge döner.</summary>
    public void ClearAllSkillSelections()
    {
        selectedIndex = -1;
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (!IsValidSlot(i)) continue;
            filterImages[i].color = StockColor(i);
        }
    }

    /// <summary>
    /// Shows the winner announcement panel. Pass null winnerName for a draw.
    /// </summary>
    public void ShowGameOver(string winnerName, long xpEarned = 0, long goldEarned = 0)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (winnerText != null)
            winnerText.text = winnerName != null
                ? string.Format(CosmicRumble.Localization.Loc.T("{0} Wins!"), winnerName)
                : CosmicRumble.Localization.Loc.T("Draw!");

        if (xpEarnedText != null)
            xpEarnedText.text = $"+{xpEarned} XP";

        if (goldEarnedText != null)
            goldEarnedText.text = string.Format(CosmicRumble.Localization.Loc.T("+{0} Gold"), goldEarned);
    }

    // ── End-game button callbacks — public so they appear in the Inspector OnClick dropdown ──

    /// <summary>OK button: dismiss winner announcement, open end-game menu.</summary>
    public void OnOKButtonClicked()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (endGameMenuPanel != null) endGameMenuPanel.SetActive(true);
    }

    /// <summary>Return to Main Menu button.</summary>
    public void OnReturnToMenuClicked()
    {
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(SceneNames.Menu);
        else
            SceneManager.LoadScene(SceneNames.Menu);
    }

    /// <summary>Free Camera button: hide all gameplay UI and hand control to FreeCameraController.</summary>
    public void OnFreeCameraClicked()
    {
        if (endGameMenuPanel != null) endGameMenuPanel.SetActive(false);
        foreach (var go in gameplayUIRoots)
            if (go != null) go.SetActive(false);
        if (freeCamera != null) freeCamera.enabled = true;
    }

    /// <summary>
    /// Called by FreeCameraController when the player presses Escape.
    /// Re-shows the end-game menu and all gameplay UI roots.
    /// </summary>
    public void ExitFreeCamera()
    {
        if (freeCamera != null) freeCamera.enabled = false;
        foreach (var go in gameplayUIRoots)
            if (go != null) go.SetActive(true);
        if (endGameMenuPanel != null) endGameMenuPanel.SetActive(true);
    }

    /// <summary>Onay beklerken ekranda kısa bir mesaj gösterir (opsiyonel TMP alanı atanmışsa).</summary>
    public void ShowConfirmPrompt(string message)
    {
        if (confirmPromptText == null) return;
        confirmPromptText.text = message;
        confirmPromptText.gameObject.SetActive(true);
    }

    /// <summary>Onay prompt'unu gizler.</summary>
    public void HideConfirmPrompt()
    {
        if (confirmPromptText == null) return;
        confirmPromptText.gameObject.SetActive(false);
    }

    private bool IsValidSlot(int idx)
    {
        return filterImages != null && countTexts != null &&
               idx >= 0 && idx < filterImages.Length &&
               idx < countTexts.Length;
    }
}
