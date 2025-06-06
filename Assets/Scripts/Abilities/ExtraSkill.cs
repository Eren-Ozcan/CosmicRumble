using UnityEngine;

public class ExtraSkill : MonoBehaviour, IAbility
{
    public KeyCode ActivationKey { get; private set; }
    public bool IsSelected { get; set; }
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;

    void Start()
    {
        ActivationKey = KeyCode.Alpha9;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!IsSelected || cooldownTimer > 0f) return;
        if (Input.GetMouseButtonDown(0))
        {
            UseAbility();
        }
    }

    public void UseAbility()
    {
        Debug.Log("ExtraSkill kullanildi!");
        cooldownTimer = cooldownTime;
        IsSelected = false;
    }
}
