# Floor Element System — Design Doc

Status: **design only** (not implemented in code yet)  
Last updated: 2026-09-13

## Goal

Give each dungeon floor a distinct identity without a heavy RPG type chart. Elements change **floor rules** and **throwable affinity**, not a 5×5 matchup matrix.

Integrates with existing systems:

- `WindManager` — wind force/direction per turn
- `ThrowManager` — charge speed, trajectory, damage
- `LevelProgression` — per-level tunables
- `ItemManager` — one-shot skills + future roguelike picks
- `AdManager` / `CurrencyWallet` — monetization hooks

---

## Core Rules

### Four elements

| Element | Floor environment | Enemy passive | Strong throwables (+25% dmg) |
|---------|-------------------|---------------|------------------------------|
| **Fire** | Wind +20%, charge faster | Head hit → burn 2 dmg/turn for 2 turns | Coal, torch, oil jar |
| **Ice** | Wind −30%, charge slower | Body hit → next charge −30% speed | Ice chunk, frozen item |
| **Thunder** | Wind flips every 2 turns | Head hit → skip enemy turn once per match | Metal, chain, bottle |
| **Poison** | No wind, trajectory −10% range | No hit for 1 turn → enemy heals 3 HP | Poison bottle, frog, mushroom |

**Neutral throwables** (coin, crate, barrel): normal damage on all floors.  
**Opposing element**: −25% damage (never blocks the throw).

### Affinity math

```
finalDamage = baseDamage * affinityMultiplier
affinityMultiplier = 1.25 (match) | 1.0 (neutral) | 0.75 (oppose)
```

Base damage unchanged: body 4, head 6, power 8, double 3×2.

---

## Roguelike Run Flow

```
Start run → pick 2 starter throwables (free)
  ↓
Floor 1–20 (element + wind + enemy)
  ↓
Win → choose 1 of 3 (throwable / charm / instant heal)
  ↓
Lose → Revive ad (existing) or end run → coins by floors cleared
```

**Loadout limits (per run):**

- Throwables in pool: max 4 types
- Charms (passive): max 2

### Pick types (after each floor win)

1. **Throwable** — add or upgrade element-tagged projectile in pool
2. **Charm** — small passive for rest of run (e.g. +5% dmg vs current element)
3. **Instant** — heal 15% max HP or restore one skill item (Power / Double)

Pick options bias toward **next floor’s element** (shown in UI preview).

---

## 20-Floor Table

| Floor | Element | Enemy | Enemy HP* | Notes |
|-------|---------|-------|-----------|-------|
| 1 | — (tutorial) | Skeleton | 24 | No element, light wind |
| 2 | — | Skeleton | 26 | Teach wind |
| 3 | Fire | Skeleton | 28 | First element intro |
| 4 | Ice | Skeleton Warrior | 28 | Slow charge |
| 5 | **Elite Thunder** | Skeleton Mage | 32 | Stronger stun passive |
| 6 | Poison | Skeleton | 30 | No wind |
| 7 | Fire | Skeleton Warrior | 30 | High wind |
| 8 | Ice | Skeleton | 32 | |
| 9 | Thunder | Skeleton Warrior | 32 | Wind flip |
| 10 | **Mini-boss Poison** | Skeleton Mage | 36 | Regen pressure |
| 11 | Fire | Skeleton | 34 | |
| 12 | Ice | Skeleton Warrior | 34 | |
| 13 | Thunder | Skeleton | 36 | |
| 14 | Poison | Skeleton Warrior | 36 | |
| 15 | **Elite Fire** | Skeleton Mage | 40 | Burn on head |
| 16 | Ice | Skeleton | 38 | |
| 17 | Thunder | Skeleton Warrior | 40 | |
| 18 | Poison | Skeleton | 40 | |
| 19 | Fire + high wind | Skeleton Mage | 42 | Pre-boss |
| 20 | **Boss (rotating)** | Skeleton King | 48 | Passive cycles every 3 turns |

\*Player HP baseline **28** (see HP balance below). Elite/boss use same prefabs + aura VFX + tuned passive.

---

## HP Balance (implemented)

Tuned for ~6–7 body hits or ~4–5 head hits per fight at level 1.

| Setting | Value |
|---------|-------|
| Player HP | 28 |
| Enemy HP (easy / normal / hard) | 28 / 32 / 36 |
| Level curve enemy HP | 28 → 40 (floors 1–20) |
| Heal item | 10 (~36% max HP) |
| Body / head / power / double dmg | 4 / 6 / 8 / 3×2 |

Sources: `GameConfig`, `ThrowingGameData.json`, `LevelProgression`, `GameManager`.

---

## Code Integration Plan

### Phase 1 — Data + UI (no combat change)

- Add `FloorElement` enum: `None, Fire, Ice, Thunder, Poison`
- Extend `LevelConfig` with `element`, `isElite`, `enemyArchetype`
- Replace pure curve in `LevelProgression.Generate()` with table lookup (keep curve as fallback)
- Floor start UI: element icon + one-line rule tooltip

### Phase 2 — Environment

- `WindManager`: read floor element → adjust `maxForce`, flip interval
- `ThrowManager.ConfigureFromLevel`: element → `chargeSpeed`, trajectory scale

### Phase 3 — Combat

- Tag projectile prefabs with `ThrowableElement`
- `ThrowManager.ApplyHit`: affinity multiplier + apply enemy passive
- Enemy passive state on `GameManager` or small `FloorEffectController`

### Phase 4 — Roguelike picks

- `RunState` (throwable pool, charms, floors cleared)
- Post-win pick screen (3 cards)
- Coin reward per floor: normal 30, elite 50, boss 100

### Phase 5 — Monetization

| Placement | Type | Trigger |
|-----------|------|---------|
| `revive` | Rewarded | Existing — lose floor |
| `double_coins` | Rewarded | Existing — win floor |
| `reroll_pick` | Rewarded | Don’t like 3 post-win options |
| `extra_pick` | Rewarded | Take 2 of 3 (floors 15+) |
| `continue_run` | Rewarded | Lose run at floor 12+ |
| Interstitial | Capped | `retry` / `next_level` (existing) |

**Coins (meta):** starter throwable slot, elemental charms (+5% cosmetic dmg), projectile skins.  
**IAP (later):** remove ads, season cosmetic pass — no direct power sells.

---

## What NOT to build

- 8+ element type chart
- Multiple active enemy skills per floor
- Separate player element + item element + floor element + weather element stacks
- Inventory UI beyond 4 throwables + 2 charms

---

## Open Questions

1. Should 2P local skip element passives on the human opponent?
2. Re-import `ThrowingGameData.json` into a `GameConfig` asset in editor after HP change?
3. Boss floor 20: fixed rotation order or random subset of 4 passives?

---

## Related Files

| File | Role |
|------|------|
| `Assets/Script/Gameplay/LevelProgression.cs` | Level + HP curve |
| `Assets/Script/Gameplay/WindManager.cs` | Wind UI + force |
| `Assets/Script/Gameplay/ThrowManager.cs` | Throw + damage |
| `Assets/Script/GameData/GameConfig.cs` | Tunable defaults |
| `Assets/Script/GameData/ThrowingGameData.json` | JSON import source |
