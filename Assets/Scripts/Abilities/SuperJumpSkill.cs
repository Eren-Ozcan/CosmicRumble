using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class SuperJumpSkill : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha5;
    public float cooldownTime = 5f;

    public override int SlotIndex => 4;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    protected override void Awake()
    {
        base.Awake();
        gravityBody.onSuperJumpConsumed += ClearSuperJumpFilter;
    }

    public override void OnDestroy()
    {
        if (gravityBody != null)
            gravityBody.onSuperJumpConsumed -= ClearSuperJumpFilter;
        base.OnDestroy();
    }

    private void ClearSuperJumpFilter()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.filterImages[SlotIndex].color = Color.clear;
    }

    protected override void OnFireUpdate()
    {
        if (charAbilities != null && charAbilities.GetSuperJumpsRemaining() <= 0)
        {
            charAbilities.DeselectAll();
            isSelected = false;
            fireAllowed = false;
            return;
        }

        bool canUse = charAbilities == null || charAbilities.UseSuperJump();
        if (canUse)
        {
            // Networked modda gerçek tüketim + "sıradaki zıplama super" bayrağı server'a taşınır
            // (diğer ability'lerle aynı desen — client-side UseSuperJump() yalnızca iyimser UI/tahmin
            // içindir, gerçek kontrol ServerTryConsume'da) — offline hotseat'te eski doğrudan yol
            // aynen çalışır.
            if (IsSpawned) ConsumeServerRpc();
            else gravityBody.nextJumpIsSuper = true;

            charAbilities?.OnAbilityConsumed();
            cooldownTimer = cooldownTime;
            AchievementEvents.FireAbilityUsed("skill_superjump");
            AudioManager.Instance?.PlaySfx("skill_superjump");
        }
        isSelected = false;
        fireAllowed = false;
    }

    [ServerRpc]
    private void ConsumeServerRpc()
    {
        if (!ServerCanAct) return;
        if (charAbilities == null || !charAbilities.ServerTryConsume(SlotIndex)) return;

        // nextJumpIsSuper yalnızca sahibinin kendi Update()'inde okunan plain bir bool (GravityBody
        // input'u owner-only) — bu yüzden server, gerçek sahibin makinesine hedefli bir ClientRpc ile
        // "bir sonraki zıplaman süper olsun" der (GravityBody.ApplyForce/Teleport'taki owner-hedefli
        // ClientRpc deseniyle birebir aynı).
        var rpcParams = new ClientRpcParams
        { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } };
        ApplySuperJumpClientRpc(rpcParams);
    }

    [ClientRpc]
    private void ApplySuperJumpClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        gravityBody.nextJumpIsSuper = true;
    }
}
