# CosmicRumble

## Proje
2D çok gezegenli sıra tabanlı dövüş oyunu (Worms + Crazy Planets tarzı).

- Engine: Unity 2D, C#
- Fizik: Custom gravity — Unity'nin default gravity'si (`Physics2D.gravity`) KAPALI
- Karakter: 360° yüzey hareketi, vektörel çekim
- Silahlar/Yetenekler: Pistol, RPG, Shotgun, Grenade, BlackHole, Teleport, Shield, BatHammer, Bomb
- Multiplayer: henüz entegre edilmedi — önerilen yaklaşım için `TODO.md`'ye bak

## Klasör Yapısı
```
Assets/Scripts/
├── Gravity/      → GravitySource, GravityBody, GravityManager
├── Character/    → PlayerController2D, CharacterHealth, CharacterAbilities
├── Abilities/    → IAbility + tüm yetenekler
├── Projectile/   → ProjectileBase hiyerarşisi, TrajectoryPredictor
├── Managers/     → TurnManager, UIManager
├── Planet/       → DestructiblePlanet, BombExplosion
└── UI/           → HealthBarUI, TurnTimerUI, ToggleSkillPanel
```

## Kritik Kurallar (İhlal Edilemez)
1. Unity default gravity (`Physics2D.gravity`) asla açılmaz.
2. Velocity direkt set edilmez — `AddForce` kullan.
3. Her yetenek `IAbility` implement etmeli.
4. `FixedUpdate` → fizik, `Update` → input/UI.
5. `TurnManager` onayı olmadan aksiyon alınamaz.
6. Test geçmeden "bitti" deme.

## Slash Komutları
- `/analyze [sistem|dosya]` — belirtilen sistemi (gravity, turn, trajectory ...) veya dosyayı derinlemesine analiz eder, veri akışını haritalar, iyileştirme önerir.
- `/review [dosya]` — fizik, mimari, performans, Unity kullanımı ve gameplay açısından 5 lensli kod incelemesi yapar.
- `/optimize [dosya|sistem]` — performans (FPS, GC, object pool) ve kod kalitesi (SOLID, refactor fırsatları) analizini P0/P1/P2 önceliğiyle raporlar.
- `/commit` — staged değişiklikleri analiz edip semantic commit mesajı önerir.

## Backlog
Ertelenen işler (kostümler, quest içeriği, ses içeriği, multiplayer, tam tuş rebinding, cloud save vb.) için `TODO.md`'ye bak.

## Commit Alışkanlığı
Commit atmak önemli — kullanıcı GitHub profilinin aktif/kalabalık görünmesini istiyor. Buna göre:
- Anlamlı her adımdan sonra ayrı ayrı commit at (tek dev seansında birden fazla iş kalemi varsa,
  her kalemi bitirince kendi commit'ini at — hepsini sona biriktirip tek dev commit yapma).
- Küçük, bağımsız bir düzeltme/iyileştirme fark edersen bile ayrı bir commit olarak kaydet;
  büyük bir işin içine gömüp kaybetme.
- Yine de her commit gerçek, çalışan bir durumu temsil etmeli (derleme temiz, mümkünse test/play-test
  edilmiş) — sık commit "yarım kalmış/bozuk kod commit et" anlamına gelmiyor.
- Push'u da unutma — yalnızca local commit profilde görünmez.
