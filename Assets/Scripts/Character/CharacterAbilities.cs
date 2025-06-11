using UnityEngine;
using System;

public class CharacterAbilities : MonoBehaviour
{
    private const int TotalSlots = 10;  // Toplam slot sayısı

    [Header("Super Jump Ayarlari")]
    public int maxSuperJumps = 3;
    [HideInInspector] public int superJumpsRemaining;

    [Header("RPG Ammo Ayarlari")]
    public int maxRpgAmmo = 4;
    [HideInInspector] public int rpgAmmoRemaining;

    [Header("Tabanca Ammo Ayarlari")]
    public int pistolAmmo = -1;  // -1 = sinirsiz

    [Header("Shotgun Ayarlari")]
    public int maxShotgunAmmo = 5;
    [HideInInspector] public int shotgunAmmoRemaining;

    [Header("El Bombasi Ayarlari")]
    public int maxGrenades = 2;
    [HideInInspector] public int grenadesRemaining;

    [Header("Shield Ayarlari")]
    public int maxShields = 1;
    [HideInInspector] public int shieldsRemaining;

    // Bireysel event'ler
    public event Action SuperJumpChanged;
    public event Action RpgAmmoChanged;
    public event Action PistolAmmoChanged;
    public event Action ShotgunAmmoChanged;

    // Genel slot-change event
    public event Action<int> SkillChanged;

    private void Awake()
    {
        superJumpsRemaining = maxSuperJumps;
        rpgAmmoRemaining = maxRpgAmmo;
        shotgunAmmoRemaining = maxShotgunAmmo;
        grenadesRemaining = maxGrenades;
        shieldsRemaining = maxShields;

        SuperJumpChanged?.Invoke();
        RpgAmmoChanged?.Invoke();
        PistolAmmoChanged?.Invoke();
        ShotgunAmmoChanged?.Invoke();

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
            SkillChanged?.Invoke(2);
            return true;
        }
        return false;
    }

    public bool UsePistol()
    {
        if (pistolAmmo == 0)
            return false;
        if (pistolAmmo > 0)
            pistolAmmo--;
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
        if (grenadesRemaining == 0)
            return false;
        grenadesRemaining--;
        SkillChanged?.Invoke(3);
        return true;
    }

    public bool UseShield()
    {
        if (shieldsRemaining == 0)
            return false;
        shieldsRemaining--;
        SkillChanged?.Invoke(5);
        return true;
    }

    // Getter metodlar
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
