using UnityEngine;

public interface IAbility
{
    void UseAbility();
    bool IsSelected { get; set; }
    KeyCode ActivationKey { get; }
}