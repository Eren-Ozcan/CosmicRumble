using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Slot’lar (0=Pistol,1=Shotgun,2=RPG,3=Grenade,4=SuperJump,5=Shield)")]
    public Image[] filterImages;         // 10 elemanlı
    public TextMeshProUGUI[] countTexts; // 10 elemanlı

    [Header("Renk Ayarları")]
    public Color selectionColor = new Color(1, 1, 0, 0.5f);
    public Color confirmColor = new Color(0, 1, 0, 0.5f);
    public Color emptyColor = new Color(1, 0, 0, 0.5f);

    private CharacterAbilities currentAb;

    // Cached delegates to avoid lambda leaks
    private Action<int> skillChangedHandler;
    private Action superJumpChangedHandler;
    private Action rpgAmmoChangedHandler;
    private Action pistolAmmoChangedHandler;
    private Action shotgunAmmoChangedHandler;
    private Action grenadeChangedHandler;
    private Action shieldChangedHandler;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Cache delegate instances once
        skillChangedHandler = UpdateSlot;
        superJumpChangedHandler = OnSuperJumpChanged;
        rpgAmmoChangedHandler = OnRpgAmmoChanged;
        pistolAmmoChangedHandler = OnPistolAmmoChanged;
        shotgunAmmoChangedHandler = OnShotgunAmmoChanged;
        grenadeChangedHandler = OnGrenadeChanged;
        shieldChangedHandler = OnShieldChanged;
    }

    public void SetCharacter(CharacterAbilities ab)
    {
        if (currentAb != null)
        {
            // Önceki karakterden event temizle
            currentAb.SkillChanged -= skillChangedHandler;        // FIX: use cached delegates
            currentAb.SuperJumpChanged -= superJumpChangedHandler;
            currentAb.RpgAmmoChanged -= rpgAmmoChangedHandler;
            currentAb.PistolAmmoChanged -= pistolAmmoChangedHandler;
            currentAb.ShotgunAmmoChanged -= shotgunAmmoChangedHandler;
            currentAb.GrenadeChanged -= grenadeChangedHandler;
            currentAb.ShieldChanged -= shieldChangedHandler;
        }

        currentAb = ab;

        // Yeni karakter event bağla
        currentAb.SkillChanged += skillChangedHandler;        // FIX: use cached delegates
        currentAb.SuperJumpChanged += superJumpChangedHandler;
        currentAb.RpgAmmoChanged += rpgAmmoChangedHandler;
        currentAb.PistolAmmoChanged += pistolAmmoChangedHandler;
        currentAb.ShotgunAmmoChanged += shotgunAmmoChangedHandler;
        currentAb.GrenadeChanged += grenadeChangedHandler;
        currentAb.ShieldChanged += shieldChangedHandler;

        // Abiliti durumunu hemen isteyip UI'ı eşitle
        for (int i = 0; i < filterImages.Length; i++)
            UpdateSlot(i);
    }

    private void OnDestroy()
    {
        // Ensure events are released if manager is destroyed
        if (currentAb != null)
        {
            currentAb.SkillChanged -= skillChangedHandler;
            currentAb.SuperJumpChanged -= superJumpChangedHandler;
            currentAb.RpgAmmoChanged -= rpgAmmoChangedHandler;
            currentAb.PistolAmmoChanged -= pistolAmmoChangedHandler;
            currentAb.ShotgunAmmoChanged -= shotgunAmmoChangedHandler;
            currentAb.GrenadeChanged -= grenadeChangedHandler;
            currentAb.ShieldChanged -= shieldChangedHandler;
        }
    }

    public void ClearSkillColor(int slotIndex, bool isEmpty)
    {
        if (slotIndex < 0 || slotIndex >= filterImages.Length) return;
        filterImages[slotIndex].color = isEmpty ? emptyColor : Color.clear;
    }

    private void UpdateSlot(int slotIndex)
    {
        // UI’ları aktif et
        filterImages[slotIndex].gameObject.SetActive(true);
        countTexts[slotIndex].gameObject.SetActive(true);

        // Sayıyı al ve yaz
        int left = currentAb.GetSkillRemaining(slotIndex);
        countTexts[slotIndex].text = left.ToString();

        // Stoğa göre renklendir
        filterImages[slotIndex].color = (left == 0 ? emptyColor : Color.clear);
    }

    private void OnSuperJumpChanged() => UpdateSlot(4);
    private void OnRpgAmmoChanged() => UpdateSlot(2);
    private void OnPistolAmmoChanged() => UpdateSlot(0);
    private void OnShotgunAmmoChanged() => UpdateSlot(1);
    private void OnGrenadeChanged() => UpdateSlot(3);
    private void OnShieldChanged() => UpdateSlot(5);

    /// <summary>
    /// Tüm skill slot’larını gri yapar (turda skill kullanıldığında)
    /// </summary>
    public void LockAllSkillsUI()
    {
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (filterImages[i] != null)
                filterImages[i].color = new Color(0.5f, 0.5f, 0.5f, 0.6f); // gri
        }
    }

    /// <summary>
    /// Yeni tur başında filtreleri sıfırlar (stoğa göre boşsa kırmızı kalır)
    /// </summary>
    public void ClearAllSkillFilters()
    {
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (filterImages[i] != null)
            {
                int remaining = currentAb?.GetSkillRemaining(i) ?? 0;
                filterImages[i].color = (remaining == 0) ? emptyColor : Color.clear;
            }
        }
    }

    public void HighlightSkill(int slot)
    {
        if (slot >= 0 && slot < filterImages.Length)
            filterImages[slot].color = selectionColor;
    }

    public void ConfirmSkill(int slot)
    {
        if (slot >= 0 && slot < filterImages.Length)
            filterImages[slot].color = confirmColor;
    }
}
