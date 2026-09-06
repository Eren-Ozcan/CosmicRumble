# CosmicRumble — Gem pack pricing (proposal, 2026-09-06)

The five SKUs already exist in code (`IAPManager.cs`) and the shop UI already flags
`gem_pack_1200` as "Popular" and `gem_pack_6000` as "Best value". Only the prices were open.
This is the proposal to enter in the Play Console; **it is a business decision, so treat the
table as a draft until the account owner confirms it.**

## What gems actually buy today

| Sink | Cost |
|---|---|
| Epic chest | 25 gems |
| Elite costume c2_3 | 50 gems |
| Elite costume c4_3 | 80 gems |

Total gem-priced content in the game right now: 130 gems of costumes plus a repeatable
25-gem chest. The entry pack therefore has to cover a meaningful purchase on its own, and the
large packs have to feel like stocking up rather than like buying an unspendable pile.

## Proposed ladder

Standard charm-priced tiers, with gems-per-dollar rising monotonically so that every step up is
visibly better value — the ladder is the offer, not the individual price.

| SKU | Gems | USD | Gems per $ | Extra vs. the entry pack |
|---|---|---|---|---|
| `gem_pack_100` | 100 | $0.99 | 101 | — |
| `gem_pack_550` | 550 | $4.99 | 110 | +9% |
| `gem_pack_1200` | 1200 | $9.99 | 120 | +19% ("Popular") |
| `gem_pack_2500` | 2500 | $19.99 | 125 | +24% |
| `gem_pack_6000` | 6000 | $39.99 | 150 | +49% ("Best value") |

Note the top pack is **$39.99, not $49.99**. At $49.99 it would give 120 gems/$, i.e. worse value
than the $19.99 pack, which breaks the ladder and makes the "Best value" badge a lie.

Rationale for the shape: 3-5 tiers spanning casual through whale is the standard recommendation,
and prices ending in .99 are the standard charm-pricing convention for impulse purchases. No tier
above $39.99 is proposed for launch — a $99.99 whale tier is worth adding only once there is
enough gem-priced content to justify it (today 6000 gems is already ~46 epic chests).

## Other markets

Set USD as the base price and let Play's automatic conversion fill the rest, then review Turkey
and any other market where the converted price lands on an odd number, since local charm prices
(e.g. ₺X,99) matter as much there as they do in USD. Play recalculates automatically converted
prices as exchange rates move, so this stays approximately right without maintenance.

## When entering these in the Console

- The product IDs must match `IAPManager.cs` **exactly**: `gem_pack_100`, `gem_pack_550`,
  `gem_pack_1200`, `gem_pack_2500`, `gem_pack_6000`. A typo here is not fixable later — Play does
  not allow reusing or renaming a product ID.
- All five are **consumable** managed products (gems are spent), not subscriptions.
- Titles/descriptions come from the same definitions ("Small/Medium/Large/Mega/Ultimate Gem Pack").
- Real purchases can only be tested from the closed track with a licensed test account
  (STORE-02 in `docs/TEST_PLAN.md`).
