# CosmicRumble — Play Console setup, step by step

Written 2026-09-06. The developer account, payment profile and Android developer verification are
already done at the studio level — see `C:\Projects\pictures\STUDIO.md`. Nothing here creates an
account; this is only the new app listing for CosmicRumble.

**Open the Console at `https://play.google.com/console?authuser=yilkgamesstudio@gmail.com`.** If a
"create a developer account" or signup screen appears, you are in the wrong Google account — back
out, do not fill that form.

## Facts this app needs

| Field | Value |
|---|---|
| App name | Cosmic Rumble |
| Package | `com.yilkgames.cosmicrumble` |
| Upload key | `pictures/CosmicRumble/android-keystore/` (private repo) |
| Category | Games → Action (or Arcade) |
| Target audience | 13+ (same answer the other three games used) |
| Privacy policy | `https://yilkgames.com/privacy-policy/` |
| Account deletion | `https://yilkgames.com/account-deletion/` |
| Data deletion | `https://yilkgames.com/account-deletion/#data-only` |
| Contains ads | **No** — no ad SDK is integrated |
| In-app purchases | Yes — five consumables, `gem_pack_100/550/1200/2500/6000` |

## Order of operations

1. **Create the app** (Console → All apps → Create app). Name, default language, Game, Free.
2. **Set up the closed testing track first.** The 12-testers-for-14-days clock only starts once a
   build is live on a closed track, and it gates production access — everything else can be done
   while it runs. Upload the AAB from `Builds/Android/`, pick the existing tester group.
3. **App content forms**: privacy policy URL, ads declaration (no ads), content rating
   questionnaire, target audience, data safety, government-app and financial-features declarations.
   Data safety answers: `docs/store/data-safety.md` in this repo — do not re-derive them.
4. **In-app products**: create the five consumables with the exact IDs above. Prices:
   `docs/store/iap-pricing.md`. A product ID cannot be renamed or reused later.
5. **Play Games Services**: create the game project, link the app, then paste the resulting
   Application ID and Web client ID into the Unity project (Window → Google Play Games → Setup).
   `Assets/GooglePlayGames/.../GameInfo.cs` currently has both empty, which is why sign-in cannot be
   tested on a device yet.
6. **Achievements**: create the 50 achievements, then copy each generated `CgkI...` id into the
   matching `AchievementDefinition.googlePlayId` field in the Unity Inspector. Code side is done;
   this is data entry only.
7. **Store listing**: icon, feature graphic, screenshots, short and full description. Assets live in
   the private pictures repo, never in this one (see this repo's CLAUDE.md).

## Before the first upload

- `versionCode` must increase with every upload. It is `PlayerSettings.Android.bundleVersionCode`;
  the build tool prints it and names the output file after it.
- Build the AAB with **Tools → Android → Build Signed AAB**, not from the File menu — the tool sets
  the package name, IL2CPP, ARM64 and the upload key, and those are the settings the Console
  permanently associates with the app.
- Play App Signing will ask to accept a Google-managed signing key on the first upload. Accept it;
  the keystore in the private repo then remains only the *upload* key.
