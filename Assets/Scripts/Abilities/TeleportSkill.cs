using UnityEngine;

public class TeleportSkill : MonoBehaviour, IAbility
{
    public KeyCode ActivationKey { get; private set; }
    public bool IsSelected { get; set; }
    public Transform playerTransform;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;

    void Start()
    {
        ActivationKey = KeyCode.Alpha8;
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
        Vector2 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (playerTransform != null)
            playerTransform.position = new Vector3(targetPos.x, targetPos.y, playerTransform.position.z);

        cooldownTimer = cooldownTime;
        IsSelected = false;
    }
}
