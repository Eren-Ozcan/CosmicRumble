using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Slot’lar (0=Pistol,1=Shotgun,2=RPG,3=Grenade,4=SuperJump,5=Shield)")]
    public Image[] filterImages;         // 10 elemanlı dizi
    public TextMeshProUGUI[] countTexts; // 10 elemanlı dizi

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
            currentAb.SkillChanged -= UpdateSlot;

        currentAb = ab;

        for (int i = 0; i < filterImages.Length; i++)
            UpdateSlot(i);

        currentAb.SkillChanged += UpdateSlot;
    }

    private void UpdateSlot(int slotIndex)
    {
        int left = currentAb.GetSkillRemaining(slotIndex);
        countTexts[slotIndex].text = left.ToString();
        filterImages[slotIndex].color = (left == 0 ? emptyColor : Color.clear);
    }

    public void HighlightSkill(int slot)
    {
        filterImages[slot].color = selectionColor;
    }

    public void ConfirmSkill(int slot)
    {
        filterImages[slot].color = confirmColor;
    }
}
