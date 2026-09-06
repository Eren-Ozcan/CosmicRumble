# CosmicRumble — Data Safety declaration (source of truth for the Play Console form)

Written 2026-09-06 from the code, not from memory. Fill the Console form from this table; if the
code changes, change this file in the same commit.

Studio-wide context (accounts, the three shared URLs, the three traps this form has sprung before)
lives in `C:\Projects\pictures\STUDIO.md` — read that first, it is not repeated here.

## What the app actually does

| System | Package | What leaves the device |
|---|---|---|
| Anonymous / Play Games sign-in | `com.unity.services.authentication` | UGS player ID (an account identifier), and the Play Games account when the player signs in with it |
| Cloud save | `com.unity.services.cloudsave` | Progress: currency, level/XP, costumes, avatar, achievements, quests, login streak |
| Multiplayer sessions | `com.unity.services.multiplayer` | Session membership, display name, in-match state while a match runs |
| Friends / presence | `com.unity.services.friends` | Display name (the friend code is the UGS player name, e.g. `Nova731#1234`), online/away status, and a coarse activity string — `in_match` or `in_menu` |
| Analytics | `com.unity.services.analytics` | Automatic session/engagement events plus one custom `match_completed` event (won, ranked) |
| Crash reporting | `com.unity.services.cloud-diagnostics` | Crash and exception reports |
| Purchases | Unity IAP / Play Billing | Purchase of the five `gem_pack_*` consumables |
| Local notifications | `com.unity.mobile.notifications` | **Nothing** — scheduled on the device, never sent to a server |

**No advertising SDK is integrated** (no AdMob, no GoogleMobileAds, no Unity Ads anywhere in
`Assets/Scripts`), so there is no advertising ID to declare and no ads-related consent flow.

## How that maps onto the form

| Data type | Collected | Shared | Purpose | Optional? |
|---|---|---|---|---|
| Personal info → **User IDs** | Yes | No | App functionality, account management | Required |
| Personal info → **Name** (display name / friend code) | Yes | No | App functionality (multiplayer, friends) | Required |
| **App activity → In-app actions** (match results, progression) | Yes | No | App functionality, analytics | Required |
| **App info and performance → Crash logs** | Yes | No | Diagnostics | Required |
| **App info and performance → Diagnostics** | Yes | No | Analytics, diagnostics | Required |
| **Financial info → Purchase history** | Yes | No | App functionality | Required |
| Device or other IDs | **No** | — | — | — |
| Location, contacts, photos, messages, health, browsing | **No** | — | — | — |

Traps, all three of which have bitten this studio before (see STUDIO.md):

- The anonymous UGS/Firebase-style auth id belongs under **Personal info → User IDs**, not under
  *Device or other identifiers* — Play's device-identifier category means the device or browser, and
  an auth UID is an account identifier.
- **Check the "Completed" tab before filling anything in.** A stale-but-present declaration is more
  dangerous than a missing one.
- Answering **OAuth** on the account-creation question forces an account-deletion URL, which is why
  the studio has `account-deletion` as a real page rather than pointing at the privacy policy.

## URLs (the studio uses one set for every game — do not create game-specific pages)

| Console field | URL |
|---|---|
| Privacy policy | `https://yilkgames.com/privacy-policy/` |
| Account deletion | `https://yilkgames.com/account-deletion/` |
| Data deletion | `https://yilkgames.com/account-deletion/#data-only` |

**Before pointing Play at those URLs**, the studio privacy page has to describe what CosmicRumble
collects — friends/display name and purchases in particular. Otherwise the app is bound to a policy
that does not describe its own data. The site does not deploy on push:
`npx wrangler pages deploy . --project-name=yilkgames-web` from the `yilkgames_web` repo.

## Consent note for analytics

`AnalyticsManager` calls `StartDataCollection()`, which the SDK documents as asserting that consent
has been obtained or is not required. That is fine for internal testing, but the privacy policy has
to cover analytics before a build with it goes to real users (roadmap item 7/19).
