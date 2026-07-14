using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class Bomb : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha0;
    public float cooldownTime = 8f;

    public override int SlotIndex => 9;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 10f;

    [Header("Bomb Settings")]
    public float fuseTime = 2f;
    public GameObject explosionPrefab;

    private bool isDragging;
    private Vector2 dragStart;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnFireUpdate()
    {
        Vector2 mouseWorld = PointerWorldPosition;

        if (PointerDown)
        {
            isDragging = true;
            dragStart = mouseWorld;
        }
        else if (isDragging && PointerHeld)
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && PointerUp)
        {
            Fire();
            cooldownTimer = cooldownTime;
            charAbilities?.OnAbilityConsumed();
            CancelAim();
            fireAllowed = false;
            isSelected = false;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector2 pull = dragStart - PointerWorldPosition;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        // Networked modda atış isteği server'a taşınır (diğer 9 yetenekle aynı desen) — bu silah
        // 2026-07-14 güvenlik geçişinde atlanmıştı: bomba yalnızca atanın makinesinde var oluyor
        // (client atınca hasar no-op, host atınca rakip görünmez bombadan hasar alıyordu) ve
        // ServerCanAct/ServerTryConsume hiç işlemiyordu. Offline hotseat'te eski yol aynen çalışır.
        if (IsSpawned) FireServerRpc(initial);
        else SpawnAndInit(initial);
    }

    [ServerRpc]
    private void FireServerRpc(Vector2 initialVelocity)
    {
        if (!ServerCanAct) return;
        if (charAbilities != null && !charAbilities.ServerTryConsume(SlotIndex)) return;
        SpawnAndInit(ClampFireVelocity(initialVelocity, maxDragDistance, powerMultiplier));
    }

    private void SpawnAndInit(Vector2 initialVelocity)
    {
        AnnounceFire("weapon_bomb_place", "weapon_bomb", isWeapon: true);

        GameObject bombObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        if (IsSpawned) bombObj.GetComponent<NetworkObject>().Spawn();

        Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Prefab'a eklenen NetworkRigidbody2D offline'da body'yi Kinematic'e zorlar —
            // diğer mermilerin Init() yollarındaki düzeltmenin aynısı (bkz. NetworkPhysicsGuard).
            NetworkPhysicsGuard.EnsureDynamicWhenNotSpawned(rb);
            rb.AddForce(initialVelocity, ForceMode2D.Impulse);
        }

        // BombBehaviour (fitil + patlama akışı) yalnızca simülasyonu yöneten makinede gerekir;
        // client kopyaları prefab'daki BombExplosion ile yalnızca yerel görsel temas efektini oynatır.
        BombBehaviour bb = bombObj.AddComponent<BombBehaviour>();
        bb.Init(fuseTime, explosionPrefab);
        // TurnManager bildirimleri BombBehaviour.Init() içinde yapılıyor
    }

    protected override void CancelAim()
    {
        isDragging = false;
        base.CancelAim(); // trajectory?.Hide()
    }
}
