using UnityEngine;
using System.Collections.Generic;

public static class WeaponUIManager
{
    public static void UpdateUI(List<IAbility> abilities)
    {
        foreach (IAbility a in abilities)
        {
            if (a is AbilityBase)
            {
                if (a.IsSelected)
                {
#if UNITY_EDITOR
                    Debug.Log($"[WeaponUI] Secili silah: {a.GetType().Name}");
#endif
                }
            }
        }
    }
}
