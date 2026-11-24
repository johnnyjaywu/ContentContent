# How Stats, Buffs & Health Works

`com.contentcontent.stats` provides a generic, zero-dependency RPG attribute, timed buff, and health/damage system for Unity, split cleanly between a pure C# core and thin Inspector-friendly adapters.

## Package Architecture

```text
Packages/com.contentcontent.stats/
├── package.json                                # Standalone UPM package (zero dependencies)
├── Runtime/
│   ├── ContentContent.Stats.asmdef             # Pure C# (noEngineReferences: true)
│   ├── StatModifier.cs                         # ModifierType & immutable StatModifier struct
│   ├── Stat.cs                                 # Dirty-cached attribute with ordered stacking & clamps
│   ├── StatCollection.cs                       # Generic StatCollection<TStatId> keyed container
│   ├── Buffs.cs                                # BuffDefinition, ActiveBuff & BuffController<TStatId>
│   ├── Health.cs                               # DamageInfo, DamageResult, IDamageable & Health
│   └── Unity/
│       ├── ContentContent.Stats.Unity.asmdef   # Thin Unity adapters
│       ├── StatSheetAsset.cs                   # ScriptableObject base-stat sheet
│       ├── StatsComponent.cs                   # MonoBehaviour hosting Stats + ticked Buffs
│       └── HealthComponent.cs                  # MonoBehaviour IDamageable + UnityEvents
└── Tests/EditMode/
    ├── ContentContent.Stats.Tests.asmdef
    └── StatsSystemTests.cs
```

Because `ContentContent.Stats.asmdef` sets `"noEngineReferences": true`, all stat math, buff timers, stacking policies, and health/damage logic compile without `UnityEngine` and run deterministically in EditMode or pure `.NET` unit tests.

---

## Core Concepts

### 1. Dirty-Cached Modifiers (`Stat` & `StatModifier`)

Each `Stat` holds a `BaseValue`, optional `MinValue` / `MaxValue` clamps, and an ordered list of immutable `StatModifier` structs. Re-evaluation is **lazy**: `Value` is cached and only recalculated when `BaseValue` or modifiers change.

Modifiers are inserted in ascending `Order` (defaulting to the `ModifierType` enum value):

1. **`ModifierType.Flat` (`100`)** — Added directly to the running value (`finalValue += mod.Value`).
2. **`ModifierType.PercentAdd` (`200`)** — Consecutive `PercentAdd` modifiers are summed and applied once (`finalValue *= 1f + sum`). For example, `+0.20` and `+0.30` yield `× 1.50`.
3. **`ModifierType.PercentMult` (`300`)** — Applied multiplicatively one after another (`finalValue *= 1f + mod.Value`).

After all modifiers run, the result is clamped to `[MinValue, MaxValue]` and rounded to 4 decimal places (`Math.Round(finalValue, 4)`) to prevent floating-point drift.

Each `StatModifier` carries an optional `object Source` tag (such as an equipped item or an `ActiveBuff`), making bulk cleanup trivial via `RemoveAllModifiersFromSource(source)`.

### 2. Generic Stat Containers (`StatCollection<TStatId>`)

`StatCollection<TStatId>` maps any key type — an `enum` for compile-time safety or `string` for data-driven sheets — to `Stat` instances. It exposes convenience helpers (`RegisterStat`, `TryGetStat`, `GetStat`, `GetValue`, `AddModifier`) and cross-stat cleanup (`RemoveAllModifiersFromSource`).

### 3. Timed & Stacking Buffs (`BuffController<TStatId>`)

`BuffController<TStatId>` manages active `BuffDefinition<TStatId>` instances and automatically synchronizes their modifiers with a `StatCollection<TStatId>` using each `ActiveBuff<TStatId>` as the modifier `Source`.

- **Stacking policies (`BuffStackingPolicy`):**
  - `RefreshDuration` — Resets `RemainingDuration` without increasing stacks.
  - `StackValue` — Increments `Stacks` up to `MaxStacks`, multiplies each modifier entry by `Stacks`, and refreshes duration.
  - `IgnoreIfActive` — Leaves the existing buff untouched.
- **Permanent buffs:** Setting `Duration = 0f` creates a permanent buff (`IsPermanent == true`) that never expires on `Tick(deltaTime)` until explicitly removed via `RemoveBuff` or `ClearAll`.
- **Events:** `OnBuffApplied`, `OnBuffRefreshed`, and `OnBuffExpired`.

### 4. Health & Dynamic Stat Binding (`Health` & `IDamageable`)

`Health` implements `IDamageable` and `IDisposable`, tracking `Current`, `Max`, `Normalized`, `IsAlive`, and `IsInvulnerable`:

- **`ApplyDamage(DamageInfo)`** returns a `DamageResult` (`RequestedDamage`, `AppliedDamage`, `RemainingHealth`, `WasFatal`, `WasBlocked`) and fires `OnDamaged`, `OnHealthChanged`, and `OnDied` (on fatal hits).
- **`Heal(amount)`** and **`Revive(healthAmount?)`** handle recovery and resurrection with `OnHealed`, `OnRevived`, and `OnHealthChanged`.
- **`BindMaxHealthStat(Stat, MaxHealthChangePolicy)`** subscribes to a `Stat.OnValueChanged` event so equipment or buffs that alter max health automatically update `Health.Max` using one of three policies:
  - `KeepCurrent` — Keeps `Current` unchanged (clamped to new `Max`).
  - `KeepPercentage` — Preserves `Current / Max` ratio.
  - `AddDelta` — Adds positive max-health gains directly to `Current`.

> Always call `health.Dispose()` (or `UnbindMaxHealthStat()`) when the owner is destroyed so the `Stat` event subscription is released. `HealthComponent` does this automatically in `OnDestroy()`.
