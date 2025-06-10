using UnityEngine;
using System.Collections.Generic;

public static class SkillUIManager
{
    public static void UpdateUI(List<IAbility> abilities)
    {
        foreach (IAbility a in abilities)
        {
            if (!(a is WeaponBase))
            {
                if (a.IsSelected)
                    Debug.Log($"[SkillUI] Secili skill: {a.GetType().Name}");
            }
        }
    }
}
