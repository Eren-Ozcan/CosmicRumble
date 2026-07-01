CosmicRumble için /analyze — .claude/commands/analyze.md
---
Belirtilen sistemi derinlemesine analiz et.

Adımlar:
1. CLAUDE.md'yi oku
2. İlgili .cs dosyalarını bul ve oku
3. Veri akışını haritala
4. Sorunları tespit et (fizik, mantık, perf)
5. Somut iyileştirme öner — kod örneğiyle

Kullanım:
  /analyze gravity       → GravityBody/Source/Manager analizi
  /analyze turn          → TurnManager + CharacterAbilities
  /analyze trajectory    → TrajectoryPredictor doğrulama
  /analyze [DosyaAdi]    → Spesifik dosya analizi
