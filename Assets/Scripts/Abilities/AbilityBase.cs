using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tüm silah/ability'ler için ortak boilerplate.
/// Cooldown, onay akışı, wasActive takibi ve TrajectoryDots yönetimini sağlar.
/// Alt sınıflar: SlotIndex, ActivationKey, CooldownTime ve OnFireUpdate() tanımlamalı.
///
/// NetworkBehaviour: Player prefabı zaten bir NetworkObject taşıdığı için ability'ler de
/// (GravityBody/TurnManager ile aynı emsal) networked hale gelebiliyor — [ServerRpc] ile ateşleme
/// isteğini server'a taşıyan alt sınıflar (bkz. Pistol.cs) için gerekli. IsSpawned=false olan
/// offline hotseat'te davranış etkilenmez.
/// </summary>
public abstract class AbilityBase : NetworkBehaviour, IAbilitySelectable, ICooldownResettable
{
    // ── Nişan/ateş için mouse+touch ortak pointer erişimi ────────
    // Pointer.current, son kullanılan pointer cihazına (Mouse, Touchscreen'in
    // primary touch'ı, Pen) otomatik işaret eder — masaüstünde mouse, mobilde
    // parmak, tek kod yolu. press: mouse'ta sol tık, touch'ta ekrana dokunma.
    protected static Vector2 PointerWorldPosition
    {
        get
        {
            Vector2 screenPos = Pointer.current != null
                ? Pointer.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
            return Camera.main.ScreenToWorldPoint(screenPos);
        }
    }

    protected static bool PointerDown => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    protected static bool PointerHeld => Pointer.current != null && Pointer.current.press.isPressed;
    protected static bool PointerUp => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;

    // ── Ortak field'lar ──────────────────────────────────────────
    protected float cooldownTimer;
    protected bool awaitingConfirmation;
    protected bool fireAllowed;
    protected bool isSelected;
    protected bool wasActive;
    protected GravityBody gravityBody;
    protected CharacterAbilities charAbilities;
    protected TrajectoryDots trajectory;

    // ── Alt sınıfın tanımlaması gerekenler ──────────────────────
    public abstract int SlotIndex { get; }
    public abstract KeyCode ActivationKey { get; }
    public abstract float CooldownTime { get; }

    /// <summary>
    /// Server-side güvenlik kontrolü — her [ServerRpc] ateşleme/aktivasyon handler'ının İLK
    /// satırında çağrılmalı. Client-side Update() akışındaki isActive.Value kontrolü yalnızca
    /// bu client'ın kendi UI/input döngüsünü kısıtlar; değiştirilmiş bir client [ServerRpc]'yi
    /// Update() akışını hiç çalıştırmadan (reflection/özel NGO mesajıyla) doğrudan çağırabilir.
    /// Bu property sunucunun kendi "gerçekten bu karakterin sırası mı" doğrulamasıdır — hem sıra
    /// dışı ateşlemeyi hem de aynı turda mermi henüz çözülmeden ikinci bir ateşlemeyi engeller
    /// (TurnManager.NotifyProjectileLaunched ilk ateşten hemen sonra isActive.Value'yu senkron
    /// olarak false yapar, bu yüzden art arda çağrılan bir RPC ikinci denemede burada reddedilir).
    /// </summary>
    protected bool ServerCanAct => gravityBody != null && gravityBody.isActive.Value;

    /// <summary>
    /// Client'tan gelen atış hızını meşru üst sınıra kırpar — nişan/güç client'ta hesaplandığı
    /// için değiştirilmiş bir client [ServerRpc]'ye istediği büyüklükte vektör gönderebilir.
    /// Meşru maksimum her silahta maxDragDistance * powerMultiplier'dır; server bunun üstünü
    /// kabul etmez (BatHammerSkill.SwingServerRpc'deki normalize/Clamp01 ile aynı ilke).
    /// NaN/Infinity içeren kasıtlı bozuk girdiler sıfırlanır.
    /// </summary>
    protected static Vector2 ClampFireVelocity(Vector2 v, float maxDragDistance, float powerMultiplier)
    {
        if (float.IsNaN(v.x) || float.IsNaN(v.y) ||
            float.IsInfinity(v.x) || float.IsInfinity(v.y))
            return Vector2.zero;

        float max = Mathf.Abs(maxDragDistance * powerMultiplier);
        if (max <= 0f) return Vector2.zero;
        return Vector2.ClampMagnitude(v, max);
    }

    // ── IAbilitySelectable ───────────────────────────────────────
    public virtual void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
        if (!selected) CancelAim();
    }

    public virtual void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelAim();
    }

    /// <summary>
    /// Enter tuşuyla aynı onay adımı — UI/dokunmatik tık tarafından da çağrılabilir
    /// (bkz. CharacterAbilities.ConfirmSkill), klavyeye özel değildir.
    /// </summary>
    public virtual void Confirm()
    {
        if (!isSelected || !awaitingConfirmation) return;
        fireAllowed = true;
        awaitingConfirmation = false;
        UIManager.Instance?.ConfirmSkill(SlotIndex);
        TurnManager.NotifyWeaponConfirmed();
    }

    // ── ICooldownResettable ──────────────────────────────────────
    public virtual void ResetCooldown()
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelAim();
    }

    // ── Unity lifecycle ──────────────────────────────────────────
    protected virtual void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();
        trajectory = GetComponent<TrajectoryDots>()
                  ?? GetComponentInChildren<TrajectoryDots>(true)
#if UNITY_2022_2_OR_NEWER
                  ?? FindFirstObjectByType<TrajectoryDots>(FindObjectsInactive.Include);
#else
                  ?? FindObjectOfType<TrajectoryDots>(true);
#endif
    }

    protected virtual void Update()
    {
        // 1. Tur kullanım kontrolü
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn) return;

        // 2. Cooldown azalt
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // 3. isActive değişim tespiti
        if (gravityBody != null && gravityBody.isActive.Value && !wasActive)
        {
            wasActive = true;
            Cancel();
        }
        else if (gravityBody != null && !gravityBody.isActive.Value)
        {
            wasActive = false;
            return;
        }

        // 3b. Networked modda sadece bu karakterin sahibi input okuyabilir (offline hotseat'te
        // IsSpawned=false olduğu için bu kontrol devre dışı kalır, eski davranış korunur).
        if (gravityBody != null && gravityBody.IsSpawned && !gravityBody.IsOwner) return;

        // 4. Cooldown dolmadıysa aim iptal
        if (cooldownTimer > 0f) { CancelAim(); return; }

        // 5. Seçili değilse aktivasyon tuşunu dinle
        if (!isSelected)
        {
            if (Input.GetKeyDown(ActivationKey))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        // 6. Onay bekleniyor
        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Confirm();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
            }
            return;
        }

        // 7. Ateş hazır değilse dur
        if (!fireAllowed) return;

        // 8. Alt sınıfın nişan/ateş mantığı
        OnFireUpdate();
    }

    // ── Alt sınıfın implement etmesi gerekenler ─────────────────
    /// <summary>fireAllowed=true olduğunda her frame çağrılır.</summary>
    protected abstract void OnFireUpdate();

    /// <summary>Nişan/drag görselini iptal eder. Override ile özelleştir.</summary>
    protected virtual void CancelAim()
    {
        trajectory?.Hide();
    }
}
