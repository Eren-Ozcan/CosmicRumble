// Assets/Scripts/Weapons/WeaponManager.cs
using UnityEngine;

/// <summary>
/// WeaponManager:
/// - Farklı silah tipleri (Pistol, Shotgun, RPG) için atış mantığını tutar.
/// - Input geldiğinde ilgili Projectile prefab’ını instantiate eder, fireRate ve ammo takibi yapar.
/// - CharacterAbilities aracılığıyla ammo kontrolü sağlar.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public enum WeaponType { Pistol, Shotgun, RPG }

    [Header("Silah Prefab'ları")]
    [Tooltip("Pistol mermisi prefab'ı")]
    public GameObject pistolPrefab;
    [Tooltip("Shotgun kovanı prefab'ı (her tane birer Projectile olabilir)")]
    public GameObject shotgunPrefab;
    [Tooltip("RPG roketi prefab'ı")]
    public GameObject rpgPrefab;

    [Header("Fire Point Transforms")]
    [Tooltip("Pistol mermisinin spawn noktası")]
    public Transform pistolFirePoint;
    [Tooltip("Shotgun mermilerinin spawn noktaları (birden çok olabilir)")]
    public Transform[] shotgunFirePoints;
    [Tooltip("RPG roketinin spawn noktası")]
    public Transform rpgFirePoint;

    [Header("Fire Rate (saniyede bir atış için bekleme)")]
    public float pistolFireRate = 0.2f;
    public float shotgunFireRate = 1f;
    public float rpgFireRate = 1.5f;

    private float pistolTimer = 0f;
    private float shotgunTimer = 0f;
    private float rpgTimer = 0f;

    private CharacterAbilities abilities;

    private void Awake()
    {
        abilities = GetComponent<CharacterAbilities>();
        if (abilities == null)
            Debug.LogWarning("[WeaponManager] " + name + " üzerinde CharacterAbilities bulunamadı!");
    }

    private void Update()
    {
        // Zamanlayıcıları azalt
        pistolTimer -= Time.deltaTime;
        shotgunTimer -= Time.deltaTime;
        rpgTimer -= Time.deltaTime;

        // 1) Pistol: Mouse0 basılıyken (otomatik ateş)
        if (Input.GetButton("Fire1") && pistolTimer <= 0f)
        {
            TryFire(WeaponType.Pistol);
        }

        // 2) Shotgun: Q tuşu
        if (Input.GetKeyDown(KeyCode.Q) && shotgunTimer <= 0f)
        {
            TryFire(WeaponType.Shotgun);
        }

        // 3) RPG: E tuşu
        if (Input.GetKeyDown(KeyCode.E) && rpgTimer <= 0f)
        {
            TryFire(WeaponType.RPG);
        }
    }

    private void TryFire(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol:
                if (abilities == null || !abilities.UsePistol())
                    return;
                FirePistol();
                pistolTimer = pistolFireRate;
                break;

            case WeaponType.Shotgun:
                FireShotgun();
                shotgunTimer = shotgunFireRate;
                break;

            case WeaponType.RPG:
                if (abilities == null || !abilities.UseRpg())
                    return;
                FireRPG();
                rpgTimer = rpgFireRate;
                break;
        }
    }

    private void FirePistol()
    {
        if (pistolPrefab == null || pistolFirePoint == null)
            return;

        Instantiate(pistolPrefab, pistolFirePoint.position, pistolFirePoint.rotation);
        int remaining = (abilities != null) ? abilities.GetPistolAmmo() : -1;
        string ammoText = (remaining < 0) ? "∞" : remaining.ToString();
        Debug.Log("[WeaponManager] Pistol fired. Kalan ammo: " + ammoText);
    }

    private void FireShotgun()
    {
        if (shotgunPrefab == null || shotgunFirePoints == null || shotgunFirePoints.Length == 0)
            return;

        foreach (var fp in shotgunFirePoints)
        {
            Instantiate(shotgunPrefab, fp.position, fp.rotation);
        }
        Debug.Log("[WeaponManager] Shotgun fired.");
    }

    private void FireRPG()
    {
        if (rpgPrefab == null || rpgFirePoint == null)
            return;

        Instantiate(rpgPrefab, rpgFirePoint.position, rpgFirePoint.rotation);
        int remaining = abilities.GetRpgAmmoRemaining();
        Debug.Log("[WeaponManager] RPG fired. Kalan ammo: " + remaining);
    }
}
