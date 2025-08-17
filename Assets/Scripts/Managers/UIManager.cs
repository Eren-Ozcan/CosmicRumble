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

    public int? SelectedIndex { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetCharacter(CharacterAbilities ab)
    {
        if (currentAb != null)
        {
            // Önceki karakterden event temizle
            currentAb.SkillChanged -= UpdateSlot;
            currentAb.SuperJumpChanged -= () => UpdateSlot(4);
            currentAb.RpgAmmoChanged -= () => UpdateSlot(2);
            currentAb.PistolAmmoChanged -= () => UpdateSlot(0);
            currentAb.ShotgunAmmoChanged -= () => UpdateSlot(1);
            currentAb.GrenadeChanged -= () => UpdateSlot(3);
            currentAb.ShieldChanged -= () => UpdateSlot(5);
        }

        currentAb = ab;

        // İlk durumu uygula
        for (int i = 0; i < filterImages.Length; i++)
            UpdateSlot(i);

        // Yeni karakter event bağla
        currentAb.SkillChanged += UpdateSlot;
        currentAb.SuperJumpChanged += () => UpdateSlot(4);
        currentAb.RpgAmmoChanged += () => UpdateSlot(2);
        currentAb.PistolAmmoChanged += () => UpdateSlot(0);
        currentAb.ShotgunAmmoChanged += () => UpdateSlot(1);
        currentAb.GrenadeChanged += () => UpdateSlot(3);
        currentAb.ShieldChanged += () => UpdateSlot(5);
    }

    public void ClearSkillColor(int slotIndex, bool isEmpty)
    {
        PaintSlot(slotIndex, isEmpty ? emptyColor : Color.clear);
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
        PaintSlot(slotIndex, left == 0 ? emptyColor : Color.clear);
    }

    /// <summary>
    /// Tüm skill slot’larını gri yapar (turda skill kullanıldığında)
    /// </summary>
    public void LockAllSkillsUI()
    {
        ClearSelection();
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (filterImages[i] != null)
                filterImages[i].color = new Color(0.5f, 0.5f, 0.5f, 0.6f); // gri
        }
        SelectedIndex = null;
    }

    /// <summary>
    /// Yeni tur başında filtreleri sıfırlar (stoğa göre boşsa kırmızı kalır)
    /// </summary>
    public void ClearAllSkillFilters()
    {
        ClearSelection();
        for (int i = 0; i < filterImages.Length; i++)
        {
            if (filterImages[i] != null)
            {
                int remaining = currentAb?.GetSkillRemaining(i) ?? 0;
                PaintSlot(i, (remaining == 0) ? emptyColor : Color.clear);
            }
        }
    }

    private Color GetBaseColor(int slot)
    {
        int remaining = currentAb?.GetSkillRemaining(slot) ?? 0;
        return (remaining == 0) ? emptyColor : Color.clear;
    }

    private void PaintSlot(int slot, Color color)
    {
        if (slot < 0 || slot >= filterImages.Length)
            return;
        if (filterImages[slot] != null)
            filterImages[slot].color = color;
    }

    public void SelectSkill(int index)
    {
        if (index < 0 || index >= filterImages.Length)
            return;

        if (SelectedIndex == index)
        {
            ClearSelection();
            return;
        }

        ClearSelection();
        SelectedIndex = index;
        PaintSlot(index, selectionColor);
    }

    public void ClearSelection()
    {
        if (filterImages == null)
            return;

        for (int i = 0; i < filterImages.Length; i++)
        {
            if (filterImages[i] == null)
                continue;

            if (filterImages[i].color == selectionColor)
                PaintSlot(i, GetBaseColor(i));
        }

        SelectedIndex = null;
    }

    public void SetConfirmed(int index, bool on)
    {
        if (index < 0 || index >= filterImages.Length)
            return;

        if (on)
        {
            ClearSelection();
            SelectedIndex = index;
            PaintSlot(index, confirmColor);
        }
        else
        {
            if (SelectedIndex == index)
                SelectedIndex = null;
            PaintSlot(index, GetBaseColor(index));
        }
    }

    [Obsolete("Use SelectSkill instead")] public void HighlightSkill(int slot) => SelectSkill(slot);
    [Obsolete("Use SetConfirmed instead")] public void ConfirmSkill(int slot) => SetConfirmed(slot, true);
}
