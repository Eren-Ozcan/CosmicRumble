// Assets/Scripts/UIManager.cs
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
        if (slotIndex < 0 || slotIndex >= filterImages.Length) return;

        filterImages[slotIndex].color = isEmpty ? emptyColor : Color.clear;
    }

    private void UpdateSlot(int slotIndex)
    {
        // Her slot için UI öğelerini aktif et
        filterImages[slotIndex].gameObject.SetActive(true);
        countTexts[slotIndex].gameObject.SetActive(true);

        // Kalan miktarı al
        int left = currentAb.GetSkillRemaining(slotIndex);
        countTexts[slotIndex].text = left.ToString();

        // Stoğa göre renklendirme
        filterImages[slotIndex].color = (left == 0 ? emptyColor : Color.clear);
    }

    public void HighlightSkill(int slot)
        => filterImages[slot].color = selectionColor;

    public void ConfirmSkill(int slot)
        => filterImages[slot].color = confirmColor;
}
