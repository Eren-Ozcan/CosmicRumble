// Assets/Scripts/CharacterAbilities.cs
using UnityEngine;
using System;

public class CharacterAbilities : MonoBehaviour
{
    private const int TotalSlots = 10;

    // Her turn sadece bir skill kullanabilme kontrolü
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

    private void Awake()
    {
        // Başlangıç değerleri
        superJumpsRemaining = maxSuperJumps;
        rpgAmmoRemaining = maxRpgAmmo;
        shotgunAmmoRemaining = maxShotgunAmmo;
        grenadesRemaining = maxGrenades;
        shieldsRemaining = maxShields;

        // İlk UI güncellemesi için tüm event’leri tetikle
        SuperJumpChanged?.Invoke();
        RpgAmmoChanged?.Invoke();
        PistolAmmoChanged?.Invoke();
        ShotgunAmmoChanged?.Invoke();
        GrenadeChanged?.Invoke();
        ShieldChanged?.Invoke();

        // Her slot’u güncelle
        for (int i = 0; i < TotalSlots; i++)
            SkillChanged?.Invoke(i);
    }

    // Kullanım metodları
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
            SkillChanged?.Invoke(2); // slot index
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

    // Getter’lar
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
            default: return 0;
        }
    }
}
