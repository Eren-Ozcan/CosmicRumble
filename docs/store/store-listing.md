# CosmicRumble — store listing copy

Draft 2026-09-06, written from what the game actually contains. Graphics (icon, feature graphic,
screenshots) are **not** in this repo — see this repo's CLAUDE.md and the private pictures repo.

Play limits: app name 30 chars, short description 80 chars, full description 4000 chars.

## English (default locale)

**App name**

```
Cosmic Rumble
```

**Short description** (77 chars)

```
Turn-based artillery duels on tiny planets. Aim around gravity. Outlast them all.
```

**Full description**

```
Every planet has its own gravity — and every shot has to travel through it.

Cosmic Rumble is a turn-based artillery game played across a cluster of small worlds. Walk all the
way around a planet, line up a shot, and watch it curve. A rocket that misses can loop back around
and hit the shooter. Ground that takes a direct hit stops being ground.

TAKE YOUR TURN
Nine weapons and abilities, each with its own reason to exist: pistol, shotgun, RPG, grenade, bomb,
bat hammer, teleport, shield, and a black hole that rewrites the battlefield for everyone. Aim with
a drag, watch the trajectory bend, and commit.

PLAY WITH OTHER PEOPLE
Real-time online matches for 2 to 8 players: 1v1 duels, free-for-all, or team battles up to 4v4
and 2v2v2v2. Play with a friend code, or let Quick Match find you an opponent. If the host drops
mid-match, the match keeps going on someone else's device.

DESTRUCTIBLE WORLDS
Terrain is carved away by every explosion, and it stays carved. The safe ledge you were standing
behind three turns ago may not exist any more.

PROGRESS THAT STAYS
Trophies, a leaderboard, daily quests, chests, achievements and costumes — all backed up to the
cloud, so they follow you to a new phone.

PLAY YOUR WAY
Practise offline against training dummies, or pass one device around in hotseat mode. Seven
languages. No ads.
```

## Türkçe

**Uygulama adı**

```
Cosmic Rumble
```

**Kısa açıklama** (72 karakter)

```
Küçük gezegenlerde sıra tabanlı topçu düellosu. Yerçekimini hesaba kat.
```

**Tam açıklama**

```
Her gezegenin kendi yerçekimi var — ve attığın her mermi onun içinden geçiyor.

Cosmic Rumble, birbirine yakın küçük dünyalarda oynanan sıra tabanlı bir topçu oyunu. Gezegenin
etrafında tam tur at, nişan al ve merminin nasıl kıvrıldığını izle. Iskalayan bir roket dönüp
atanın kendisini vurabilir. Tam isabet alan zemin artık zemin değildir.

SIRA SENDE
Dokuz silah ve yetenek, her biri ayrı bir amaç için: tabanca, pompalı, RPG, el bombası, bomba,
sopa-çekiç, ışınlanma, kalkan ve herkesin sahasını yeniden çizen bir kara delik. Sürükleyerek nişan
al, yörüngenin büküldüğünü gör ve ateşle.

BAŞKALARIYLA OYNA
2-8 oyuncu için gerçek zamanlı çevrimiçi maçlar: 1v1 düello, herkes kendine ya da 4v4 ve 2v2v2v2'ye
kadar takım savaşları. Arkadaş koduyla oyna ya da Hızlı Maç sana rakip bulsun. Maçın ortasında host
düşerse maç bir başkasının cihazında kaldığı yerden devam eder.

YIKILAN DÜNYALAR
Her patlama zemini oyar ve o oyuk kalıcıdır. Üç tur önce arkasına saklandığın çıkıntı artık orada
olmayabilir.

KALICI İLERLEME
Kupalar, lider tablosu, günlük görevler, sandıklar, başarımlar ve kostümler — hepsi buluta
yedeklenir, yeni telefonuna seninle gelir.

NASIL İSTERSEN
Çevrimdışı antrenman modunda çalış ya da tek cihazı sırayla paylaşarak oyna. Yedi dil. Reklam yok.
```

## Notes for whoever fills the Console form

- "No ads" is a factual claim: no ad SDK is integrated. If that ever changes, this copy and the ads
  declaration have to change together.
- The host-migration sentence is now true and tested — it was not before 2026-09-06.
- 2v2v2v2 exists in `GameModeType`; keep the listed modes in sync with `GameModeDefinition.cs`.
- The other five languages the game ships in (ZH, ES, JA, KO, DE) are worth adding as Console
  locales later; the game itself is already translated.
