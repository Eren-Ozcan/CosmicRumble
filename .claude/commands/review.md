CosmicRumble için /review — .claude/commands/review.md
---
Review the specified file or recent changes with 5 lenses:

1. PHYSICS: Physics2D.gravity kapalı mı? velocity direkt set var mı? AddForce kullanılıyor mu?
2. ARCHITECTURE: IAbility implement edilmiş mi? Single responsibility var mı? Interface'ler korunuyor mu?
3. PERFORMANCE: Update içinde FindObjectOfType var mı? Event leak var mı? Gereksiz allocation?
4. UNITY: FixedUpdate/Update ayrımı doğru mu? SerializeField parametreler var mı?
5. GAMEPLAY: TurnManager senkronizasyonu doğru mu? Ability state temizleniyor mu?

Her sorun için: Dosya.cs:satır → sorun → neden yanlış → nasıl düzeltilir
