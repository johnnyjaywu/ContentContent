# Stats, Buffs & Health Scripting API

## Namespace: `ContentContent.Stats`

### `ModifierType` *(enum)*

| Value | Default Order | Behavior |
| --- | --- | --- |
| `Flat` | `100` | Added directly to the running value (`+ value`). |
| `PercentAdd` | `200` | Consecutive entries are summed and applied once (`× (1 + sum)`). |
| `PercentMult` | `300` | Applied multiplicatively one after another (`× (1 + value)`). |

### `StatModifier` *(readonly struct, `IEquatable<StatModifier>`)*

| Member | Signature / Type | Description |
| --- | --- | --- |
| Constructor | `StatModifier(float value, ModifierType type, object source = null)` | Creates a modifier using `(int)type` as `Order`. |
| Constructor | `StatModifier(float value, ModifierType type, int order, object source = null)` | Creates a modifier with an explicit evaluation `Order`. |
| `Value`, `Type`, `Order`, `Source` | properties | Magnitude, mode, sort order, and owner tag for bulk removal. |

### `Stat` *(class)*

| Member | Signature / Type | Description |
| --- | --- | --- |
| Constructor | `Stat(float baseValue, float? minValue = null, float? maxValue = null)` | Initializes stat and optional clamps (`minValue <= maxValue`). |
| `BaseValue` | `float` *(get/set)* | Unmodified base value; setting marks dirty and notifies if final value changes. |
| `Value` | `float` *(get)* | Lazy-evaluated, clamped, 4-decimal rounded final value. |
| `MinValue` / `MaxValue` | `float?` | Optional clamp bounds. |
| `Modifiers` | `IReadOnlyList<StatModifier>` | Active modifiers in evaluation order. |
| `OnValueChanged` | `event Action<float, float>` | Fired when computed `Value` changes: `(oldValue, newValue)`. |
| `AddModifier` / `RemoveModifier` / `RemoveAllModifiersFromSource` / `ClearModifiers` | methods | Mutates modifiers and recalculates on next read. |

### `StatCollection<TStatId>` *(class)*

`Stats`, `RegisterStat(id, baseValue, minValue, maxValue)`, `TryGetStat(id, out stat)`, `GetStat(id)`, `GetValue(id, fallback)`, `AddModifier(id, modifier)`, `RemoveAllModifiersFromSource(source)`.

### Buffs (`BuffStackingPolicy`, `BuffModifierEntry<TStatId>`, `BuffDefinition<TStatId>`, `ActiveBuff<TStatId>`, `BuffController<TStatId>`)

- **`BuffStackingPolicy`**: `RefreshDuration`, `StackValue`, `IgnoreIfActive`.
- **`BuffDefinition<TStatId>`**: `Id`, `Duration` (`0` = permanent), `Modifiers`, `StackingPolicy`, `MaxStacks`.
- **`BuffController<TStatId>`**: `ActiveBuffs`, `OnBuffApplied`, `OnBuffRefreshed`, `OnBuffExpired`, `ApplyBuff`, `RemoveBuff`, `HasBuff`, `Tick(deltaTime)`, `ClearAll`.

### Health & Damage (`DamageInfo`, `DamageResult`, `IDamageable`, `MaxHealthChangePolicy`, `Health`)

- **`IDamageable`**: `DamageResult ApplyDamage(DamageInfo damage)`.
- **`Health`**: `Current`, `Max`, `Normalized`, `IsAlive`, `IsInvulnerable`, `OnHealthChanged`, `OnDamaged`, `OnHealed`, `OnDied`, `OnRevived`, `BindMaxHealthStat`, `UnbindMaxHealthStat`, `SetMaxHealth`, `ApplyDamage`, `Heal`, `Revive`, `Dispose`.

---

## Namespace: `ContentContent.Stats.Unity`

### `StatSheetAsset` & `BuffAsset` (`ScriptableObject`)
- **`StatSheetAsset`**: Create via **Create → ContentContent → Stats → Stat Sheet** (`Entries`, `ApplyStarterRpgPreset()`, `Populate(StatCollection<string>)`).
- **`BuffAsset`**: Create via **Create → ContentContent → Stats → Buff** (`BuffId`, `Duration`, `StackingPolicy`, `MaxStacks`, `Modifiers`, `BuildDefinition()`).

### `StatsComponent` (`MonoBehaviour`)
- Hosts lazily initialized `Stats` (`StatCollection<string>`) and `Buffs` (`BuffController<string>`), applies `_startingBuffs` on `Start()`, ticks `Buffs.Tick(dt)` in `Update()`.
- Methods & Events: `ApplyBuff(BuffAsset)`, `RemoveBuff(BuffAsset)`, `RemoveBuffById(string)`, `GetStatValue(string, float)`, `OnBuffApplied`, `OnBuffExpired`.

### `HealthComponent` (`MonoBehaviour, IDamageable`)
- Auto-discovers sibling `StatsComponent` and binds `Max` to `_maxHealthStatId` (`"MaxHealth"` by default) using `_maxHealthPolicy`.
- Exposes `Health`, `IsBoundToStat`, `Current`, `Max`, `IsAlive`, `ApplyDamage(DamageInfo)`, `ApplyFlatDamage(float)`, `Heal(float)`, `FullHeal()`, `Kill()`, `Revive(float?)`, `TryBindToStats(StatsComponent)`, and `UnityEvent`s (`OnHealthChanged`, `OnDamaged`, `OnHealed`, `OnDied`, `OnRevived`).
