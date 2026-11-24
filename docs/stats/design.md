# Stats, Buffs & Health Design & Best Practices

Most beginners start by storing `public float attackDamage = 20;` on a player script and doing `attackDamage += 10;` when equipping a sword or drinking a potion. Within a week, unequipping two items in the wrong order, floating-point drift, or stacked temporary buffs permanently corrupt player stats. `com.contentcontent.stats` models the clean RPG modifier-stack pattern so stats stay deterministic and reversible.

---

## 1. Never Mutate Base Stats — Store Modifiers by Source

Instead of overwriting `Attack = 30`, keep `BaseValue = 20` untouched and attach a list of `StatModifier` entries tagged with their **`Source`** (a weapon object, an `ActiveBuff`, a passive skill):

```text
BaseValue (20)
  ├── +10 Flat        (Source: IronSword)
  ├── +25% PercentAdd (Source: IronSword)
  └── +15% PercentAdd (Source: BattleCry Buff)
       │
       ▼  Evaluation Order: (Base + Flat) × (1 + Σ PercentAdd) × Π (1 + PercentMult)
  Final Cached Value = (20 + 10) × (1 + 0.40) = 42.0
```

### Why This Evaluation Order Matters
- **`Flat` runs before percentages:** Adding `+10` sword damage scales cleanly with a `+20%` damage buff regardless of whether the sword or buff was applied first.
- **`PercentAdd` sums together before multiplying:** Two `+15%` buffs give `+30%`, preventing runaway exponential scaling from common bonuses.
- **`PercentMult` compounds independently:** Reserved for rare multiplicative effects (e.g., `"2x critical state"` or `"0.5x weakened curse"`).
- **Clean unequipping:** Calling `RemoveAllModifiersFromSource(sword)` removes every bonus from that item across all stats in one call — zero drift, zero order bugs.

---

## 2. Dirty Caching & Health Binding

- **Compute only when modifiers change:** `Stat` caches `_cachedValue` and sets `_isDirty = true` when modifiers are added or removed, returning `Value` in $O(1)$ during every combat hit or UI frame.
- **Bind `Health` to `MaxHealth` cleanly:** `HealthComponent` subscribes to `Stat.OnValueChanged` automatically. When a vitality ring or temporary buff raises `MaxHealth` from `100` to `150`, `MaxHealthChangePolicy.KeepPercentage` preserves `50/100 -> 75/150` without losing health state when the buff expires.

---

## 3. Best Practices & Common Pitfalls

- **Always attack via `IDamageable`:** Weapons, traps, and projectiles only call `target.TryGetComponent<IDamageable>()`. They never need to know whether the target is a player, enemy, destructible crate, or boss shield.
- **Check `BuffStackingPolicy` intentionally:** Use `RefreshDuration` for poison/burn resets, `StackValue` for ramp-up buffs, and `IgnoreIfActive` for shields that shouldn't double-refresh.

---

## 4. Engine Transferability (Godot & Unreal)

- **Godot:** Layer 1 (`ContentContent.Stats`) drops straight into Godot C#; replace `StatSheetAsset` / `BuffAsset` with Godot `Resource` subclasses.
- **Unreal:** Directly mirrors the Gameplay Ability System (GAS) `FAttributeSet` (`BaseValue` vs. `CurrentValue`) + `UGameplayEffect` modifier aggregation (`Add`, `Multiply`) without the heavy GAS setup boilerplate.
