using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
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
        // Önceki karakteri pasif yap
        GravityBody oldGb = characters[currentIndex];
        if (oldGb != null)
        {
            oldGb.isActive = false;
            oldGb.ZeroHorizontalVelocity();
        }

        // Yeni karakteri aktif yap
        currentIndex = newIndex;
        GravityBody newGb = characters[currentIndex];
        if (newGb != null)
        {
            newGb.isActive = true;
            newGb.OnTurnStart();

            // Abilities ve UI sıfırlamaları
            var abilities = newGb.GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                UIManager.Instance.SetCharacter(abilities);
                abilities.HasUsedSkillThisTurn = false;             // 🔧 TUR BAŞINDA SIFIRLAMA
                UIManager.Instance.ClearAllSkillFilters();
            }

            var superJump = newGb.GetComponent<SuperJumpSkill>();
            if (superJump != null)
            {
                superJump.IsSelected = true;
                superJump.ResetCooldown();
            }
        }

        var skills = newGb.GetComponents<MonoBehaviour>();
        foreach (var skill in skills)
        {
            var method = skill.GetType().GetMethod("ResetCooldown");
            if (method != null)
                method.Invoke(skill, null);
        }

        turnTimer = turnDuration;
        TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);
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
