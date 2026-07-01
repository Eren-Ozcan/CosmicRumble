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

        int left = currentAb.GetSkillRemaining(slotIndex);
        countTexts[slotIndex].text = left.ToString();

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
            int remaining = currentAb?.GetSkillRemaining(i) ?? 0;
            filterImages[i].color = (remaining == 0) ? emptyColor : noneColor;
        }
    }

    /// <summary>Yalnızca verilen slotu sarı yapar; diğerlerini stoğa göre çeker.</summary>
    public void HighlightSelected(int slot)
    {
        // önce hepsini stok rengine çek
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (!IsValidSlot(i) || i == slot) continue;
            int remaining = currentAb?.GetSkillRemaining(i) ?? 0;
            filterImages[i].color = (remaining == 0) ? emptyColor : noneColor;
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
            int remaining = currentAb?.GetSkillRemaining(i) ?? 0;
            filterImages[i].color = (remaining == 0) ? emptyColor : noneColor;
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

    /// <summary>Tüm highlight’ları kaldırır; stoğa göre renge döner.</summary>
    public void ClearAllSkillSelections()
    {
        selectedIndex = -1;
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (!IsValidSlot(i)) continue;
            int remaining = currentAb?.GetSkillRemaining(i) ?? 0;
            filterImages[i].color = (remaining == 0) ? emptyColor : noneColor;
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
            winnerText.text = winnerName != null ? $"{winnerName} Wins!" : "Draw!";

        if (xpEarnedText != null)
            xpEarnedText.text = $"+{xpEarned} XP";

        if (goldEarnedText != null)
            goldEarnedText.text = $"+{goldEarned} Gold";
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
