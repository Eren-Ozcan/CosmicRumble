// Assets/Scripts/Abilities/AbilityController.cs
using UnityEngine;
using System.Collections.Generic;

public class AbilityController : MonoBehaviour
{
    public List<MonoBehaviour> abilityScripts;
    private List<IAbility> abilities = new List<IAbility>();

    void Awake()
    {
        foreach (var mb in abilityScripts)
        {
            IAbility ia = mb as IAbility;
            if (ia != null)
                abilities.Add(ia);
            else
                Debug.LogWarning($"{mb.name} IAbility implement etmiyor!");
        }
    }

    void Update()
    {
        // 1) Önce hangi skill/weapon seçilecek?
        foreach (var ability in abilities)
        {
            if (Input.GetKeyDown(ability.ActivationKey))
            {
                SelectAbility(ability);
            }
        }

        // 2) Seçili skill/weapon’ü bul
        IAbility selected = abilities.Find(a => a.IsSelected);
        if (selected == null)
            return;

        // 3) Eğer seçili ShieldSkill ise, Enter tuşuna basınca direkt UseAbility
        if (selected is ShieldSkill)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                selected.UseAbility();
            }
        }
        // 4) Diğer tüm yetenekler (silahlar, teleport vb.) için sol tıkla UseAbility
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                selected.UseAbility();
            }
        }
    }

    void SelectAbility(IAbility abilityToSelect)
    {
        foreach (var a in abilities)
            a.IsSelected = false;

        abilityToSelect.IsSelected = true;
        Debug.Log($"[AbilityController] Seçilen skill: {abilityToSelect.GetType().Name}");

        WeaponUIManager.UpdateUI(abilities);
        SkillUIManager.UpdateUI(abilities);
    }
}
