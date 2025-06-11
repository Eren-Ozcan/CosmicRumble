// Assets/Scripts/Managers/TurnManager.cs
using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [Header("Sıra Tabanlı Oynanacak Karakterler")]
    [Tooltip("GravityBody içeren karakter objelerini buraya atayın.")]
    public List<GravityBody> characters;

    [Tooltip("Sıra değişim tuşu")]
    public KeyCode nextTurnKey = KeyCode.Tab;

    private int currentIndex = 0;

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
        if (Input.GetKeyDown(nextTurnKey) && characters.Count > 1)
        {
            ActivateCharacter((currentIndex + 1) % characters.Count);
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
        }

        // 2) Yeni karakteri aktif et
        currentIndex = newIndex;
        GravityBody newGb = characters[currentIndex];
        if (newGb != null)
        {
            newGb.isActive = true;
            newGb.OnTurnStart();
            Debug.Log($"[TurnManager] Yeni aktif karakter: {newGb.gameObject.name}");

            // → UI güncellemesi için CharacterAbilities bileşenini UIManager'a bildir
            var abilities = newGb.GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                UIManager.Instance.SetCharacter(abilities);
            }
        }
    }
}
