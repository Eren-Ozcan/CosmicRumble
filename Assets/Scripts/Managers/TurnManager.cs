using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.TextCore.Text;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Sıra Tabanlı Oynanacak Karakterler")]
    [Tooltip("GravityBody içeren karakter objelerini buraya atayın.")]
    public List<GravityBody> characters;

    [Tooltip("Sıra değişim tuşu")]
    public KeyCode nextTurnKey = KeyCode.Tab;

    [Header("Tur Süresi Ayarları")]
    [Tooltip("Her karakterin tur süresi (saniye cinsinden)")]
    public float turnDuration = 15f;

    private int currentIndex = 0;
    private float turnTimer = 0f;
    private bool inputLocked = false;
    public bool InputLocked => inputLocked;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("[TurnManager] characters listesi boş!");
            return;
        }

        // İlk karakteri aktif et
        ActivateCharacter(0);
    }

    private void Update()
    {
        if (characters.Count < 2) return;

        // Manuel geçiş
        if (Input.GetKeyDown(nextTurnKey))
        {
            NextTurn();
        }

        // Otomatik zamanlayıcı
        if (turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;

            // ⏱ Radial UI güncellemesi
            TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);

            if (turnTimer <= 0f)
            {
                NextTurn();
            }
        }
    }

    /// <summary>
    /// Belirlenen indeksteki karakteri aktif yap, eski karakteri pasif hale getir.
    /// Ayrıca UIManager'a yeni karakterin abilities bileşenini bildirir.
    /// </summary>
    /// <param name="newIndex">Yeni aktif karakter indeksi</param>
    private void ActivateCharacter(int newIndex)
    {
        // 1) Önceki karakteri pasif hale getir
        GravityBody oldGb = characters[currentIndex];
        if (oldGb != null)
        {
            oldGb.isActive = false;
            oldGb.ZeroHorizontalVelocity();
            StartCoroutine(IgnoreMovementInputBriefly());
        }

        // 2) Yeni karakteri aktif et
        currentIndex = newIndex;
        GravityBody newGb = characters[currentIndex];
        if (newGb != null)
        {
            newGb.isActive = true;
            newGb.OnTurnStart();

            // UIManager’a bağlı abilities güncelle
            var abilities = newGb.GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                UIManager.Instance.SetCharacter(abilities);              // ✅ UI’ı bu karaktere bağla
                abilities.HasUsedSkillThisTurn = false;                   // ✅ skill hakkını yenile (UI otomatik açılır)
            }

            // ✅ SuperJump UI sistemi için aktif et
            var superJump = newGb.GetComponent<SuperJumpSkill>();
            if (superJump != null)
            {
                superJump.IsSelected = true;
                superJump.ResetCooldown(); // cooldown sıfırlansın
            }
        }

        // Yeni turn süresi başlat
        turnTimer = turnDuration;

        // ⏱ UI başlatma (ilk dolu gösterim)
        TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);
    }

    private IEnumerator IgnoreMovementInputBriefly()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (var pi in UnityEngine.Object.FindObjectsOfType<PlayerInput>())
        {
            var map = pi.currentActionMap;
            if (map != null)
            {
                map.Disable();
                map.Enable();
            }
        }
#endif
        inputLocked = true; // prevent movement input for a short window
        float timer = 0.1f;
        while (timer > 0f)
        {
            Input.ResetInputAxes(); // clear held keys
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }
        inputLocked = false;
    }

    /// <summary>
    /// Sıradaki karaktere geç.
    /// </summary>
    private void NextTurn()
    {
        int nextIndex = (currentIndex + 1) % characters.Count;
        ActivateCharacter(nextIndex);
    }
}
