using UnityEngine;

/// <summary>
/// Base class for projectile style abilities. Provides hooks for UI selection
/// handling so that derived abilities only need to manage their own gameplay
/// logic.
/// </summary>
public abstract class BaseProjectileAbility : MonoBehaviour
{
    [SerializeField]
    private int uiSlotIndex = -1;

    /// <summary>
    /// Index of this ability in the UIManager skill arrays.
    /// </summary>
    protected int UISlotIndex => uiSlotIndex;

    /// <summary>
    /// Called when the ability starts waiting for confirmation/aiming. Marks
    /// the corresponding UI slot as selected.
    /// </summary>
    public virtual void OnSelect()
    {
        UIManager.Instance?.SelectSkill(uiSlotIndex);
    }

    /// <summary>
    /// Called when the ability selection is cancelled or after firing. This
    /// clears any selection/confirmation visuals from the UI.
    /// </summary>
    public virtual void OnCancelSelection()
    {
        if (UIManager.Instance == null)
            return;

        UIManager.Instance.SetConfirmed(uiSlotIndex, false);

        if (UIManager.Instance.SelectedIndex == uiSlotIndex)
            UIManager.Instance.ClearSelection();
    }

    /// <summary>
    /// Called when the ability receives user confirmation (e.g. press Enter).
    /// Highlights the slot in green.
    /// </summary>
    public virtual void OnConfirm()
    {
        UIManager.Instance?.SetConfirmed(uiSlotIndex, true);
    }
}
