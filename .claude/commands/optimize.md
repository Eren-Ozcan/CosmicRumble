CosmicRumble için /optimize — .claude/commands/optimize.md
---
Belirtilen dosya veya sistemin performans ve kalite analizini yap.

Adımlar:
1. Performans taraması: FPS maliyeti, GC allocation, object pool fırsatı
2. Kalite taraması: SOLID ihlali, refactor fırsatı, P0/P1/P2 önceliklendirme
3. Öncelikleri birleştir — hangisi önce yapılmalı?
4. Onay al
5. Uygula → değişiklikleri review et → test et

Kullanım:
  /optimize                      → tüm Assets/Scripts
  /optimize GravityBody.cs       → spesifik dosya
  /optimize gravity              → gravity sistemi
  /optimize --perf-only          → sadece performans
  /optimize --quality-only       → sadece kod kalitesi

Çıktı: Öncelikli iyileştirme listesi (P0/P1/P2) + tahmini kazanım
