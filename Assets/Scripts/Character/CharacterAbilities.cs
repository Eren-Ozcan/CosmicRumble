// Assets/Scripts/Character/CharacterAbilities.cs

using UnityEngine;
using System;

public class CharacterAbilities : MonoBehaviour
{
    [Header("Super Jump Ayarlari")]
    public int maxSuperJumps = 3;
    [HideInInspector]
    public int superJumpsRemaining;

    [Header("RPG Ammo Ayarlari")]
    public int maxRpgAmmo = 4;
    [HideInInspector]
    public int rpgAmmoRemaining;

    [Header("Tabanca Ammo Ayarlari")]
    public int pistolAmmo = -1;  // -1 sinirsiz

    // 1) Event tanımları:
    public event Action SuperJumpChanged;
    public event Action RpgAmmoChanged;
    public event Action PistolAmmoChanged;

    private void Awake()
    {
        superJumpsRemaining = maxSuperJumps;
        rpgAmmoRemaining = maxRpgAmmo;
    }

    public bool UseSuperJump()
    {
        if (superJumpsRemaining > 0)
        {
            superJumpsRemaining--;
            SuperJumpChanged?.Invoke();
            return true;
        }
        return false;
    }

    public int GetSuperJumpsRemaining()
    {
        return superJumpsRemaining;
    }

    public bool UseRpg()
    {
        if (rpgAmmoRemaining > 0)
        {
            rpgAmmoRemaining--;
            RpgAmmoChanged?.Invoke();
            return true;
        }
        return false;
    }

    public int GetRpgAmmoRemaining()
    {
        return rpgAmmoRemaining;
    }

    public bool UsePistol()
    {
        if (pistolAmmo == 0)
            return false;

        if (pistolAmmo > 0)
            pistolAmmo--;

        PistolAmmoChanged?.Invoke();
        return true;
    }

    public int GetPistolAmmo()
    {
        return pistolAmmo;
    }
}
