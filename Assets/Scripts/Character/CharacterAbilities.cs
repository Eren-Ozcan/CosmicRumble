using UnityEngine;
using System;
using Unity.Netcode;

public interface IAbilitySelectable
{
    int SlotIndex { get; }
    void SetSelected(bool selected);
    void Confirm();
    void Cancel();
}

// ✨ DEĞİŞİKLİK: Cooldown reset için opsiyonel arayüz
public interface ICooldownResettable
{
    void ResetCooldown();
}

/// <summary>
/// Cephane sayaçları + "turda bir yetenek" bayrağı — güvenlik denetiminde (2026-07-14) bulunan
/// "ammo/tur kısıtlaması sadece client-side, hileli bir client sınırsız RPG/el bombası ateşleyebilir"
/// açığının kapatılması için tek bir serileştirilebilir struct'ta toplanıp NetworkVariable'a taşındı
/// (GravityBody/CharacterHealth'teki server-write NetworkVariable deseniyle birebir aynı).
/// </summary>
public struct AmmoState : INetworkSerializable, IEquatable<AmmoState>
{
    public int superJumps;
    public int rpgAmmo;
    public int pistolAmmo;   // -1 = sınırsız
    public int shotgunAmmo;
    public int grenades;
    public int shields;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref superJumps);
        serializer.SerializeValue(ref rpgAmmo);
        serializer.SerializeValue(ref pistolAmmo);
        serializer.SerializeValue(ref shotgunAmmo);
        serializer.SerializeValue(ref grenades);
        serializer.SerializeValue(ref shields);
    }

    public bool Equals(AmmoState other) =>
        superJumps == other.superJumps && rpgAmmo == other.rpgAmmo && pistolAmmo == other.pistolAmmo &&
        shotgunAmmo == other.shotgunAmmo && grenades == other.grenades && shields == other.shields;
}

public class CharacterAbilities : NetworkBehaviour
{
    private const int TotalSlots = 10;

    // Server-authoritative cephane + turda-bir-yetenek bayrağı. Offline hotseat'te (IsSpawned=false)
    // eskisi gibi doğrudan mutasyona uğrar; online'da yalnızca server yazar — asıl düşürme/kontrol
    // ServerTryConsume() içinde, her ability'nin [ServerRpc] handler'ından (ServerCanAct'ten SONRA)
    // çağrılır. Client-side Use*() metodları artık yalnızca "mevcut mu" okuyup UI'ı iyimser şekilde
    // güncelliyor — networked modda gerçek düşürme burada olmadığı için client bunu ATLAYAMAZ.
    private readonly NetworkVariable<AmmoState> netAmmo =
        new NetworkVariable<AmmoState>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> netHasUsedSkill =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // turn başına 1 skill kuralı — artık server-authoritative (bkz. yukarı).
    public bool HasUsedSkillThisTurn => netHasUsedSkill.Value;

    [Header("Super Jump Ayarları")]
    public int maxSuperJumps = 3;

    [Header("RPG Ammo Ayarları")]
    public int maxRpgAmmo = 4;

    [Header("Tabanca Ammo Ayarları")]
    public int pistolAmmo = -1;  // -1 = sınırsız (başlangıç değeri — netAmmo'ya kopyalanır)

    [Header("Shotgun Ayarları")]
    public int maxShotgunAmmo = 5;

    [Header("El Bombası Ayarları")]
    public int maxGrenades = 2;

    [Header("Shield Ayarları")]
    public int maxShields = 1;

    // Bireysel event'ler
    public event Action SuperJumpChanged;
    public event Action RpgAmmoChanged;
    public event Action PistolAmmoChanged;
    public event Action ShotgunAmmoChanged;
    public event Action GrenadeChanged;
    public event Action ShieldChanged;

    // Genel slot-change event
    public event Action<int> SkillChanged;

    private IAbilitySelectable[] abilitySlots = new IAbilitySelectable[TotalSlots];

    private void Awake()
    {
        // Offline hotseat: IsSpawned henüz false, eski davranış aynen korunur (doğrudan set).
        // Online'da OnNetworkSpawn'da server tarafından set edilecek; client'ta bu ilk değer
        // Spawn'a kadarki kısa ömürlü bir placeholder'dır, senkron değer hemen üzerine yazar.
        if (!IsSpawned)
        {
            netAmmo.Value = new AmmoState
            {
                superJumps = maxSuperJumps,
                rpgAmmo    = maxRpgAmmo,
                pistolAmmo = pistolAmmo,
                shotgunAmmo = maxShotgunAmmo,
                grenades   = maxGrenades,
                shields    = maxShields
            };
        }

        FireAllChangeEvents();

        // slota göre ability'leri yerleştir
        var comps = GetComponents<IAbilitySelectable>();
        foreach (var ab in comps)
        {
            if (ab.SlotIndex >= 0 && ab.SlotIndex < TotalSlots)
                abilitySlots[ab.SlotIndex] = ab;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            netAmmo.Value = new AmmoState
            {
                superJumps = maxSuperJumps,
                rpgAmmo    = maxRpgAmmo,
                pistolAmmo = pistolAmmo,
                shotgunAmmo = maxShotgunAmmo,
                grenades   = maxGrenades,
                shields    = maxShields
            };
        }

        netAmmo.OnValueChanged        += (oldV, newV) => FireAllChangeEvents();
        netHasUsedSkill.OnValueChanged += (oldV, newV) => { };

        FireAllChangeEvents();
    }

    private void FireAllChangeEvents()
    {
        SuperJumpChanged?.Invoke();
        RpgAmmoChanged?.Invoke();
        PistolAmmoChanged?.Invoke();
        ShotgunAmmoChanged?.Invoke();
        GrenadeChanged?.Invoke();
        ShieldChanged?.Invoke();
        for (int i = 0; i < TotalSlots; i++)
            SkillChanged?.Invoke(i);
    }

    // --- Kullanım metodları (client-side "mevcut mu" kontrolü + iyimser UI güncelleme) ---
    // Offline'da (IsSpawned=false) gerçek düşürme burada yapılır (eski davranış aynen korunur).
    // Online'da gerçek düşürme yoktur — yalnızca ServerTryConsume() (server-side) günceller,
    // güncellenen değer netAmmo.OnValueChanged ile tüm client'lara otomatik yayılır.
    public bool UseSuperJump()
    {
        if (netAmmo.Value.superJumps <= 0) return false;
        if (!IsSpawned)
        {
            var s = netAmmo.Value;
            s.superJumps--;
            netAmmo.Value = s;
        }
        SkillChanged?.Invoke(4);
        return true;
    }

    public bool UseRpg()
    {
        if (netAmmo.Value.rpgAmmo <= 0) return false;
        if (!IsSpawned)
        {
            var s = netAmmo.Value;
            s.rpgAmmo--;
            netAmmo.Value = s;
        }
        SkillChanged?.Invoke(2);
        return true;
    }

    public bool UsePistol()
    {
        if (netAmmo.Value.pistolAmmo == 0) return false;
        if (!IsSpawned)
        {
            var s = netAmmo.Value;
            if (s.pistolAmmo > 0) s.pistolAmmo--;
            netAmmo.Value = s;
        }
        SkillChanged?.Invoke(0);
        return true;
    }

    public bool UseShotgun()
    {
        if (netAmmo.Value.shotgunAmmo <= 0) return false;
        if (!IsSpawned)
        {
            var s = netAmmo.Value;
            s.shotgunAmmo--;
            netAmmo.Value = s;
        }
        SkillChanged?.Invoke(1);
        return true;
    }

    public bool UseGrenade()
    {
        if (netAmmo.Value.grenades == 0) return false;
        if (!IsSpawned)
        {
            var s = netAmmo.Value;
            s.grenades--;
            netAmmo.Value = s;
        }
        SkillChanged?.Invoke(3);
        return true;
    }

    public bool UseShield()
    {
        if (netAmmo.Value.shields == 0) return false;
        if (!IsSpawned)
        {
            var s = netAmmo.Value;
            s.shields--;
            netAmmo.Value = s;
        }
        SkillChanged?.Invoke(5);
        return true;
    }

    public bool UseTeleport()
    {
        SkillChanged?.Invoke(6);
        return true;
    }

    public bool UseBlackHole()
    {
        SkillChanged?.Invoke(8);
        return true;
    }

    /// <summary>
    /// Sunucu tarafı gerçek tüketim — güvenlik denetiminin ana düzeltmesi. Her ability'nin
    /// [ServerRpc] handler'ı, AbilityBase.ServerCanAct kontrolünden HEMEN SONRA bunu çağırmalı;
    /// false dönerse RPC hiçbir şey yapmadan çıkmalı. Offline'da (IsSpawned=false) doğrudan;
    /// online'da yalnızca IsServer'dan çağrılabilir (zaten yalnızca server'daki [ServerRpc]
    /// gövdesinden çağrılacağı için bu her zaman doğrudur, ama savunmacı olarak da kontrol edilir).
    /// "Turda bir yetenek" kuralı burada uygulanır: netHasUsedSkill true iken HERHANGİ bir slot
    /// reddedilir — bu, ayrıca her silaha özel bir server-side cooldown zamanlayıcısı olmadan
    /// "aynı turda art arda farklı silahlarla ateşleme" açığını da kapatır.
    /// </summary>
    public bool ServerTryConsume(int slotIndex)
    {
        if (IsSpawned && !IsServer) return false;
        if (netHasUsedSkill.Value) return false;

        var s = netAmmo.Value;
        switch (slotIndex)
        {
            case 0: // Pistol
                if (s.pistolAmmo == 0) return false;
                if (s.pistolAmmo > 0) s.pistolAmmo--;
                break;
            case 1: // Shotgun
                if (s.shotgunAmmo <= 0) return false;
                s.shotgunAmmo--;
                break;
            case 2: // RPG
                if (s.rpgAmmo <= 0) return false;
                s.rpgAmmo--;
                break;
            case 3: // Grenade
                if (s.grenades <= 0) return false;
                s.grenades--;
                break;
            case 4: // Super Jump
                if (s.superJumps <= 0) return false;
                s.superJumps--;
                break;
            case 5: // Shield
                if (s.shields <= 0) return false;
                s.shields--;
                break;
            case 6: // Teleport — sınırsız
            case 7: // BatHammer — sınırsız
            case 8: // BlackHole — sınırsız
            case 9: // Bomb — sınırsız
                break;
            default:
                return false;
        }

        netAmmo.Value = s;
        netHasUsedSkill.Value = true;
        return true;
    }

    // --- Getter'lar (public API değişmedi — WeaponUIManager/SkillUIManager'da hiçbir değişiklik gerekmedi) ---
    public int GetSuperJumpsRemaining() => netAmmo.Value.superJumps;
    public int GetRpgAmmoRemaining()    => netAmmo.Value.rpgAmmo;
    public int GetPistolAmmo()          => netAmmo.Value.pistolAmmo;
    public int GetShotgunAmmo()         => netAmmo.Value.shotgunAmmo;
    public int GetGrenadesRemaining()   => netAmmo.Value.grenades;
    public int GetShieldsRemaining()    => netAmmo.Value.shields;

    public int GetSkillRemaining(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return netAmmo.Value.pistolAmmo;
            case 1: return netAmmo.Value.shotgunAmmo;
            case 2: return netAmmo.Value.rpgAmmo;
            case 3: return netAmmo.Value.grenades;
            case 4: return netAmmo.Value.superJumps;
            case 5: return netAmmo.Value.shields;
            case 6: return 1; // Teleport (sınırsız)
            case 7: return 1; // Bat/Hammer (sınırsız)
            case 8: return 1; // Black Hole (sınırsız)
            case 9: return 1; // Bomb (sınırsız)
            default: return 0;
        }
    }

    // --- Tek-seçim çekirdeği ---
    public void SelectSkill(int idx)
    {
        if (idx < 0 || idx >= abilitySlots.Length) return;

        // tümünü kapat + iptal
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            var ab = abilitySlots[i];
            if (ab == null) continue;
            if (i != idx)
            {
                ab.SetSelected(false);
                ab.Cancel();
            }
        }

        // yalnız seçileni aç
        var target = abilitySlots[idx];
        if (target != null)
        {
            target.SetSelected(true);
            UIManager.Instance?.HighlightSelected(idx);
        }
    }

    /// <summary>
    /// Klavyesiz (dokunmatik/UI) onay: seçili slotu Enter tuşuyla aynı şekilde onaylar.
    /// </summary>
    public void ConfirmSkill(int idx)
    {
        if (idx < 0 || idx >= abilitySlots.Length) return;
        abilitySlots[idx]?.Confirm();
    }

    public void DeselectAll()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i]?.SetSelected(false);
            abilitySlots[i]?.Cancel();
        }
        UIManager.Instance?.ClearAllSkillSelections();
        TurnManager.NotifyWeaponCancelled();
    }

    /// <summary>
    /// ✨ DEĞİŞİKLİK: Tüm skiller için cooldown sıfırlama (opsiyonel destekleyenler)
    /// </summary>
    public void ResetAllCooldowns()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            var ab = abilitySlots[i] as ICooldownResettable;
            ab?.ResetCooldown();
        }
    }

    /// <summary>
    /// ✨ DEĞİŞİKLİK: Turn başında tek noktadan reset (flag + seçim + cooldown). TurnManager.
    /// ActivateCharacter() içinden çağrılır — bu metod her zaman ya offline (tek makine) ya da
    /// online'da yalnızca server tarafında (TurnManager'ın kendi IsServer gate'i) çalışır, bu
    /// yüzden netHasUsedSkill.Value'yu burada koşulsuz sıfırlamak güvenlidir.
    /// </summary>
    public void ResetTurnState()
    {
        if (!IsSpawned || IsServer)
            netHasUsedSkill.Value = false;

        // seçimleri temizle
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i]?.SetSelected(false);
            abilitySlots[i]?.Cancel();
        }
        // cooldown'ları sıfırla
        ResetAllCooldowns();
    }

    /// <summary>
    /// Başarılı aktivasyondan sonra ability'ler tarafından çağrılır: turn kullanımını işaretler ve UI'ı kilitler.
    /// Offline'da doğrudan; online'da gerçek işaretleme zaten ServerTryConsume() ile server'da
    /// yapılmış olur (bu çağrı orada no-op'a düşer) — yalnızca UI kilidi için tutuluyor.
    /// </summary>
    public void OnAbilityConsumed()
    {
        if (!IsSpawned) netHasUsedSkill.Value = true;
        UIManager.Instance?.LockAllSkillsUI();
    }
}
