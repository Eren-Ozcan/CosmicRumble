public interface IAbility
{
    void UseAbility();
    bool IsSelected { get; set; }
    UnityEngine.KeyCode ActivationKey { get; }
}
