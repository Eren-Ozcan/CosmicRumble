// TurnManager.cs
using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [Header("Karakterler (GravityBody)")]
    public List<GravityBody> characters;
    public KeyCode nextTurnKey = KeyCode.Tab;

    int currentIndex = 0;

    void Start()
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("TurnManager: Karakter yok!");
            return;
        }

        // Karakterler arası collider çarpışmalarını atla
        for (int i = 0; i < characters.Count; i++)
        {
            for (int j = i + 1; j < characters.Count; j++)
            {
                var colsA = characters[i].GetComponents<Collider2D>();
                var colsB = characters[j].GetComponents<Collider2D>();
                foreach (var a in colsA)
                    foreach (var b in colsB)
                        Physics2D.IgnoreCollision(a, b, true);
            }
        }

        ActivateCharacter(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(nextTurnKey) && characters.Count > 1)
        {
            ActivateCharacter((currentIndex + 1) % characters.Count);
        }
    }

    void ActivateCharacter(int idx)
    {
        // 1) Eski karakterin yatay hızı sıfırla
        var old = characters[currentIndex];
        old.isActive = false;
        old.ZeroHorizontalVelocity();

        // 2) Yeni karakteri aktif et
        currentIndex = idx;
        var now = characters[currentIndex];
        now.isActive = true;
        now.OnTurnStart();
        Debug.Log($"TurnManager: Aktif karakter = {now.name}");
    }
}
