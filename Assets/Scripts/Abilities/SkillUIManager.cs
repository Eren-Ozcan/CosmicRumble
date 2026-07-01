using UnityEngine;
using System.Collections.Generic;

public static class SkillUIManager
{
    public static void UpdateUI(List<IAbility> abilities)
    {
        foreach (IAbility a in abilities)
        {
            if (!(a is AbilityBase))
            {
                if (a.IsSelected)
                {
#if UNITY_EDITOR
                    Debug.Log($"[SkillUI] Secili skill: {a.GetType().Name}");
#endif
                }
            }
        }
    }
}
