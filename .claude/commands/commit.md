CosmicRumble için /commit — .claude/commands/commit.md
---
Staged değişiklikleri analiz et ve semantic commit mesajı oluştur.

Format: <type>(<scope>): <description>

Types: feat, fix, refactor, perf, test, docs
Scopes: gravity, character, ability, projectile, ui, turn, planet, manager

Örnekler:
  feat(gravity): add multi-planet vectoral force summation
  fix(turn): clear ability state on turn end
  refactor(ability): extract IAbility base implementation

Commit öncesi kontrol:
  - Physics2D.gravity kullanımı var mı?
  - velocity direkt set var mı?
  - Test geçti mi?

Temizse: git commit -m "[mesaj]"
