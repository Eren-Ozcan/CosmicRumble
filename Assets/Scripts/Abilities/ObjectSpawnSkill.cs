using UnityEngine;

public class ObjectSpawnSkill : MonoBehaviour, IAbility
{
    public KeyCode ActivationKey { get; private set; }
    public bool IsSelected { get; set; }
    public GameObject objectPrefab;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;

    void Start()
    {
        ActivationKey = KeyCode.Alpha7;
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
        if (objectPrefab != null)
            Instantiate(objectPrefab, targetPos, Quaternion.identity);

        cooldownTimer = cooldownTime;
        IsSelected = false;
    }
}
