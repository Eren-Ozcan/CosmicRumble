using UnityEngine;
using System;

public interface IAbilitySelectable
{
    int SlotIndex { get; }
    void SetSelected(bool selected);
    void Cancel();
}

// ✨ DEĞİŞİKLİK: Cooldown reset için opsiyonel arayüz
public interface ICooldownResettable
{
    void ResetCooldown();
}

public class CharacterAbilities : MonoBehaviour
{
    private const int TotalSlots = 10;

    // turn başına 1 skill kuralı
    public bool HasUsedSkillThisTurn { get; set; }

    [Header("Super Jump Ayarları")]
    public int maxSuperJumps = 3;
    [HideInInspector] public int superJumpsRemaining;

    [Header("RPG Ammo Ayarları")]
    public int maxRpgAmmo = 4;
    [HideInInspector] public int rpgAmmoRemaining;

    [Header("Tabanca Ammo Ayarları")]
    public int pistolAmmo = -1;  // -1 = sınırsız

    [Header("Shotgun Ayarları")]
    public int maxShotgunAmmo = 5;
    [HideInInspector] public int shotgunAmmoRemaining;

    [Header("El Bombası Ayarları")]
    public int maxGrenades = 2;
    [HideInInspector] public int grenadesRemaining;

    [Header("Shield Ayarları")]
    public int maxShields = 1;
    [HideInInspector] public int shieldsRemaining;

    // Bireysel event’ler
    public event Action SuperJumpChanged;
    public event Action RpgAmmoChanged;
    public event Action PistolAmmoChanged;
    public event Action ShotgunAmmoChanged;
    public event Action GrenadeChanged;
    public event Action ShieldChanged;

    // Genel slot-change event
    public event Action<int> SkillChanged;

    private IAbilitySelectable[] abilitySlots = new IAbilitySelectable[TotalSlots];

    private void Awake()
    {
        // Başlangıç değerleri
        superJumpsRemaining = maxSuperJumps;
        rpgAmmoRemaining = maxRpgAmmo;
        shotgunAmmoRemaining = maxShotgunAmmo;
        grenadesRemaining = maxGrenades;
        shieldsRemaining = maxShields;

        // İlk UI tetikleri
        SuperJumpChanged?.Invoke();
        RpgAmmoChanged?.Invoke();
        PistolAmmoChanged?.Invoke();
        ShotgunAmmoChanged?.Invoke();
        GrenadeChanged?.Invoke();
        ShieldChanged?.Invoke();

        for (int i = 0; i < TotalSlots; i++)
            SkillChanged?.Invoke(i);

        // slota göre ability’leri yerleştir
        var comps = GetComponents<IAbilitySelectable>();
        foreach (var ab in comps)
        {
            if (ab.SlotIndex >= 0 && ab.SlotIndex < TotalSlots)
                abilitySlots[ab.SlotIndex] = ab;
        }
    }

    // --- Kullanım metodları (ammo düşürme + UI events) ---
    public bool UseSuperJump()
    {
        if (superJumpsRemaining > 0)
        {
            superJumpsRemaining--;
            SuperJumpChanged?.Invoke();
            SkillChanged?.Invoke(4);
            return true;
        }
        return false;
    }

    public bool UseRpg()
    {
        if (rpgAmmoRemaining > 0)
        {
            rpgAmmoRemaining--;
            RpgAmmoChanged?.Invoke();
            SkillChanged?.Invoke(2);
            return true;
        }
        return false;
    }

    public bool UsePistol()
    {
        if (pistolAmmo == 0) return false;
        if (pistolAmmo > 0) pistolAmmo--;
        PistolAmmoChanged?.Invoke();
        SkillChanged?.Invoke(0);
        return true;
    }

    public bool UseShotgun()
    {
        if (shotgunAmmoRemaining > 0)
        {
            shotgunAmmoRemaining--;
            ShotgunAmmoChanged?.Invoke();
            SkillChanged?.Invoke(1);
            return true;
        }
        return false;
    }

    public bool UseGrenade()
    {
        if (grenadesRemaining == 0) return false;
        grenadesRemaining--;
        GrenadeChanged?.Invoke();
        SkillChanged?.Invoke(3);
        return true;
    }

    public bool UseShield()
    {
        if (shieldsRemaining == 0) return false;
        shieldsRemaining--;
        ShieldChanged?.Invoke();
        SkillChanged?.Invoke(5);
        return true;
    }

    // --- Getter’lar ---
    public int GetSuperJumpsRemaining() => superJumpsRemaining;
    public int GetRpgAmmoRemaining() => rpgAmmoRemaining;
    public int GetPistolAmmo() => pistolAmmo;
    public int GetShotgunAmmo() => shotgunAmmoRemaining;
    public int GetGrenadesRemaining() => grenadesRemaining;
    public int GetShieldsRemaining() => shieldsRemaining;

    public int GetSkillRemaining(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return pistolAmmo;
            case 1: return shotgunAmmoRemaining;
            case 2: return rpgAmmoRemaining;
            case 3: return grenadesRemaining;
            case 4: return superJumpsRemaining;
            case 5: return shieldsRemaining;
            case 6: return 1; // Teleport (sınırsız)
            case 7: return 1; // Bat/Hammer (sınırsız)
            case 8: return 1; // Black Hole (sınırsız)
            default: return 0;
        }
    }

    // --- Tek-seçim çekirdeği ---
    public void SelectSkill(int idx)
    {
        if (idx < 0 || idx >= abilitySlots.Length) return;

        // tümünü kapat + iptal
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            var ab = abilitySlots[i];
            if (ab == null) continue;
            if (i != idx)
            {
                ab.SetSelected(false);
                ab.Cancel();
            }
        }

        // yalnız seçileni aç
        var target = abilitySlots[idx];
        if (target != null)
        {
            target.SetSelected(true);
            UIManager.Instance?.HighlightSelected(idx);
        }
    }

    public void DeselectAll()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i]?.SetSelected(false);
            abilitySlots[i]?.Cancel();
        }
        UIManager.Instance?.ClearAllSkillSelections();
    }

    /// <summary>
    /// ✨ DEĞİŞİKLİK: Tüm skiller için cooldown sıfırlama (opsiyonel destekleyenler)
    /// </summary>
    public void ResetAllCooldowns()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            var ab = abilitySlots[i] as ICooldownResettable;
            ab?.ResetCooldown();
        }
    }

    /// <summary>
    /// ✨ DEĞİŞİKLİK: Turn başında tek noktadan reset (flag + seçim + cooldown)
    /// </summary>
    public void ResetTurnState()
    {
        HasUsedSkillThisTurn = false;
        // seçimleri temizle
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i]?.SetSelected(false);
            abilitySlots[i]?.Cancel();
        }
        // cooldown’ları sıfırla
        ResetAllCooldowns();
    }

    /// <summary>
    /// Başarılı aktivasyondan sonra ability'ler tarafından çağrılır: turn kullanımını işaretler ve UI'ı kilitler.
    /// </summary>
    public void OnAbilityConsumed()
    {
        HasUsedSkillThisTurn = true;
        UIManager.Instance?.LockAllSkillsUI();
    }
}
